using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ManagerLibrary;
using Spectre.Console;
using Transaction = ManagerLibrary.Transaction;

namespace KotovProject3_2
{
    /// <summary>
    /// Класс для работы с консольным интерфейсом с использованием библиотеки Spectre.Console.
    /// </summary>
    public class SpectreConsole
    {
        private TransactionsProcessor _transactionsProcessor;

        /// <summary>
        /// Конструктор класса SpectreConsole. Инициализирует объект TransactionsProcessor.
        /// </summary>
        /// <param name="_transactionsProcessor">Объект для обработки транзакций.</param>
        public SpectreConsole(TransactionsProcessor _transactionsProcessor)
        {
            this._transactionsProcessor = _transactionsProcessor;
        }
        /// <summary>
        /// Отображает таблицу транзакций с возможностью фильтрации, сортировки и просмотра диаграммы расходов.
        /// </summary>
        public void DrawTransactionsTable()
        {
            if (_transactionsProcessor.transactions.Count == 0) // Проверка на наличие данных
            {
                Console.WriteLine("Нет данных. Сначала введите данные через меню");
                return; // Выход из метода, если данных нет
            }

            while (true)
            {
                AnsiConsole.Clear();
                var table = new Table()
                    .AddColumn(new TableColumn("№").Centered())
                    .AddColumn(new TableColumn("Дата").Centered())
                    .AddColumn(new TableColumn("Сумма").Centered())
                    .AddColumn(new TableColumn("Категория").Centered())
                    .AddColumn(new TableColumn("Описание").Centered());

                for (int i = 0; i < _transactionsProcessor.transactions.Count; i++)
                {
                    var transaction = _transactionsProcessor.transactions[i];
                    table.AddRow(
                        (i + 1).ToString(),
                        transaction.Date.ToString("yyyy-MM-dd"),
                        transaction.Amount.ToString(),
                        transaction.Category,
                        transaction.Description
                    );
                }

                AnsiConsole.Write(table);

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Выберите действие:")
                        .AddChoices(new[] { "Фильтровать", "Сортировать", "Диаграмма расходов", "Выход" })
                );

                switch (choice)
                {
                    case "Фильтровать":
                        var field = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("Выберите категорию для фильтрации:")
                            .AddChoices(new[] { "Дата", "Сумма", "Категория" })
                        );


                        List<string> values = field switch
                        {
                            "Дата" => _transactionsProcessor.transactions.Select(t => t.Date.ToString()).Distinct().ToList(),
                            "Сумма" => _transactionsProcessor.transactions.Select(t => t.Amount.ToString()).Distinct().ToList(),
                            "Категория" => _transactionsProcessor.transactions.Select(t => t.Category).Distinct().ToList(),
                            _ => throw new ArgumentException("Неверное поле")
                        };

                        var value = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("Выберите значение для фильтрации:")
                            .AddChoices(values)
                        );

                        _transactionsProcessor.FilterTransactions(field, value);
                        break;
                    case "Сортировать":
                        field = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("Выберите категорию для сортировки:")
                            .AddChoices(new[] { "Дата", "Сумма", "Категория" })
                        );

