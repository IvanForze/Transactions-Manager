# Telegram-бот для управления транзакциями

**Дополнение к основному проекту**  
Бот предоставляет функционал управления транзакциями, бюджетирования и аналитики через Telegram. 
Название бота в телеграмме: @transactionsproject_bot

---

## Особенности бота
- **Интерактивное меню** с кнопками для навигации
- **Визуализация данных**:
  - Таблицы транзакций (`GenerateTableImage`)
  - Диаграммы расходов (`GenerateExpensesDiagramImage`)
  - Графики бюджета (`GenerateBudgetTableImage`)
  - Прогнозы (`GenerateForecastTableImage`)
- **Работа с файлами**: загрузка транзакций в формате `.txt`
- **Фильтрация и сортировка**:
  - По дате, сумме, категории
  - Возрастающий/убывающий порядок
- **Интеграция с основным проектом**: используется `TransactionsProcessor` и `TrendAnalysis`

---

### 1. Требования
- .NET 7.0+
- Токен Telegram-бота

### 2. Установка зависимостей
```bash
dotnet add package Telegram.Bot
dotnet add package SkiaSharp
dotnet add package ScottPlot