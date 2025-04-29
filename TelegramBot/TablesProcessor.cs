using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ManagerLibrary;

namespace MyTelegramBot
{
    /// <summary>
    /// Класс для обработки и генерации изображений таблиц и диаграмм с использованием библиотеки SkiaSharp.
    /// </summary>
    public class TablesProcessor
    {
        /// <summary>
        /// Генерирует изображение таблицы с транзакциями.
        /// </summary>
        /// <param name="transactions">Список транзакций для отображения в таблице.</param>
        /// <returns>Путь к сохраненному изображению.</returns>
        public string GenerateTableImage(List<Transaction> transactions)
        {
            // Настройки шрифта
            var fontPath = "arial.ttf"; // Укажите путь к файлу шрифта
            var typeface = SKTypeface.FromFile(fontPath);
            var font = new SKFont(typeface, 14);
            var textPaint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 14,
                Typeface = typeface,
                IsAntialias = true
            };

            // Заголовки таблицы
            string[] headers = { "№", "Дата", "Сумма", "Категория", "Описание" };

            // Подготовка данных таблицы
            var tableData = new List<string[]>();
            for (int i = 0; i < transactions.Count; i++)
            {
                var transaction = transactions[i];
                tableData.Add(new[]
                {
                    (i + 1).ToString(),
                    transaction.Date.ToString("yyyy-MM-dd"),
                    transaction.Amount.ToString(),
                    transaction.Category,
                    transaction.Description
                });
            }

            // Рассчитываем ширину столбцов
            var columnWidths = new int[headers.Length];
            for (int j = 0; j < headers.Length; j++)
            {
                // Ширина заголовка
                var headerWidth = (int)textPaint.MeasureText(headers[j]);
                // Ширина данных в столбце
                var maxDataWidth = tableData.Max(row => (int)textPaint.MeasureText(row[j]));
                // Общая ширина столбца (заголовок + данные + отступы)
                columnWidths[j] = Math.Max(headerWidth, maxDataWidth) + 20; // +20 для отступов
            }

            // Настройки таблицы
            int rowHeight = 30;
            int padding = 20;

            // Рассчитываем размеры изображения
            int imageWidth = columnWidths.Sum() + padding * 2;
            int imageHeight = rowHeight * (tableData.Count + 1) + padding * 2; // +1 для заголовка

            // Создаем изображение
            var info = new SKImageInfo(imageWidth, imageHeight);
            using var surface = SKSurface.Create(info);
            var canvas = surface.Canvas;

            // Заполняем фон
            canvas.Clear(SKColors.White);

            // Рисуем заголовок таблицы
            for (int j = 0; j < headers.Length; j++)
            {
                var x = columnWidths.Take(j).Sum() + padding;
                var y = padding;

                // Рисуем текст заголовка
                canvas.DrawText(headers[j], x + 10, y + 20, font, textPaint);

                // Рисуем границы заголовка
                var borderPaint = new SKPaint
                {
                    Color = SKColors.Gray,
                    StrokeWidth = 1,
                    IsStroke = true
                };
                canvas.DrawRect(x, y, columnWidths[j], rowHeight, borderPaint);
            }

            // Рисуем строки таблицы
            for (int i = 0; i < tableData.Count; i++)
            {
                var row = tableData[i];
                for (int j = 0; j < row.Length; j++)
                {
                    var x = columnWidths.Take(j).Sum() + padding;
                    var y = (i + 1) * rowHeight + padding;

                    // Рисуем текст
                    canvas.DrawText(row[j], x + 10, y + 20, font, textPaint);

                    // Рисуем границы ячеек
                    var borderPaint = new SKPaint
                    {
                        Color = SKColors.Gray,
                        StrokeWidth = 1,
                        IsStroke = true
                    };
                    canvas.DrawRect(x, y, columnWidths[j], rowHeight, borderPaint);
                }
            }

            // Сохраняем изображение
            var filePath = Path.GetTempFileName() + ".png";
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = File.OpenWrite(filePath);
            data.SaveTo(stream);

            return filePath;
        }

        /// <summary>
        /// Генерирует изображение диаграммы расходов по категориям.
        /// </summary>
        /// <param name="transactions">Список транзакций для анализа расходов.</param>
        /// <returns>Путь к сохраненному изображению.</returns>
        public string GenerateExpensesDiagramImage(List<Transaction> transactions)
        {
            // Группируем транзакции по категориям
            var categories = transactions
                .Where(t => t.Amount < 0)
                .GroupBy(t => t.Category)
                .Select(g => new {
                    Name = g.Key,
                    Total = Math.Abs(g.Sum(t => t.Amount))
                })
                .OrderByDescending(c => c.Total)
                .ToList();

            // Настройки шрифта
            var fontPath = "arial.ttf";
            var typeface = SKTypeface.FromFile(fontPath);
            var font = new SKFont(typeface, 14);
            var textPaint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 14,
                Typeface = typeface,
                IsAntialias = true
            };

            // Настройки диаграммы
            int barHeight = 30;
            int padding = 40;
            int maxBarWidth = 400;
            int textSpacing = 10;
            var colors = new[] { SKColors.Blue, SKColors.Green, SKColors.Orange, SKColors.Red };

