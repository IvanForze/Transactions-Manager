using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Polling;
using KotovProject3_2;
using ManagerLibrary;
using MyTelegramBot;

/// <summary>
/// Основной класс программы для запуска Telegram-бота.
/// </summary>
class Program
{
    /// <summary>
    /// Точка входа в программу. Запускает Telegram-бота.
    /// </summary>
    static async Task Main()
    {
        var botToken = "7939745223:AAFd_t59qZoTnwBIV6K9WmdR6oH2aePBgyA";
        var _bot = new TelegramBot(botToken);
        await _bot.StartAsync();
    }
}

/// <summary>
/// Класс, представляющий Telegram-бота для управления транзакциями и анализа данных.
/// </summary>
public class TelegramBot
{
    private readonly TelegramBotClient _bot;
    private readonly CancellationTokenSource _cts;
    private readonly ReceiverOptions _receiverOptions;
    private Dictionary<long, string> _userStates = new Dictionary<long, string>();
    private Dictionary<long, (string filterField, string filterValue)> _filterCache = new Dictionary<long, (string, string)>();

    private TransactionsProcessor _transactionsProcessor = new TransactionsProcessor();
    private FilesProcessor _filesProcessor = new FilesProcessor();
    private TrendAnalysis _trendAnalysis = new TrendAnalysis();
    private TablesProcessor _tablesProcessor = new TablesProcessor();
    private readonly StateHandler _stateHandler;


