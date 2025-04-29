using ManagerLibrary;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Transaction = ManagerLibrary.Transaction;

namespace KotovProject3_2
{
    /// <summary>
    /// Класс для обработки транзакций, включая фильтрацию, сортировку, удаление и прогнозирование бюджета.
    /// </summary>
    public class TransactionsProcessor
    {
        /// <summary>
        /// Список транзакций.
        /// </summary>
        public List<Transaction> transactions { get; set; }

        /// <summary>
        /// Словарь для хранения бюджета по категориям.
        /// </summary>
        public Dictionary<string, double> TransactionsBudget = new Dictionary<string, double>();

        /// <summary>
        /// Конструктор класса TransactionsProcessor. Инициализирует список транзакций и устанавливает начальные значения бюджета.
        /// </summary>
        public TransactionsProcessor()
        {
            transactions = new List<Transaction>();
            TransactionsBudget = new Dictionary<string, double>()
            {
                { "Продукты", 0 },
                { "Транспорт", 0 },
                { "Развлечения", 0 },
                { "Коммунальные платежи", 0 },
                { "Зарплата", 0 },
                { "Другое", 0 }
            };
        }
        /// <summary>
        /// Фильтрует транзакции по указанному полю и значению.
        /// </summary>
        /// <param name="field">Поле для фильтрации (Дата, Сумма, Категория).</param>
        /// <param name="value">Значение для фильтрации.</param>
        public void FilterTransactions(string field, string value)
        {
            transactions = field switch
            {
                "Дата" => transactions.FindAll(t => t.Date.ToString() == value),
                "Сумма" => transactions.FindAll(t => t.Amount.ToString() == value),
                "Категория" => transactions.FindAll(t => t.Category == value),
                _ => throw new ArgumentException("Неверное поле")
            };
        }

        /// <summary>
        /// Сортирует транзакции по указанному полю и порядку.
        /// </summary>
        /// <param name="field">Поле для сортировки (Дата, Сумма, Категория).</param>
        /// <param name="order">Порядок сортировки (По возрастанию, По убыванию).</param>
        public void SortTransactions(string field, string order)
        {
            if (order == "По возрастанию")
            {
                transactions = field switch
                {
                    "Дата" => transactions.OrderBy(t => t.Date).ToList(),
                    "Сумма" => transactions.OrderBy(t => t.Amount).ToList(),
                    "Категория" => transactions.OrderBy(t => t.Category).ToList()
                };
            }
            else
            {
                transactions = field switch
                {
                    "Дата" => transactions.OrderByDescending(t => t.Date).ToList(),
                    "Сумма" => transactions.OrderByDescending(t => t.Amount).ToList(),
                    "Категория" => transactions.OrderByDescending(t => t.Category).ToList()
                };
            }
        }

        /// <summary>
        /// Удаляет транзакцию по указанному индексу.
        /// </summary>
        /// <param name="index">Индекс транзакции для удаления.</param>
        public void DeleteTransaction(int index)
        {
            try
            {
                transactions.RemoveAt(index);
                Console.WriteLine($"Транзакция с индексом {index} была удалена.");
            }
            catch (ArgumentOutOfRangeException)
            {
                Console.WriteLine("Введите корректный индекс транзакции.");
            }
        }

        /// <summary>
        /// Изменяет бюджет для указанной категории.
        /// </summary>
        /// <param name="category">Категория, для которой изменяется бюджет.</param>
        /// <param name="amount">Новое значение бюджета.</param>
        public void ChangeTransactionsBudget(string category, double amount)
        {
            TransactionsBudget[category] = amount;
        }

        /// <summary>
        /// Рассчитывает прогноз бюджета на основе данных за последние три месяца.
        /// </summary>
        /// <returns>Словарь с прогнозом расходов по категориям.</returns>
        public Dictionary<string, double> CalculateBudgetForecast()
        {
            var threeMonthsAgo = DateTime.Now.AddMonths(-3);

            var expensesByCategoryAndDate = transactions
                .Where(t => t.Amount < 0 && (t.Date >= threeMonthsAgo))
                .GroupBy(t => t.Category)
                .ToDictionary(
                    g => g.Key, // Ключ — категория
                    g =>
                    {
                        // Определяем количество уникальных месяцев для текущей категории
                        var uniqueMonths = g.Select(t => new { t.Date.Month }).Distinct().Count();
                        // Вычисляем среднее арифметическое: сумма расходов / количество месяцев
                        return g.Sum(t => t.Amount) / uniqueMonths;
                    }
                );

            return expensesByCategoryAndDate;
        }
    }
}