using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public interface IVolatilityCalculator
{
    double Calculate(IList<double> plannedEfforts, IList<double> actualEfforts);
}

public interface INormalDistribution
{
    double CumulativeDistribution(double x);
}

public class BlackScholesEstimator
{
    private readonly INormalDistribution _normalDistribution;
    
    public BlackScholesEstimator(INormalDistribution normalDistribution)
    {
        _normalDistribution = normalDistribution;
    }
    
    public double CalculateEffortEstimate(
        double currentEstimate, 
        double targetEffort,
        double timeToDeadline,
        double volatility,
        double riskFreeRate)
    {
        // Проверка и коррекция граничных значений
        if (timeToDeadline <= 0) return currentEstimate;
        if (volatility <= 0) volatility = 0.01; // Минимальное значение для избежания деления на 0
        
        double d1 = (Math.Log(currentEstimate / targetEffort) 
                    + (riskFreeRate + Math.Pow(volatility, 2) / 2) * timeToDeadline)
                    / (volatility * Math.Sqrt(timeToDeadline));
        
        double d2 = d1 - volatility * Math.Sqrt(timeToDeadline);
        
        double n1 = _normalDistribution.CumulativeDistribution(d1);
        double n2 = _normalDistribution.CumulativeDistribution(d2);
        
        return currentEstimate * n1 
               - targetEffort * Math.Exp(-riskFreeRate * timeToDeadline) * n2;
    }
}

public class HistoricalVolatilityCalculator : IVolatilityCalculator
{
    public double Calculate(IList<double> plannedEfforts, IList<double> actualEfforts)
    {
        if (plannedEfforts == null || actualEfforts == null)
            throw new ArgumentNullException("Input lists cannot be null");
        
        if (plannedEfforts.Count != actualEfforts.Count)
            throw new ArgumentException("Lists must have the same length");
        
        if (plannedEfforts.Count < 2)
            return 0.3; // Возвращаем значение по умолчанию при недостатке данных
        
        int n = plannedEfforts.Count;
        var relativeDeviations = new double[n];
        
        for (int i = 0; i < n; i++)
        {
            // Используем относительные отклонения вместо абсолютных
            relativeDeviations[i] = (actualEfforts[i] - plannedEfforts[i]) / plannedEfforts[i];
        }
        
        // Расчет стандартного отклонения относительных отклонений
        double meanDeviation = relativeDeviations.Average();
        double sumSquares = relativeDeviations.Sum(d => Math.Pow(d - meanDeviation, 2));
        double variance = sumSquares / (n - 1);
        double stdDev = Math.Sqrt(variance);
        
        // Возвращаем абсолютное значение
        return Math.Abs(stdDev);
    }
}

public class HartApproximationNormalDistribution : INormalDistribution
{
    private const double A1 = 0.319381530;
    private const double A2 = -0.356563782;
    private const double A3 = 1.781477937;
    private const double A4 = -1.821255978;
    private const double A5 = 1.330274429;
    private const double Gamma = 0.2316419;
    private static readonly double InvSqrt2Pi = 1.0 / Math.Sqrt(2 * Math.PI);
    
    public double CumulativeDistribution(double x)
    {
        if (x < -7.0) return 0.0;
        if (x > 7.0) return 1.0;
        
        int sign = 1;
        if (x < 0)
        {
            sign = -1;
            x = -x;
        }
        
        double t = 1.0 / (1.0 + Gamma * x);
        double poly = t * (A1 + t * (A2 + t * (A3 + t * (A4 + t * A5))));
        double pdf = InvSqrt2Pi * Math.Exp(-0.5 * x * x);
        
        double result = 1.0 - pdf * poly;
        
        return sign == 1 ? result : 1.0 - result;
    }
}

public class TaskEstimation
{
    public string TaskName { get; set; }
    public double CurrentEstimate { get; set; }
    public double TargetEffort { get; set; }
    public double TimeToDeadline { get; set; }
    public double RiskFreeRate { get; set; }
    public double Volatility { get; set; }
    public double CalculatedEffort { get; set; }
    public string Status { get; set; }
}

public class InteractiveEffortEstimator
{
    private readonly BlackScholesEstimator _estimator;
    private readonly IVolatilityCalculator _volatilityCalc;
    private readonly INormalDistribution _normalDist;
    private double _commonVolatility = 0.3;
    private bool _useCommonVolatility = true;

    public InteractiveEffortEstimator()
    {
        _normalDist = new HartApproximationNormalDistribution();
        _estimator = new BlackScholesEstimator(_normalDist);
        _volatilityCalc = new HistoricalVolatilityCalculator();
    }

    public void Run()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("📊 Оценка трудозатрат с помощью модели Блэка-Шоулза 📊");
        Console.WriteLine("=====================================================\n");

