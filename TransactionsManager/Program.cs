using KotovProject3_2;
using ManagerLibrary;
using System.Globalization;
using System.Text;
using Transaction = ManagerLibrary.Transaction;
// Котов Иван БПИ246-1 Вариант 4.

/// <summary>
/// Основной класс программы для управления личными финансами.
/// </summary>
public class Program
{
    private FilesProcessor _filesProcessor = new FilesProcessor();

    private TransactionsProcessor _transactionsProcessor = new TransactionsProcessor();

    private SpectreConsole _spectreConsole;

    private TrendAnalysis _trendAnalysis = new TrendAnalysis();

    /// <summary>
    /// Конструктор класса Program. Инициализирует объект SpectreConsole.
    /// </summary>
    public Program()
    {
        _spectreConsole = new SpectreConsole(_transactionsProcessor);
    }
    /// <summary>
    /// Точка входа в программу. Настраивает кодировку консоли и запускает основной цикл программы.
    /// </summary>
    public static void Main()
    {
        var program = new Program();
        program.Run();
    }
    /// <summary>
    /// Основной метод, запускающий цикл работы программы. Отображает меню и обрабатывает ввод пользователя.
    /// </summary>
    public void Run()
    {
        bool isRunning = true;

        Console.OutputEncoding = Encoding.UTF8;
        while (isRunning)
        {
            Console.Clear(); // Очистка консоли перед выводом меню
            Console.WriteLine("Меню:");
            Console.WriteLine("1. Ввести данные (через файл)");
            Console.WriteLine("2. Просмотр транзакций");
            Console.WriteLine("3. Добавить транзакцию");
            Console.WriteLine("4. Удалить транзакцию");
            Console.WriteLine("5. Бюджетирование");
            Console.WriteLine("6. Прогнозирование");
            Console.WriteLine("7. Анализ трендов");
            Console.WriteLine("8. Выход");
            Console.Write("Выберите пункт меню: ");

            string? input = Console.ReadLine();

            switch (input)
            {
                case "1":
                    InputData();
                    break;
                case "2":
                    _spectreConsole.DrawTransactionsTable();
                    break;
                case "3":
                    AddTransaction();
                    break;
                case "4":
                    DeleteTransaction();
                    break;
                case "5":
                    _spectreConsole.DrawBudgetTable(_transactionsProcessor.transactions);
                    break;
                case "6":
                    _spectreConsole.DrawBudgetForecastTable(_transactionsProcessor.transactions);
                    break;                
                case "7":
                    List<string> plots_paths = _trendAnalysis.DrawScottPlots(_transactionsProcessor.transactions);
                    break;
                case "8":
                    isRunning = false;
                    _filesProcessor.SaveTransactions(_transactionsProcessor.transactions);
                    Console.WriteLine("Выход из программы...");
                    break;
                default:
                    Console.WriteLine("Неверный ввод. Пожалуйста, выберите пункт от 1 до 7.");
                    break;
            }

            if (isRunning)
            {
                Console.WriteLine("Нажмите любую клавишу для продолжения...");
                Console.ReadKey();
            }
        }

        
    }
    /// <summary>
    /// Метод для ввода данных из файла. Запрашивает путь к файлу и загружает транзакции.
    /// </summary>
    void InputData()
    {
        Console.Write("Введите путь к файлу: ");
        _filesProcessor.filePath = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(_filesProcessor.filePath))
        {
            Console.WriteLine("Введите не пустой путь!");
            return;
        }

        List<string> file = _filesProcessor.ReadFile();

        _filesProcessor.ParseTransactions(file, _transactionsProcessor.transactions);

        Console.WriteLine("Транзакции успешно добавлены.");
    }
    /// <summary>
    /// Метод для удаления транзакции по индексу. Запрашивает у пользователя номер транзакции для удаления.
    /// </summary>
    void DeleteTransaction()
    {
        if (_transactionsProcessor.transactions.Count == 0) // Проверка на наличие данных
        {
            Console.WriteLine("Нет данных. Сначала введите данные через меню");
            return; // Выход из метода, если данных нет
        }

        Console.Write($"Введите номер транзакции для удаления (доступные номера 0-{_transactionsProcessor.transactions.Count - 1}): ");
        if (int.TryParse(Console.ReadLine(), out int index) && (index >= 0))
        {
            _transactionsProcessor.DeleteTransaction(index);
        }
        else
        {
            Console.WriteLine("Введите корректный индекс транзакции.");
        }
    }
    /// <summary>
    /// Метод для добавления новой транзакции. Запрашивает у пользователя данные о транзакции: дату, сумму, категорию и описание.
    /// </summary>
    void AddTransaction()
    {
        List<string> PossibleCategories = new List<string>()
        {
            "Продукты",
            "Транспорт",
            "Развлечения",
            "Коммунальные платежи",
            "Зарплата",
            "Другое"
        };

        DateTime date;
        double amount;
        string category;
        string description;

        while (true)
        {
            IFormatProvider provider = CultureInfo.InvariantCulture;

            Console.WriteLine("Введите дату транзакции (2024-10-26): ");
            if (DateTime.TryParseExact(Console.ReadLine(), "yyyy-MM-dd", provider, DateTimeStyles.None, out date))
            {
                break;
            }
            Console.WriteLine("Введите корректную дату транзакции");
        }


        while (true)
        {
            Console.WriteLine("Введите сумму транзакции: ");
            if (double.TryParse(Console.ReadLine(), out amount))
            {
                break;
            }
            Console.WriteLine("Введите корректное сумму транзакции");
        }

        while (true)
        {
            Console.WriteLine("Выберите категорию: ");
            for (int i = 0; i < PossibleCategories.Count; i++)
            {
                Console.WriteLine((i + 1) + ". " + PossibleCategories[i]);
            }
            if (int.TryParse(Console.ReadLine(), out int choice) && (choice >= 1) && (choice <= 6))
            {
                category = PossibleCategories[choice - 1];
                break;
            }
            Console.WriteLine("Введите корректный номер категории");
        }

        while (true)
        {
            Console.WriteLine("Введите описание транзакции: ");
            description = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(description))
            {
                break;
            }
            Console.WriteLine("Введите не пустое описание транзакции");
        }

        var transaction = new Transaction()
        {
            Date = date,
            Amount = amount,
            Category = category,
            Description = description
        };

        _transactionsProcessor.transactions.Add(transaction);
    }
}