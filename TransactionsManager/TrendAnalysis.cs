using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScottPlot;
using ScottPlot.TickGenerators.TimeUnits;
using Transaction = ManagerLibrary.Transaction;


namespace KotovProject3_2
{
    /// <summary>
    /// Класс для анализа трендов и визуализации данных с использованием библиотеки ScottPlot.
    /// </summary>
    public class TrendAnalysis
    {
        /// <summary>
        /// Создает и сохраняет графики для анализа трендов.
        /// </summary>
        /// <param name="transactions">Список транзакций для анализа.</param>
        /// <returns>Список путей к сохраненным графикам.</returns>
        public List<string> DrawScottPlots(List<Transaction> transactions)
        {
            List<string> plotsPaths = new List<string>();

            string currentDir = Directory.GetCurrentDirectory(); // Получаем путь к папке с решением (Solution).

            DrawExpensesAndIncomesPlot(transactions);
            Console.WriteLine();
            Console.WriteLine("График расходов и доходов по месяцам был сохранен в:");
            Console.WriteLine(currentDir + "\\monthly_expenses_income.png");
            plotsPaths.Add(currentDir + "\\monthly_expenses_income.png");

            DrawExpensesByCategoriesPlot(transactions);
            Console.WriteLine();
            Console.WriteLine("Гистограмма расходов по категориям была сохранена в:");
            Console.WriteLine(currentDir + "\\expenses_by_category.png");
            plotsPaths.Add(currentDir + "\\expenses_by_category.png");

            DrawExpensesByCategoryPieChart(transactions);
            Console.WriteLine();
            Console.WriteLine("Круговая диаграмма распределения расходов была сохранена в:");
            Console.WriteLine(currentDir + "\\expenses_pie_chart.png");
            plotsPaths.Add(currentDir + "\\expenses_pie_chart.png");

            DrawSavingsPlot(transactions);
            Console.WriteLine();
            Console.WriteLine("График накоплений в каждый день был сохранен в:");
            Console.WriteLine(currentDir + "\\days_savings.png");
            plotsPaths.Add(currentDir + "\\days_savings.png");

            return plotsPaths;
        }

        /// <summary>
        /// Создает график расходов и доходов по месяцам.
        /// </summary>
        /// <param name="transactions">Список транзакций для анализа.</param>
        public void DrawExpensesAndIncomesPlot(List<Transaction> transactions)
        {
            var monthlyData = transactions
                .GroupBy(t => new { t.Date.Year, t.Date.Month })
                .Select(g => new
                {
                    Month = new DateTime(g.Key.Year, g.Key.Month, 1),
                    Expenses = Math.Abs(g.Where(t => t.Amount < 0).Sum(t => t.Amount)),
                    Income = g.Where(t => t.Amount > 0).Sum(t => t.Amount)
                })
                .OrderBy(x => x.Month)
                .ToList();

            // Подготовка данных для графика
            DateTime[] months = monthlyData.Select(x => x.Month).ToArray();
            double[] expenses = monthlyData.Select(x => (double)x.Expenses).ToArray();
            double[] income = monthlyData.Select(x => (double)x.Income).ToArray();

            // Создание графика
            Plot plt = new();
            plt.Title("График расходов и доходов по месяцам");
            plt.XLabel("Месяц");
            plt.YLabel("Сумма (руб)");

            // Добавление линий для расходов и доходов
            var expensesLine = plt.Add.Scatter(months, expenses);
            var incomeLine = plt.Add.Scatter(months, income);

            // Настройка меток оси X
            var axis = plt.Axes.DateTimeTicksBottom();
            plt.Axes.Bottom.Label.Text = "Месяц";

            // ЭТО КОД ИЗ ОФИЦ. ДОКУМЕНТАЦИИ
            // create a custom formatter to return a string with
            // date only when zoomed out and time only when zoomed in
            static string CustomFormatter(DateTime dt)
            {
                bool isMidnight = dt is { Hour: 0, Minute: 0, Second: 0 };
                return isMidnight
                    ? DateOnly.FromDateTime(dt).ToString()
                    : TimeOnly.FromDateTime(dt).ToString();
            }

            // apply our custom tick formatter
            var tickGen = (ScottPlot.TickGenerators.DateTimeAutomatic)axis.TickGenerator;
            tickGen.LabelFormatter = CustomFormatter;

            plt.ShowLegend();

            // Сохранение графика
            plt.SavePng("monthly_expenses_income.png", 800, 600);

        }
        /// <summary>
        /// Создает гистограмму расходов по категориям.
        /// </summary>
        /// <param name="transactions">Список транзакций для анализа.</param>
        public void DrawExpensesByCategoriesPlot(List<Transaction> transactions)
        {
            // Группировка данных по категориям
            var expensesByCategory = transactions
                .Where(t => t.Amount < 0) // Фильтруем только расходы
                .GroupBy(t => t.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    Expenses = Math.Abs(g.Sum(t => t.Amount)) // Сумма расходов по категории
                })
                .OrderBy(x => x.Category)
                .ToList();