                        var order = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("Выберите порядок сортировки:")
                            .AddChoices(new[] { "По возрастанию", "По убыванию" })
                        );
                        _transactionsProcessor.SortTransactions(field, order);
                        break;
                    case "Диаграмма расходов":
                        DrawExpensesPieChart(_transactionsProcessor.transactions);
                        break;
                    case "Выход":
                        return;
                }
            }
        }
        /// <summary>
        /// Отображает круговую диаграмму расходов по категориям.
        /// </summary>
        /// <param name="transactions">Список транзакций для анализа.</param>
        public void DrawExpensesPieChart(List<Transaction> transactions)
        {
            if (transactions.Count == 0) // Проверка на наличие данных
            {
                Console.WriteLine("Нет данных. Сначала введите данные через меню");
                return; // Выход из метода, если данных нет
            }

            var expensesByCategory = transactions
            .Where(t => t.Amount < 0)
            .GroupBy(t => t.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(t => t.Amount) })
            .ToList();

            var barChart = new BarChart()
                .Width(60)
                .Label("[green]Распределение расходов по категориям[/]")
                .CenterLabel();

            foreach (var item in expensesByCategory)
            {
                barChart.AddItem(item.Category, Math.Abs(item.Total), Color.Red);
            }

            AnsiConsole.Write(barChart);
            AnsiConsole.WriteLine("Нажмите любую клавишу для продолжения...");
            Console.ReadKey();
        }
        /// <summary>
        /// Отображает таблицу бюджета с возможностью установки лимитов расходов.
        /// </summary>
        /// <param name="transactions">Список транзакций для анализа.</param>
        public void DrawBudgetTable(List<Transaction> transactions)
        {
            if (transactions.Count == 0) // Проверка на наличие данных
            {
                Console.WriteLine("Нет данных. Сначала введите данные через меню");
                return; // Выход из метода, если данных нет
            }

            var expensesByCategory = transactions
            .Where(t => t.Amount < 0)
            .GroupBy(t => t.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(t => t.Amount) })
            .ToList();

            var transactionsBudget = _transactionsProcessor.TransactionsBudget;
            while (true)
            {
                AnsiConsole.Clear();
                var table = new Table()
                    .AddColumn(new TableColumn("№").Centered())
                    .AddColumn(new TableColumn("Категория").Centered())
                    .AddColumn(new TableColumn("Бюджет").Centered())
                    .AddColumn(new TableColumn("Фактический расход").Centered());

                for (int i = 0; i < expensesByCategory.Count; i++)
                {
                    var realExpenses = Math.Abs(expensesByCategory[i].Total);
                    var budgetExpenses = transactionsBudget[expensesByCategory[i].Category];
                    var expenseMarkup = realExpenses > budgetExpenses
                        ? $"[red]{realExpenses}[/]" // Красный, если превышен бюджет
                        : $"[green]{realExpenses}[/]"; // Зеленый, если бюджет не превышен


                    table.AddRow(
                        (i + 1).ToString(),
                        expensesByCategory[i].Category,
                        budgetExpenses.ToString(),
                        expenseMarkup
                    );
                }


                AnsiConsole.Write(table);

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Выберите действие:")
                        .AddChoices(new[] { "Установить бюджет", "Выход" })
                );

                switch (choice)
                {
                    case "Установить бюджет":
                        var categories = transactions
                            .Where(t => t.Amount < 0)
                            .Select(t => t.Category)
                            .Distinct()
                            .ToList();

                        var category = AnsiConsole.Prompt(
                            new SelectionPrompt<string>()
                            .Title("Выберите категорию для фильтрации:")
                            .AddChoices(categories)
                        );
                        while (true)
                        {
                            var amount = AnsiConsole.Prompt(
                                new TextPrompt<double>($"Введите бюджет для {category}")
                            );
                            if (amount >= 0)
                            {
                                _transactionsProcessor.ChangeTransactionsBudget(category, amount);
                                break;
                            }
                            Console.WriteLine("Введите корректное значение бюджета > 0");
                        }
                        
                        break;
                    case "Выход":
                        return;
                }
            }
        }
        /// <summary>
        /// Отображает таблицу с прогнозом расходов по категориям.
        /// </summary>
        /// <param name="transactions">Список транзакций для анализа.</param>
        public void DrawBudgetForecastTable(List<Transaction> transactions)
        {

            if (transactions.Count == 0) // Проверка на наличие данных
            {
                Console.WriteLine("Нет данных. Сначала введите данные через меню");
                return; // Выход из метода, если данных нет
            }

            AnsiConsole.Clear();
            var table = new Table()
                .AddColumn(new TableColumn("№").Centered())
                .AddColumn(new TableColumn("Категория").Centered())
                .AddColumn(new TableColumn("Прогноз расходов").Centered());

            var expensesByCategoryAndDate = _transactionsProcessor.CalculateBudgetForecast();

            int counter = 1; // Счетчик для нумерации строк

            foreach (var item in expensesByCategoryAndDate)
            {
                table.AddRow(
                    counter.ToString(), // Номер строки
                    item.Key, // Категория
                    Math.Abs(item.Value).ToString("F2") // Сумма расходов
                );

                counter++;
            }

            AnsiConsole.Write(table);
        }
    }
}