            // Рассчитываем максимальную ширину текста категорий
            float maxCategoryTextWidth = categories
                .Select(c => textPaint.MeasureText(c.Name))
                .Max();

            // Рассчитываем ширину изображения
            int imageWidth = padding * 2 + maxBarWidth + (int)maxCategoryTextWidth + 150;

            // Рассчитываем высоту изображения
            int imageHeight = padding * 2 + (barHeight + 10) * categories.Count;

            // Создаем изображение
            var info = new SKImageInfo(imageWidth, imageHeight);
            using var surface = SKSurface.Create(info);
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            // Находим максимальную сумму для масштабирования
            var maxTotal = categories.Max(c => c.Total);

            // Рисуем диаграмму
            for (int i = 0; i < categories.Count; i++)
            {
                var category = categories[i];
                var yPosition = padding + i * (barHeight + 10);

                // Рисуем название категории
                canvas.DrawText(category.Name, padding, yPosition + barHeight - 5, font, textPaint);

                // Рассчитываем ширину столбца
                var barWidth = (float)(maxBarWidth * (category.Total / maxTotal));

                // Рисуем столбец диаграммы
                var barPaint = new SKPaint
                {
                    Color = colors[i % colors.Length],
                    Style = SKPaintStyle.Fill
                };
                canvas.DrawRect(padding + maxCategoryTextWidth + textSpacing, yPosition, barWidth, barHeight, barPaint);

                // Рисуем сумму справа
                var sumText = category.Total.ToString("N2"); // Форматируем сумму с двумя знаками после запятой
                canvas.DrawText(sumText, padding + maxCategoryTextWidth + textSpacing + maxBarWidth + textSpacing,
                               yPosition + barHeight - 5, font, textPaint);
            }

            // Сохраняем изображение
            var filePath = Path.GetTempFileName() + ".png";
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = File.OpenWrite(filePath);
            data.SaveTo(stream);

