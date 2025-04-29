using System.Globalization;

namespace ManagerLibrary
{
    /// <summary>
    /// Класс для обработки файлов, включая чтение, парсинг и сохранение транзакций.
    /// </summary>
    public class FilesProcessor
    {
        /// <summary>
        /// Путь к файлу для обработки.
        /// </summary>
        public string filePath { get; set; }

        /// <summary>
        /// Читает содержимое файла и возвращает список строк.
        /// </summary>
        /// <returns>Список строк, прочитанных из файла.</returns>
        public List<string> ReadFile()
        {
            List<string> file = new List<string>();
            try
            {
                using (StreamReader fileStream = new StreamReader(filePath))
                {
                    // Перенаправляем стандартный ввод на поток чтения из файла.
                    Console.SetIn(fileStream);

                    // Читаем строку за строкой, пока не достигнем конца файла.
                    string line;
                    while ((line = Console.ReadLine()) != null)
                    {
                        file.Add(line);
                    }
                }

                // Восстанавливаем стандартный ввод.
                Console.SetIn(new StreamReader(Console.OpenStandardInput()));

                return file;
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"Файл не найден: {ex.Message}");
                return file;
            }
            catch (DirectoryNotFoundException ex)
            {
                Console.WriteLine($"Директория не найдена: {ex.Message}");
                return file;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Ошибка ввода-вывода: {ex.Message}");
                return file;
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Отсутствуют права доступа к файлу: {ex.Message}");
                return file;
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Некорректный аргумент: {ex.Message}");
                return file;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Произошла непредвиденная ошибка: {ex.Message}");
                return file;
            }
        }

        /// <summary>
        /// Парсит строки файла и добавляет транзакции в указанный список.
        /// </summary>
        /// <param name="file">Список строк, содержащих данные о транзакциях.</param>
        /// <param name="transactions">Список транзакций, в который будут добавлены данные.</param>
        public void ParseTransactions(List<string> file, List<Transaction> transactions)
        {
            foreach (string line in file)
            {
                Console.WriteLine(line); // Выводим строку для отладки
                try
                {
                    // Убираем квадратные скобки и разбиваем строку на части
                    string[] parts = line.Trim('[', ']').Split("] [");

                    // Проверяем, что строка содержит достаточно частей
                    if (parts.Length < 4 || parts.Length > 5)
                    {
                        Console.WriteLine($"Некорректный формат строки: {line}");
                        continue; // Пропускаем эту строку и переходим к следующей
                    }

                    // Парсим данные из строки
                    IFormatProvider provider = CultureInfo.InvariantCulture;

                    Transaction transaction = new Transaction()
                    {
                        Date = DateTime.ParseExact(parts[0], "yyyy-MM-dd", provider),
                        Amount = double.Parse(parts[1]),
                        Category = parts[2],
                        Description = parts[3]
                    };

                    transactions.Add(transaction); // Добавляем транзакцию в список
                }
                catch (FormatException ex)
                {
                    Console.WriteLine($"Ошибка формата данных в строке: {line}. {ex.Message}");
                }
                catch (IndexOutOfRangeException ex)
                {
                    Console.WriteLine($"Недостаточно данных в строке: {line}. {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Произошла непредвиденная ошибка при обработке строки: {line}. {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Сохраняет список транзакций в файл.
        /// </summary>
        /// <param name="transactions">Список транзакций для сохранения.</param>
        public void SaveTransactions(List<Transaction> transactions)
        {
            try
            {
                var lines = new List<string>();
                foreach (var transaction in transactions)
                {
                    lines.Add($"[{transaction.Date.ToString("yyyy-MM-dd")}] [{transaction.Amount}] [{transaction.Category}] [{transaction.Description}]");
                }

                // Сохраняем данные в файл
                File.WriteAllLines(filePath, lines);
                Console.WriteLine("Данные успешно сохранены.");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Ошибка доступа: Нет прав для записи в файл {filePath}. {ex.Message}");
            }
            catch (DirectoryNotFoundException ex)
            {
                Console.WriteLine($"Ошибка: Директория для сохранения файла не найдена. {ex.Message}");
            }
            catch (PathTooLongException ex)
            {
                Console.WriteLine($"Ошибка: Путь к файлу слишком длинный. {ex.Message}");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Ошибка ввода-вывода при сохранении файла. {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Произошла непредвиденная ошибка при сохранении файла. {ex.Message}");
            }
        }
    }
}
