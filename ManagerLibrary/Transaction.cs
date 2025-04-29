using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManagerLibrary
{
    /// <summary>
    /// Класс, представляющий транзакцию с датой, категорией, суммой и описанием.
    /// </summary>
    public class Transaction
    {
        /// <summary>
        /// Статический список возможных категорий для транзакций.
        /// </summary>
        public static List<string> PossibleCategories { get; set; }

        /// <summary>
        /// Дата транзакции.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Категория транзакции.
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Сумма транзакции.
        /// </summary>
        public double Amount { get; set; }

        /// <summary>
        /// Описание транзакции.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Конструктор по умолчанию. Инициализирует категорию как "Другое" и задает список возможных категорий.
        /// </summary>
        public Transaction()
        {
            Category = "Другое";

            PossibleCategories = new List<string>()
            {
                "Продукты",
                "Транспорт",
                "Развлечения",
                "Коммунальные платежи",
                "Зарплата",
                "Другое",
            };
        }

        /// <summary>
        /// Возвращает строковое представление транзакции в формате: "Дата | Категория | Сумма | Описание".
        /// </summary>
        /// <returns>Строковое представление транзакции.</returns>
        public override string ToString()
        {
            return $"{Date.ToString("yyyy-MM-dd")} | {Category} | {Amount} | {Description}";
        }
    }
}