            return filePath;
        }

        /// <summary>
        /// Генерирует изображение таблицы бюджета с фактическими расходами и установленными лимитами.
        /// </summary>
        /// <param name="transactions">Список транзакций для анализа.</param>
        /// <param name="transactionsBudget">Словарь с установленными лимитами бюджета по категориям.</param>
        /// <returns>Путь к сохраненному изображению.</returns>
        public string GenerateBudgetTableImage(List<Transaction> transactions, Dictionary<string, double> transactionsBudget)
        {
            // Получаем фактические расходы по категориям
            var expenses = transactions
                .Where(t => t.Amount < 0)
                .GroupBy(t => t.Category)
                .Select(g => new {
                    Category = g.Key,
                    Total = Math.Abs(g.Sum(t => t.Amount))
                });

            // Объединяем с бюджетом
            var budgetData = expenses
                .Select(e => new {
                    e.Category,
                    Total = e.Total,
                    Budget = transactionsBudget.TryGetValue(e.Category, out var budget)
                        ? (decimal)budget
                        : 0m
                })
                .OrderByDescending(x => x.Total)
                .ToList();

            // Настройки шрифта
            var fontPath = "arial.ttf";
            var typeface = SKTypeface.FromFile(fontPath);
            var font = new SKFont(typeface, 14);
            var textPaint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 14,
                Typeface = typeface,
                IsAntialias = true
            };

            // Заголовки таблицы
            string[] headers = { "№", "Категория", "Бюджет", "Факт. расход", "Статус" };

            // Рассчитываем ширину столбцов
            var columnWidths = new int[headers.Length];
            for (int j = 0; j < headers.Length; j++)
            {
                var maxWidth = (int)textPaint.MeasureText(headers[j]);

                foreach (var item in budgetData.Select((c, i) => new { c, i }))
                {
                    var status = item.c.Total > (double)item.c.Budget ? "Превышен" : "В норме";
                    var values = new[] {
                        (item.i + 1).ToString(),
                        item.c.Category,
                        item.c.Budget.ToString(),
                        item.c.Total.ToString(),
                        status
                    };

                    maxWidth = Math.Max(maxWidth, (int)textPaint.MeasureText(values[j]));
                }

                columnWidths[j] = maxWidth + 20;
            }

            // Настройки таблицы
            int rowHeight = 30;
            int padding = 20;
            int imageWidth = columnWidths.Sum() + padding * 2;
            int imageHeight = rowHeight * (budgetData.Count + 1) + padding * 2;

            // Создаем изображение
            var info = new SKImageInfo(imageWidth, imageHeight);
            using var surface = SKSurface.Create(info);
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            // Рисуем заголовки
            for (int j = 0; j < headers.Length; j++)
            {
                var x = columnWidths.Take(j).Sum() + padding;
                var y = padding;

                // Текст заголовка
                textPaint.Color = SKColors.Black;
                canvas.DrawText(headers[j], x + 10, y + 20, font, textPaint);

                // Границы
                var borderPaint = new SKPaint
                {
                    Color = SKColors.Gray,
                    StrokeWidth = 1,
                    IsStroke = true
                };
                canvas.DrawRect(x, y, columnWidths[j], rowHeight, borderPaint);
            }

            // Рисуем строки данных
            for (int i = 0; i < budgetData.Count; i++)
            {
                var item = budgetData[i];
                var status = item.Total > (double)item.Budget ? "Превышен" : "В норме";
                var fields = new[] {
                    (i + 1).ToString(),
                    item.Category,
                    item.Budget.ToString(),
                    item.Total.ToString(),
                    status
                };

                for (int j = 0; j < fields.Length; j++)
                {
                    var x = columnWidths.Take(j).Sum() + padding;
                    var y = (i + 1) * rowHeight + padding;

                    // Устанавливаем цвет
                    textPaint.Color = j switch
                    {
                        3 when item.Total > (double)item.Budget => SKColors.Red,
                        3 => SKColors.Green,
                        4 when status == "Превышен" => SKColors.Red,
                        4 => SKColors.Green,
                        _ => SKColors.Black
                    };

                    canvas.DrawText(fields[j], x + 10, y + 20, font, textPaint);

                    // Границы
                    var borderPaint = new SKPaint
                    {
                        Color = SKColors.Gray,
                        StrokeWidth = 1,
                        IsStroke = true
                    };
                    canvas.DrawRect(x, y, columnWidths[j], rowHeight, borderPaint);
                }
            }

            // Сохраняем изображение
            var filePath = Path.GetTempFileName() + ".png";
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = File.OpenWrite(filePath);
            data.SaveTo(stream);

            return filePath;
        }

        /// <summary>
        /// Генерирует изображение таблицы с прогнозом расходов по категориям.
        /// </summary>
        /// <param name="forecastData">Словарь с прогнозами расходов по категориям.</param>
        /// <returns>Путь к сохраненному изображению.</returns>
        public string GenerateForecastTableImage(Dictionary<string, double> forecastData)
        {
            // Настройки шрифта
            var fontPath = "arial.ttf";
            var typeface = SKTypeface.FromFile(fontPath);
            var font = new SKFont(typeface, 14);
            var textPaint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 14,
                Typeface = typeface,
                IsAntialias = true
            };

            // Заголовки таблицы
            string[] headers = { "№", "Категория", "Прогноз" };

            // Подготовка данных
            var data = forecastData
                .Select(kvp => new {
                    Category = kvp.Key,
                    Monthly = Math.Abs(kvp.Value)
                })
                .OrderByDescending(x => x.Monthly)
                .ToList();

            // Рассчитываем ширину столбцов (только для 3 колонок)
            var columnWidths = new int[headers.Length];
            for (int j = 0; j < headers.Length; j++)
            {
                var maxWidth = (int)textPaint.MeasureText(headers[j]);

                foreach (var item in data.Select((x, i) => new { x, i }))
                {
                    var values = new[] {
                        (item.i + 1).ToString(),
                        item.x.Category,
                        item.x.Monthly.ToString()
                    };

                    maxWidth = Math.Max(maxWidth, (int)textPaint.MeasureText(values[j]));
                }

                columnWidths[j] = maxWidth + 20;
            }

            // Настройки таблицы
            int rowHeight = 30;
            int padding = 20;
            int imageWidth = columnWidths.Sum() + padding * 2;
            int imageHeight = rowHeight * (data.Count + 1) + padding * 2;

            // Создаем изображение
            var info = new SKImageInfo(imageWidth, imageHeight);
            using var surface = SKSurface.Create(info);
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            // Рисуем заголовки (только 3 колонки)
            for (int j = 0; j < headers.Length; j++)
            {
                var x = columnWidths.Take(j).Sum() + padding;
                var y = padding;

                canvas.DrawText(headers[j], x + 10, y + 20, font, textPaint);

                var borderPaint = new SKPaint
                {
                    Color = SKColors.Gray,
                    StrokeWidth = 1,
                    IsStroke = true
                };
                canvas.DrawRect(x, y, columnWidths[j], rowHeight, borderPaint);
            }

            // Рисуем строки данных (только 3 значения)
            for (int i = 0; i < data.Count; i++)
            {
                var item = data[i];
                var fields = new[] {
                    (i + 1).ToString(),
                    item.Category,
                    item.Monthly.ToString("F2")
                };

                for (int j = 0; j < fields.Length; j++)
                {
                    var x = columnWidths.Take(j).Sum() + padding;
                    var y = (i + 1) * rowHeight + padding;

                    canvas.DrawText(fields[j], x + 10, y + 20, font, textPaint);

                    var borderPaint = new SKPaint
                    {
                        Color = SKColors.Gray,
                        StrokeWidth = 1,
                        IsStroke = true
                    };
                    canvas.DrawRect(x, y, columnWidths[j], rowHeight, borderPaint);
                }
            }

            // Сохраняем изображение
            var filePath = Path.GetTempFileName() + ".png";
            using var image = surface.Snapshot();
            using var dataStream = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = File.OpenWrite(filePath);
            dataStream.SaveTo(stream);

            return filePath;
        }
    }
}