            // Подготовка данных для гистограммы
            string[] categories = expensesByCategory.Select(x => x.Category).ToArray();
            double[] expenses = expensesByCategory.Select(x => (double)x.Expenses).ToArray();

            // Создание графика
            Plot plt = new ();
            plt.Title("Гистограмма расходов по категориям");
            plt.XLabel("Категории");
            plt.YLabel("Сумма расходов (руб)");

            // Добавление гистограммы
            var barPlot = plt.Add.Bars(expenses);

            double[] positions = Enumerable.Range(0, categories.Length).Select(x => (double)x).ToArray();
            plt.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(positions, categories);
            plt.Axes.Margins(bottom: 0);

            plt.ShowLegend(); // Включение легенды

            // Сохранение графика
            plt.SavePng("expenses_by_category.png", 800, 600);
        }
        /// <summary>
        /// Создает круговую диаграмму распределения расходов по категориям.
        /// </summary>
        /// <param name="transactions">Список транзакций для анализа.</param>
        public void DrawExpensesByCategoryPieChart(List<Transaction> transactions)
        {
            // Группировка данных по категориям
            var expensesByCategory = transactions
                .Where(t => t.Amount < 0) // Фильтруем только расходы
                .GroupBy(t => t.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    Expenses = Math.Abs(g.Sum(t => t.Amount)) // Сумма расходов по категории
                })
                .OrderBy(x => x.Category)
                .ToList();


            Plot plt = new ();
            plt.Title("Круговая диаграмма распределения расходов");

            // Заданный набор цветов
            Color[] colors = new Color[]
            {
                Colors.Red,
                Colors.Orange,
                Colors.Gold,
                Colors.Green,
                Colors.Blue
            };

            // Создание списка сегментов круговой диаграммы
            List<PieSlice> slices = new List<PieSlice>();

            for (int i = 0; i < expensesByCategory.Count; i++)
            {
                var category = expensesByCategory[i];
                var color = colors[i % colors.Length];

                // Добавление сегмента с выбранным цветом
                slices.Add(new PieSlice()
                {
                    Value = category.Expenses,
                    FillColor = color,
                    Label = category.Category
                });
            }

            // Добавление круговой диаграммы
            var pie = plt.Add.Pie(slices);

            plt.Axes.Frameless();
            plt.HideGrid();

            // Сохранение графика
            plt.SavePng("expenses_pie_chart.png", 800, 600);
        }

        /// <summary>
        /// Создает график накоплений по дням.
        /// </summary>
        /// <param name="transactions">Список транзакций для анализа.</param>
        public void DrawSavingsPlot(List<Transaction> transactions)
        {
            // Группировка данных по дням
            var dailyData = transactions
                .GroupBy(t => t.Date.Date) // Группируем по дате
                .Select(g => new
                {
                    Date = g.Key,
                    NetAmount = g.Sum(t => t.Amount) // Сумма доходов и расходов за день
                })
                .OrderBy(x => x.Date)
                .ToList();

            // Подготовка данных для графика
            DateTime[] days = dailyData.Select(x => x.Date).ToArray();
            double[] netAmounts = dailyData.Select(x => (double)x.NetAmount).ToArray();

            // Вычисление накоплений
            double[] savings = new double[netAmounts.Length];
            double totalSavings = 0;
            for (int i = 0; i < netAmounts.Length; i++)
            {
                totalSavings += netAmounts[i];
                savings[i] = totalSavings;
            }

            Plot plt = new();
            plt.Title("График накоплений в каждый день");
            plt.XLabel("День");
            plt.YLabel("Сумма (руб)");

            var axis = plt.Axes.DateTimeTicksBottom();
            plt.Axes.Bottom.Label.Text = "День";

            // ЭТО КОД ИЗ ОФИЦ. ДОКУМЕНТАЦИИ
            // create a custom formatter to return a string with
            // date only when zoomed out and time only when zoomed in
            static string CustomFormatter(DateTime dt)
            {
                bool isMidnight = dt is { Hour: 0, Minute: 0, Second: 0 };
                return isMidnight
                    ? DateOnly.FromDateTime(dt).ToString()
                    : TimeOnly.FromDateTime(dt).ToString();
            }

            // apply our custom tick formatter
            var tickGen = (ScottPlot.TickGenerators.DateTimeAutomatic)axis.TickGenerator;
            tickGen.LabelFormatter = CustomFormatter;

            var savingsLine = plt.Add.Scatter(days, savings);
            plt.ShowLegend();

            // Сохранение графика
            plt.SavePng("days_savings.png", 800, 600);
        }
    }

}