    /// <summary>
    /// Конструктор класса TelegramBot. Инициализирует бота и его зависимости.
    /// </summary>
    /// <param name="botToken">Токен Telegram-бота.</param>
    public TelegramBot(string botToken)
    {
        _cts = new CancellationTokenSource();
        _bot = new TelegramBotClient(botToken);
        _receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = { }
        };
        _stateHandler = new StateHandler(
            this,
            _bot,
            _transactionsProcessor,
            _filesProcessor,
            _tablesProcessor,
            _userStates,
            _filterCache);
    }

    /// <summary>
    /// Запускает Telegram-бота и начинает обработку входящих сообщений.
    /// </summary>
    public async Task StartAsync()
    {
        var me = await _bot.GetMe();
        Console.WriteLine($"@{me.Username} is running... Press Enter to terminate");

        // Устанавливаем команды с подсказками
        var commands = new List<BotCommand>
        {
            new BotCommand { Command = "start", Description = "Запустить бота и показать главное меню" },
        };

        await _bot.SetMyCommands(commands);

        // Запуск бота с обработчиком обновлений
        _bot.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            _receiverOptions,
            cancellationToken: _cts.Token
        );

        Console.ReadLine();
        _cts.Cancel(); // Остановка бота
    }

    /// <summary>
    /// Обрабатывает входящие обновления от Telegram.
    /// </summary>
    /// <param name="botClient">Клиент Telegram-бота.</param>
    /// <param name="update">Входящее обновление.</param>
    /// <param name="cancellationToken">Токен отмены для асинхронных операций.</param>
    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type == UpdateType.Message && update.Message?.Text != null)
        {
            var msg = update.Message;
            var chatId = msg.Chat.Id;
            var messageText = msg.Text;

            Console.WriteLine($"Received '{messageText}' in {msg.Chat}");

            if (messageText == "/start")
            {
                _userStates.Remove(chatId);
                await SendMainMenuAsync(chatId, cancellationToken);
            }
            else if (_userStates.ContainsKey(chatId))
            {
                var state = _userStates[chatId];
                await _stateHandler.HandleStateAsync(chatId, messageText, state, cancellationToken);
            }
        }
        else if (update.Type == UpdateType.Message && update.Message?.Document != null)
        {
            var chatId = update.Message.Chat.Id;
            var document = update.Message.Document;

            if (_userStates.ContainsKey(chatId) && _userStates[chatId] == "awaiting_file")
            {
                
                try
                {
                    // Получаем информацию о файле
                    var fileId = document.FileId;
                    var fileInfo = await _bot.GetFile(fileId, cancellationToken);

                    // Скачиваем файл
                    var filePath = Path.Combine(Path.GetTempPath(), document.FileName);
                    await using (var fileStream = File.Create(filePath))
                    {
                        await _bot.DownloadFile(fileInfo.FilePath, fileStream, cancellationToken);
                    }

                    // Читаем содержимое файла
                    var fileContent = await File.ReadAllLinesAsync(filePath, cancellationToken);

                    // Обрабатываем транзакции
                    _filesProcessor.ParseTransactions(fileContent.ToList(), _transactionsProcessor.transactions);
                    await _bot.SendMessage(chatId, "Транзакции успешно добавлены из файла.");

                    // Удаляем временный файл
                    File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    await _bot.SendMessage(chatId, $"Ошибка при обработке файла: {ex.Message}");
                }

                // Сбрасываем состояние пользователя
                _userStates.Remove(chatId);
                await SendMainMenuAsync(chatId, cancellationToken);
            }
        }
        else if (update.Type == UpdateType.CallbackQuery)
        {
            await HandleCallbackQuery(update.CallbackQuery);
        }
    }

    /// <summary>
    /// Отправляет главное меню с доступными действиями.
    /// </summary>
    /// <param name="chatId">Идентификатор чата.</param>
    /// <param name="cancellationToken">Токен отмены для асинхронных операций.</param>
    public async Task SendMainMenuAsync(long chatId, CancellationToken cancellationToken)
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("Загрузить транзакции через файл", "add_file") },
            new[] { InlineKeyboardButton.WithCallbackData("Просмотр транзакций", "view_transactions") },
            new[] { InlineKeyboardButton.WithCallbackData("Добавить транзакцию", "add_transaction") },
            new[] { InlineKeyboardButton.WithCallbackData("Удалить транзакцию", "delete_transaction") },
            new[] { InlineKeyboardButton.WithCallbackData("Бюджетирование", "budget") },
            new[] { InlineKeyboardButton.WithCallbackData("Прогнозирование", "forecast") },
            new[] { InlineKeyboardButton.WithCallbackData("Анализ трендов", "trend_analysis") }
        });

        await _bot.SendMessage(chatId, "Выберите действие:", replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Отправляет меню для работы с бюджетом.
    /// </summary>
    /// <param name="chatId">Идентификатор чата.</param>
    /// <param name="cancellationToken">Токен отмены для асинхронных операций.</param>
    public async Task SendBudgetMenuAsync(long chatId, CancellationToken cancellationToken)
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("Фильтрация", "filter_transactions") },
            new[] { InlineKeyboardButton.WithCallbackData("Сортировка", "transactions_sort_menu") },
            new[] { InlineKeyboardButton.WithCallbackData("Диаграмма расходов", "expenses_diagram") },
            new[] { InlineKeyboardButton.WithCallbackData("Главное меню", "main_menu") }
        });

        await _bot.SendMessage(chatId, "Выберите действие:", replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Отправляет меню для фильтрации транзакций.
    /// </summary>
    /// <param name="chatId">Идентификатор чата.</param>
    /// <param name="cancellationToken">Токен отмены для асинхронных операций.</param>
    public async Task SendFilterMenuAsync(long chatId, CancellationToken cancellationToken)
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("Дата", "filter_date") },
            new[] { InlineKeyboardButton.WithCallbackData("Сумма", "filter_amount") },
            new[] { InlineKeyboardButton.WithCallbackData("Категория", "filter_category") },
            new[] { InlineKeyboardButton.WithCallbackData("Главное меню", "main_menu") }
        });

        await _bot.SendMessage(chatId, "Выберите действие:", replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);
    }
    /// <summary>
    /// Отправляет меню для установки бюджета.
    /// </summary>
    /// <param name="chatId">Идентификатор чата.</param>
    /// <param name="cancellationToken">Токен отмены для асинхронных операций.</param>
    public async Task SendSetBudgetMenuAsync(long chatId, CancellationToken cancellationToken)
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("Установить бюджет", "set_budget") },
            new[] { InlineKeyboardButton.WithCallbackData("Главное меню", "main_menu") }
        });

        await _bot.SendMessage(chatId, "Выберите действие:", replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Отправляет меню для сортировки транзакций.
    /// </summary>
    /// <param name="chatId">Идентификатор чата.</param>
    /// <param name="cancellationToken">Токен отмены для асинхронных операций.</param>
    /// <param name="isOrderStep">Флаг, указывающий на этап выбора порядка сортировки.</param>
    private async Task SendSortMenuAsync(long chatId, CancellationToken cancellationToken, bool isOrderStep = false)
    {
        if (!isOrderStep)
        {
            // Шаг 1: Выбор поля
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Дата ↑", "sort_date_asc"),
                    InlineKeyboardButton.WithCallbackData("Дата ↓", "sort_date_desc")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Сумма ↑", "sort_amount_asc"),
                    InlineKeyboardButton.WithCallbackData("Сумма ↓", "sort_amount_desc")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Категория ↑", "sort_category_asc"),
                    InlineKeyboardButton.WithCallbackData("Категория ↓", "sort_category_desc")
                },
                new[] { InlineKeyboardButton.WithCallbackData("Главное меню", "main_menu") }
            });

            await _bot.SendMessage(chatId, "Выберите поле и порядок сортировки:", replyMarkup: inlineKeyboard);
        }
    }

    /// <summary>
    /// Обрабатывает нажатия на кнопки в меню.
    /// </summary>
    /// <param name="callbackQuery">Объект CallbackQuery, содержащий данные о нажатии.</param>
    public async Task HandleCallbackQuery(CallbackQuery callbackQuery)
    {
        var chatId = callbackQuery.Message.Chat.Id;
        var data = callbackQuery.Data;

        Console.WriteLine($"User {callbackQuery.From.Username} clicked on {data}");

        // Отправляем ответ в зависимости от выбранной кнопки
        switch (data)
        {
            case "add_file":
                await _bot.SendMessage(chatId, "Вы выбрали добавить транзакции через файл.");
                _userStates[chatId] = "awaiting_file";

                break;

            case "view_transactions":
                if (_transactionsProcessor.transactions.Count == 0) // Проверка на наличие данных
                {
                    await _bot.SendMessage(chatId, "Нет данных. Сначала введите данные через меню");

                    await SendMainMenuAsync(chatId, CancellationToken.None);
                    break; // Выход из метода, если данных нет
                }
                await _bot.SendMessage(chatId, "Вы выбрали посмотреть транзакции.");

                var imagePath = _tablesProcessor.GenerateTableImage(_transactionsProcessor.transactions);

                // Отправляем изображение в Telegram
                await SendPhotoAsync(chatId, imagePath, "Ваши транзакции:");

                await SendBudgetMenuAsync(chatId, CancellationToken.None);

                break;

            case "add_transaction":
                _userStates[chatId] = "awaiting_transaction";
                await _bot.SendMessage(chatId, "Вы выбрали добавить транзакцию.");
                await _bot.SendMessage(chatId, "Введите транзацию в формате [Дата(ГГГГ - ММ - ДД)] [Сумма] [Категория] [Описание]");
                

                // Логика для создания и отправки круговой диаграммы
                break;

            case "delete_transaction":
                if (_transactionsProcessor.transactions.Count == 0) // Проверка на наличие данных
                {
                    await _bot.SendMessage(chatId, "Нет данных. Сначала введите данные через меню");

                    await SendMainMenuAsync(chatId, CancellationToken.None);
                    break; // Выход из метода, если данных нет
                }
                await _bot.SendMessage(chatId, "Вы выбрали удалить транзакцию.");
                await DeleteTransaction(chatId);
                break;

            case "budget":
                if (_transactionsProcessor.transactions.Count == 0) // Проверка на наличие данных
                {
                    await _bot.SendMessage(chatId, "Нет данных. Сначала введите данные через меню");

                    await SendMainMenuAsync(chatId, CancellationToken.None);
                    break; // Выход из метода, если данных нет
                }
                await _bot.SendMessage(chatId, "Вы выбрали удалить бюджетирование.");

                imagePath = _tablesProcessor.GenerateBudgetTableImage(_transactionsProcessor.transactions, _transactionsProcessor.TransactionsBudget);
                await SendPhotoAsync(chatId, imagePath, "Ваши транзакции:");

                SendSetBudgetMenuAsync(chatId, CancellationToken.None);

                break;
            case "set_budget":
                if (_transactionsProcessor.transactions.Count == 0) // Проверка на наличие данных
                {
                    await _bot.SendMessage(chatId, "Нет данных. Сначала введите данные через меню");

                    await SendMainMenuAsync(chatId, CancellationToken.None);
                    break; // Выход из метода, если данных нет
                }
                await _bot.SendMessage(chatId, "Вы выбрали установить бюджет.");
                await _bot.SendMessage(chatId, "Введите категорию и сумму (например [Другое] [600]).");
                _userStates[chatId] = "awaiting_set_budget";
                

                break;

            case "forecast":
                if (_transactionsProcessor.transactions.Count == 0) // Проверка на наличие данных
                {
                    await _bot.SendMessage(chatId, "Нет данных. Сначала введите данные через меню");

                    await SendMainMenuAsync(chatId, CancellationToken.None);
                    break; // Выход из метода, если данных нет
                }
                await _bot.SendMessage(chatId, "Вы выбрали прогнозирование");
                var budget = _transactionsProcessor.CalculateBudgetForecast();

                imagePath = _tablesProcessor.GenerateForecastTableImage(budget);
                await SendPhotoAsync(chatId, imagePath, "Ваш прогноз:");

                SendMainMenuAsync(chatId, CancellationToken.None);

                break;
            case "trend_analysis":
                if (_transactionsProcessor.transactions.Count == 0) // Проверка на наличие данных
                {
                    await _bot.SendMessage(chatId, "Нет данных. Сначала введите данные через меню");

                    await SendMainMenuAsync(chatId, CancellationToken.None);
                    break; // Выход из метода, если данных нет
                }
                await _bot.SendMessage(chatId, "Вы выбрали анализ трендов.");

                await SendPlots(chatId);

                await SendMainMenuAsync(chatId, CancellationToken.None);
                break;

            
            
            case "filter_transactions":
                await SendFilterMenuAsync(chatId, CancellationToken.None);
                break;
            case "filter_date":
            case "filter_amount":
            case "filter_category":
                await _bot.SendMessage(chatId, "Введите значение для фильтрации");
                // Сохраняем выбранное поле для фильтрации
                _userStates[chatId] = "awaiting_filter_value";
                _filterCache[chatId] = (data.Split('_')[1], null);

                break;
            case "expenses_diagram":
                imagePath = _tablesProcessor.GenerateExpensesDiagramImage(_transactionsProcessor.transactions);
                await SendPhotoAsync(chatId, imagePath, "Ваши транзакции:");

                await SendBudgetMenuAsync(chatId, CancellationToken.None);

                break;
            case "transactions_sort_menu":
                await SendSortMenuAsync(chatId, CancellationToken.None);
                break;
            case string s when s.StartsWith("sort_"):
                var parts = data.Split('_');
                var field = parts[1] switch
                {
                    "date" => "Дата",
                    "amount" => "Сумма",
                    "category" => "Категория",
                };
                var order = parts[2] switch
                {
                    "asc" => "По возрастанию",
                    "desc" => "По убыванию"
                };

                _transactionsProcessor.SortTransactions(field, order);

                await _bot.SendMessage(chatId, $"Транзакции отсортированы.");

                // Показываем обновленные транзакции
                imagePath = _tablesProcessor.GenerateTableImage(_transactionsProcessor.transactions);
                await SendPhotoAsync(chatId, imagePath, "Ваши транзакции:");

                await SendBudgetMenuAsync(chatId, CancellationToken.None);
                break;
            case "main_menu":
                await SendMainMenuAsync(chatId, CancellationToken.None);
                break;

            default:
                await _bot.SendMessage(chatId, "Неизвестная команда.");
                break;
        }


        // Подтверждаем обработку CallbackQuery
        await _bot.AnswerCallbackQuery(callbackQuery.Id);
    }

    /// <summary>
    /// Обрабатывает запрос на удаление транзакции.
    /// </summary>
    /// <param name="chatId">Идентификатор чата.</param>
    public async Task DeleteTransaction(long chatId)
    {
        if (_transactionsProcessor.transactions.Count == 0) // Проверка на наличие данных
        {
            await _bot.SendMessage(chatId, "Нет данных. Сначала введите данные через меню");

            await SendMainMenuAsync(chatId, CancellationToken.None);
            return; // Выход из метода, если данных нет
        }

        await _bot.SendMessage(chatId, $"Введите номер транзакции для удаления (доступные номера 0-{_transactionsProcessor.transactions.Count - 1}): ");

        _userStates[chatId] = "awaiting_delete_transaction";
        
    }

    /// <summary>
    /// Отправляет изображение в Telegram.
    /// </summary>
    /// <param name="chatId">Идентификатор чата.</param>
    /// <param name="imagePath">Путь к изображению.</param>
    /// <param name="caption">Подпись к изображению.</param>
    public async Task SendPhotoAsync(long chatId, string imagePath, string caption = "")
    {
        try
        {
            // Открываем файл изображения
            await using var stream = File.OpenRead(imagePath);

            // Отправляем изображение
            await _bot.SendPhoto(
                chatId: chatId,
                photo: new InputFileStream(stream, Path.GetFileName(imagePath)),
                caption: caption
            );

            // Удаляем временный файл
            File.Delete(imagePath);
        }
        catch (Exception ex)
        {
            await _bot.SendMessage(chatId, $"Ошибка при отправке изображения: {ex.Message}");
        }
    }

    /// <summary>
    /// Отправляет графики анализа трендов.
    /// </summary>
    /// <param name="chatId">Идентификатор чата.</param>
    public async Task SendPlots(long chatId)
    {
        if (_transactionsProcessor.transactions.Count == 0) // Проверка на наличие данных
        {
            await _bot.SendMessage(chatId, "Нет данных. Сначала введите данные через меню");
            return; // Выход из метода, если данных нет
        }

        var transactions = _transactionsProcessor.transactions;
        List<string> plotsPaths = _trendAnalysis.DrawScottPlots(transactions);

        // Проверяем, что список путей не пустой
        if (plotsPaths == null || plotsPaths.Count == 0)
        {
            await _bot.SendMessage(chatId, "Не удалось сгенерировать графики.");
            return;
        }

        var media = new List<InputMediaPhoto>();

        foreach (var plotPath in plotsPaths)
        {
            try
            {
                if (!File.Exists(plotPath))
                {
                    await _bot.SendMessage(chatId, $"Файл не найден: {plotPath}");
                    continue;
                }

                // Открытие потока для каждого файла
                var stream = File.OpenRead(plotPath);

                // Добавление фото в список медиа
                media.Add(new InputMediaPhoto
                {
                    Media = InputFile.FromStream(stream, Path.GetFileName(plotPath)),
                    Caption = $"{Path.GetFileName(plotPath)}",
                });
            }
            catch (Exception ex)
            {
                // Логируем ошибку и отправляем сообщение пользователю
                Console.WriteLine($"Ошибка при обработке файла {plotPath}: {ex.Message}");
                await _bot.SendMessage(chatId, $"Ошибка при обработке файла: {plotPath} - {ex.Message}");
            }
        }

        // Отправка группированных медиафайлов
        if (media.Count > 0)
        {
            try
            {
                await _bot.SendMediaGroup(chatId, media);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отправке фото: {ex.Message}");
                await _bot.SendMessage(chatId, "Ошибка при отправке фото. Попробуйте еще раз.");
            }
        }
    }

    /// <summary>
    /// Обрабатывает ошибки, возникающие при работе бота.
    /// </summary>
    /// <param name="botClient">Клиент Telegram-бота.</param>
    /// <param name="exception">Исключение, вызвавшее ошибку.</param>
    /// <param name="cancellationToken">Токен отмены для асинхронных операций.</param>
    public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Error occurred: {exception.Message}");
        // Можно добавить сюда логику для повторной попытки или уведомления о ошибке
        await Task.Delay(2000, cancellationToken);
    }
}
