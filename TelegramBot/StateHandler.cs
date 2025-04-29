using KotovProject3_2;
using ManagerLibrary;
using MyTelegramBot;
using Telegram.Bot;

/// <summary>
/// Класс для обработки состояний пользователя в Telegram-боте.
/// </summary>
public class StateHandler
{
    private readonly TelegramBot _telegramBot;
    private readonly TelegramBotClient _bot;
    private readonly TransactionsProcessor _transactionsProcessor;
    private readonly FilesProcessor _filesProcessor;
    private readonly TablesProcessor _tablesProcessor;
    private readonly Dictionary<long, string> _userStates;
    private readonly Dictionary<long, (string filterField, string filterValue)> _filterCache;

    /// <summary>
    /// Конструктор класса StateHandler. Инициализирует зависимости и кэши.
    /// </summary>
    /// <param name="telegramBot">Экземпляр TelegramBot.</param>
    /// <param name="bot">Экземпляр TelegramBotClient.</param>
    /// <param name="transactionsProcessor">Обработчик транзакций.</param>
    /// <param name="filesProcessor">Обработчик файлов.</param>
    /// <param name="tablesProcessor">Обработчик таблиц и графиков.</param>
    /// <param name="userStates">Словарь состояний пользователей.</param>
    /// <param name="filterCache">Словарь для хранения данных фильтрации.</param>
    public StateHandler(
        TelegramBot telegramBot,
        TelegramBotClient bot,
        TransactionsProcessor transactionsProcessor,
        FilesProcessor filesProcessor,
        TablesProcessor tablesProcessor,
        Dictionary<long, string> userStates,
        Dictionary<long, (string, string)> filterCache)
    {
        _telegramBot = telegramBot;
        _bot = bot;
        _transactionsProcessor = transactionsProcessor;
        _filesProcessor = filesProcessor;
        _tablesProcessor = tablesProcessor;
        _userStates = userStates;
        _filterCache = filterCache;
    }

    /// <summary>
    /// Обрабатывает текущее состояние пользователя и выполняет соответствующие действия.
    /// </summary>
    /// <param name="chatId">Идентификатор чата пользователя.</param>
    /// <param name="messageText">Текст сообщения пользователя.</param>
    /// <param name="state">Текущее состояние пользователя.</param>
    /// <param name="cancellationToken">Токен отмены для асинхронных операций.</param>
    public async Task HandleStateAsync(long chatId, string messageText, string state, CancellationToken cancellationToken)
    {
        switch (state)
        {
            case "awaiting_transaction":
                await HandleAwaitingTransaction(chatId, messageText, cancellationToken);
                break;
            case "awaiting_delete_transaction":
                await HandleAwaitingDeleteTransaction(chatId, messageText, cancellationToken);
                break;
            case "awaiting_filter_value":
                await HandleAwaitingFilterValue(chatId, messageText, cancellationToken);
                break;
            case "awaiting_set_budget":
                await HandleAwaitingSetBudget(chatId, messageText, cancellationToken);
                break;
        }
    }

    /// <summary>
    /// Обрабатывает состояние ожидания добавления транзакции.
    /// </summary>
    /// <param name="chatId">Идентификатор чата пользователя.</param>
    /// <param name="messageText">Текст сообщения пользователя.</param>
    /// <param name="cancellationToken">Токен отмены для асинхронных операций.</param>
    private async Task HandleAwaitingTransaction(long chatId, string messageText, CancellationToken cancellationToken)
    {
        try
        {
            var file = new List<string>() { messageText };
            var transactions = _transactionsProcessor.transactions;
            _filesProcessor.ParseTransactions(file, transactions);
            await _bot.SendMessage(chatId, "Транзакции успешно добавлены.");
        }
        catch (Exception e)
        {
            await _bot.SendMessage(chatId, $"{e}");
        }

        _userStates.Remove(chatId);
        await _telegramBot.SendMainMenuAsync(chatId, cancellationToken);
    }

    /// <summary>
    /// Обрабатывает состояние ожидания удаления транзакции.
    /// </summary>
    /// <param name="chatId">Идентификатор чата пользователя.</param>
    /// <param name="messageText">Текст сообщения пользователя.</param>
    /// <param name="cancellationToken">Токен отмены для асинхронных операций.</param>
    private async Task HandleAwaitingDeleteTransaction(long chatId, string messageText, CancellationToken cancellationToken)
    {
        if (int.TryParse(messageText, out int index) && index >= 0 && index < _transactionsProcessor.transactions.Count)
        {
            _transactionsProcessor.DeleteTransaction(index);
            await _bot.SendMessage(chatId, "Транзакция успешно удалена.");
        }
        else
        {
            await _bot.SendMessage(chatId, "Введите корректный индекс транзакции.");
        }
        _userStates.Remove(chatId);
        await _telegramBot.SendMainMenuAsync(chatId, cancellationToken);
    }

    /// <summary>
    /// Обрабатывает состояние ожидания значения для фильтрации.
    /// </summary>
    /// <param name="chatId">Идентификатор чата пользователя.</param>
    /// <param name="messageText">Текст сообщения пользователя.</param>
    /// <param name="cancellationToken">Токен отмены для асинхронных операций.</param>
    private async Task HandleAwaitingFilterValue(long chatId, string messageText, CancellationToken cancellationToken)
    {
        try
        {
            var filterValue = messageText.Trim();
            var (filterField, _) = _filterCache[chatId];
            var field = filterField switch
            {
                "date" => "Дата",
                "amount" => "Сумма",
                "category" => "Категория",
            };
            _transactionsProcessor.FilterTransactions(field, filterValue);
            await _bot.SendMessage(chatId, "Применён фильтр");

            var imagePath = _tablesProcessor.GenerateTableImage(_transactionsProcessor.transactions);
            await _telegramBot.SendPhotoAsync(chatId, imagePath, "Ваши транзакции:");

            await _telegramBot.SendBudgetMenuAsync(chatId, CancellationToken.None);
        }
        catch (Exception ex)
        {
            await _bot.SendMessage(chatId, $"Ошибка фильтрации: {ex.Message}");
        }

        _userStates.Remove(chatId);
        _filterCache.Remove(chatId);
    }

    /// <summary>
    /// Обрабатывает состояние ожидания установки бюджета.
    /// </summary>
    /// <param name="chatId">Идентификатор чата пользователя.</param>
    /// <param name="messageText">Текст сообщения пользователя.</param>
    /// <param name="cancellationToken">Токен отмены для асинхронных операций.</param>
    private async Task HandleAwaitingSetBudget(long chatId, string messageText, CancellationToken cancellationToken)
    {
        try
        {
            var split = messageText.Trim('[', ']').Split("] [");
            var category = split[0];
            var value = double.Parse(split[1]);

            _transactionsProcessor.ChangeTransactionsBudget(category, value);

            var imagePath = _tablesProcessor.GenerateBudgetTableImage(_transactionsProcessor.transactions, _transactionsProcessor.TransactionsBudget);
            await _telegramBot.SendPhotoAsync(chatId, imagePath, "Ваши транзакции:");
            await _telegramBot.SendSetBudgetMenuAsync(chatId, CancellationToken.None);
        }
        catch (Exception ex)
        {
            await _bot.SendMessage(chatId, $"Ошибка установки бюджета: {ex.Message}");
        }

        _userStates.Remove(chatId);
    }
}