        // Выбор режима волатильности
        Console.WriteLine("🔢 Выберите режим волатильности для всех задач:");
        Console.WriteLine("1️⃣ - Общая волатильность для всех задач");
        Console.WriteLine("2️⃣ - Индивидуальная волатильность для каждой задачи");
        Console.Write("Ваш выбор: ");
        _useCommonVolatility = GetInput("", s => 
        {
            if (s == "1") return true;
            if (s == "2") return false;
            throw new FormatException();
        });

        if (_useCommonVolatility)
        {
            // Расчет общей волатильности
            Console.WriteLine("\n🔢 Выберите способ расчета общей волатильности:");
            Console.WriteLine("1️⃣ - Использовать исторические данные");
            Console.WriteLine("2️⃣ - Ввести готовое значение волатильности");
            Console.Write("Ваш выбор: ");
            _commonVolatility = GetInput("", s => 
            {
                if (s == "1") return 1;
                if (s == "2") return 2;
                throw new FormatException();
            }) == 1 ? CalculateVolatilityFromHistory() : GetVolatilityFromUser();
            Console.WriteLine($"\n📊 Рассчитанная общая волатильность: {_commonVolatility:P2}");
        }

        // Список задач для оценки
        var tasks = new List<TaskEstimation>();
        
        // Основной цикл ввода задач
        while (true)
        {
            Console.WriteLine("\n➕ ДОБАВЛЕНИЕ НОВОЙ ЗАДАЧИ");
            Console.WriteLine("-----------------------------------------------------");
            
            var task = new TaskEstimation();
            
            // Ввод названия задачи
            task.TaskName = GetStringInput("📝 Название задачи: ");
            
            // Ввод параметров задачи
            Console.WriteLine("\n🌟 Введите параметры задачи:");
            task.CurrentEstimate = GetInput("🔮 Текущая оценка трудозатрат (S) - начальное время на задачу (часы): ", Convert.ToDouble);
            task.TargetEffort = GetInput("🎯 Целевая трудозатрата (K) - максимальное время на задачу (часы): ", Convert.ToDouble);
            
            // Проверка на корректность времени
            task.TimeToDeadline = GetInput("⏱️ Время до дедлайна (T) - оставшееся время до сдачи (в годах): ", 
                s => {
                    double value = Convert.ToDouble(s);
                    if (value <= 0) throw new ArgumentException("Время должно быть положительным");
                    return value;
                });
            
            task.RiskFreeRate = GetInput("📈 Безрисковая ставка (r) - прирост производительности команды (например 0.05): ", Convert.ToDouble);
            
            // Ввод волатильности, если требуется индивидуальная
            if (!_useCommonVolatility)
            {
                Console.WriteLine("\n🔢 Выберите способ расчета волатильности для этой задачи:");
                Console.WriteLine("1️⃣ - Использовать исторические данные");
                Console.WriteLine("2️⃣ - Ввести готовое значение волатильности");
                Console.Write("Ваш выбор: ");
                task.Volatility = GetInput("", s => 
                {
                    if (s == "1") return 1;
                    if (s == "2") return 2;
                    throw new FormatException();
                }) == 1 ? CalculateVolatilityFromHistory() : GetVolatilityFromUser();
            }
            else
            {
                task.Volatility = _commonVolatility;
            }
            
            // Расчет и сохранение результатов
            task.CalculatedEffort = _estimator.CalculateEffortEstimate(
                task.CurrentEstimate,
                task.TargetEffort,
                task.TimeToDeadline,
                task.Volatility,
                task.RiskFreeRate);
            
            task.Status = task.CalculatedEffort < task.TargetEffort ? 
                "✅ Успех" : "⚠️ Риск";
            
            tasks.Add(task);
            
            // Запрос на добавление новой задачи
            Console.Write("\n❓ Добавить еще одну задачу? (да/нет): ");
            string answer = Console.ReadLine().Trim().ToLower();
            if (answer != "да" && answer != "д" && answer != "yes" && answer != "y")
                break;
        }
        
        if (tasks.Any())
        {
            // Вывод сводной таблицы результатов
            PrintSummaryTable(tasks);
            
            // Детальный анализ рисков
            PrintRiskAnalysis(tasks);
        }
        else
        {
            Console.WriteLine("\n🚫 Задачи не были добавлены. Программа завершена.");
        }
    }

    private void PrintSummaryTable(List<TaskEstimation> tasks)
    {
        Console.WriteLine("\n📊 СВОДНАЯ ТАБЛИЦА РЕЗУЛЬТАТОВ");
        Console.WriteLine("====================================================================================================");
        Console.WriteLine("| Название задачи | Текущая (ч) | Целевая (ч) | Срок (лет) | Волатильность | Прогноз (ч) | Статус    |");
        Console.WriteLine("====================================================================================================");
        
        foreach (var task in tasks)
        {
            Console.WriteLine($"| {task.TaskName,-15} | {task.CurrentEstimate,10:F1} | {task.TargetEffort,11:F1} | {task.TimeToDeadline,10:F3} | {task.Volatility,13:P1} | {task.CalculatedEffort,11:F1} | {task.Status,-9} |");
        }
        
        Console.WriteLine("====================================================================================================");
    }

    private void PrintRiskAnalysis(List<TaskEstimation> tasks)
    {
        Console.WriteLine("\n🔍 АНАЛИЗ РИСКОВ");
        
        var riskyTasks = tasks.Where(t => t.Status == "⚠️ Риск").ToList();
        var successfulTasks = tasks.Where(t => t.Status == "✅ Успех").ToList();
        
        Console.WriteLine($"• Всего задач: {tasks.Count}");
        Console.WriteLine($"• Задачи с риском: {riskyTasks.Count} ({riskyTasks.Count * 100.0 / tasks.Count:F1}%)");
        Console.WriteLine($"• Задачи с высокой вероятностью успеха: {successfulTasks.Count}");
        
        if (riskyTasks.Any())
        {
            Console.WriteLine("\n🚨 ЗАДАЧИ С ВЫСОКИМ РИСКОМ:");
            foreach (var task in riskyTasks)
            {
                double overshoot = task.CalculatedEffort - task.TargetEffort;
                double overshootPercent = overshoot * 100 / task.TargetEffort;
                Console.WriteLine($"- {task.TaskName}: превышение на {overshoot:F1} ч ({overshootPercent:F1}%)");
            }
            
            Console.WriteLine("\n💡 РЕКОМЕНДАЦИИ ДЛЯ РИСКОВЫХ ЗАДАЧ:");
            Console.WriteLine("1. Пересмотрите объем работы (Scope Reduction)");
            Console.WriteLine("2. Добавьте дополнительные ресурсы на задачу");
            Console.WriteLine("3. Проведите ревью оценки с экспертами");
            Console.WriteLine("4. Рассмотрите возможность переноса дедлайна");
        }
        
        if (successfulTasks.Any())
        {
            Console.WriteLine("\n🎉 ЗАДАЧИ С ВЫСОКОЙ ВЕРОЯТНОСТЬЮ УСПЕХА:");
            foreach (var task in successfulTasks)
            {
                double buffer = task.TargetEffort - task.CalculatedEffort;
                double bufferPercent = buffer * 100 / task.TargetEffort;
                Console.WriteLine($"- {task.TaskName}: запас {buffer:F1} ч ({bufferPercent:F1}%)");
            }
            
            Console.WriteLine("\n💡 ВОЗМОЖНЫЕ ДЕЙСТВИЯ:");
            Console.WriteLine("• Используйте высвободившиеся ресурсы для рисковых задач");
            Console.WriteLine("• Улучшите качество выполнения задачи");
            Console.WriteLine("• Возьмите дополнительные задачи в этот же срок");
        }
    }

    private double CalculateVolatilityFromHistory()
    {
        Console.WriteLine("\n📈 Расчет волатильности по историческим данным");
        Console.WriteLine("-----------------------------------------------------");
        
        List<double> plannedEfforts = GetListInput("📅 Введите планируемые трудозатраты для завершенных задач (через запятую): ");
        List<double> actualEfforts = GetListInput("📊 Введите фактические трудозатраты для тех же задач (через запятую): ");

        try
        {
            double volatility = _volatilityCalc.Calculate(plannedEfforts, actualEfforts);
            
            // Если волатильность слишком низкая, добавляем минимальное значение
            if (volatility < 0.01)
            {
                Console.WriteLine("⚠️ Рассчитанная волатильность очень низкая. Используется минимальное значение 1%");
                return 0.01;
            }
            
            return volatility;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ошибка: {ex.Message}");
            Console.WriteLine("⚠️ Используется значение волатильности по умолчанию 0.3 (30%)");
            return 0.3;
        }
    }

    private double GetVolatilityFromUser()
    {
        return GetInput("📉 Введите значение волатильности (σ) (например 0.25 для 25%): ", 
            s => {
                double value = Convert.ToDouble(s);
                if (value <= 0) throw new ArgumentException("Волатильность должна быть положительной");
                return value;
            });
    }

    private List<double> GetListInput(string prompt)
    {
        while (true)
        {
            try
            {
                Console.Write(prompt);
                string input = Console.ReadLine();
                return input.Split(',')
                    .Select(s => double.Parse(s.Trim().Replace(',', '.'), CultureInfo.InvariantCulture))
                    .ToList();
            }
            catch
            {
                Console.WriteLine("❌ Неверный формат! Введите числа через запятую (пример: 10, 20, 30.5)");
            }
        }
    }

    private T GetInput<T>(string prompt, Func<string, T> converter)
    {
        while (true)
        {
            try
            {
                Console.Write(prompt);
                return converter(Console.ReadLine());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка: {ex.Message}");
            }
        }
    }
    
    private string GetStringInput(string prompt)
    {
        Console.Write(prompt);
        return Console.ReadLine();
    }
}

public class Program
{
    public static void Main()
    {
        // Устанавливаем культуру для корректного ввода чисел
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        
        var estimator = new InteractiveEffortEstimator();
        estimator.Run();
    }
}