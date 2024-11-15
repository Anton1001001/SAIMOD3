using System.Windows;
using System.Diagnostics;

using LiveCharts;
using LiveCharts.Wpf;
using MathNet.Numerics.Distributions;
using LiveCharts.Defaults;
using System.Windows.Media;


namespace SAIMOD3
{
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();

            // Выполняем 1000 симуляций для гистограммы
            for (int i = 0; i < 1000; i++)
            {
                Simulation simulation = new Simulation();
                simulation.Run();
            }

            var averageMachineTimes = Simulation.AvgMachine1List;
            InitializeHistogram(averageMachineTimes, 4, 5, 20);
            CalculateAndDisplayStatistics(averageMachineTimes);
            InitializeChartsWithIntervals();




        }

        // Метод инициализации гистограммы
        private void InitializeHistogram(List<float> data, double minRange, double maxRange, int intervalCount)
        {
            double intervalWidth = (data.Max() - data.Min()) / intervalCount;

            var intervalFrequencies = Enumerable
                .Range(0, (int)((maxRange - minRange) / intervalWidth))
                .Select(i => new
                {
                    Lower = minRange + i * intervalWidth,
                    Upper = minRange + (i + 1) * intervalWidth
                })
                .Select(interval => data.Count(value => value >= interval.Lower && value < interval.Upper))
                .ToList();

            MyChart.Series = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Гистограмма",
                    Values = new ChartValues<int>(intervalFrequencies)
                }
            };
        }

        // Метод инициализации графиков с доверительными интервалами и тестом χ²
        private void InitializeChartsWithIntervals()
        {
            List<Tuple<double, double, double>> intervalStatistics = new();
            List<double> chiSquareValues = new();

            for (int i = 0; i < 30; i++)
            {
                RunSimulations(i * 10);
                var confidenceInterval = CalculateConfidenceInterval(Simulation.AvgMachine1List);
                intervalStatistics.Add(confidenceInterval);
                chiSquareValues.Add(CalculateChiSquare(Simulation.AvgMachine1List, false));
                Simulation.AvgMachine1List.Clear();
                Simulation.AvgMachine1List_model.Clear();
            }

            PlotScatterChart(intervalStatistics);
            PlotLineChart("Chi-Square", chiSquareValues, LineChart);

            // Дополнительный график для значений 'sense'
            PlotSenseChart();

            RunSimulations(1);
            var transitionList = Simulation.AvgMachine1List_model;
            DisplayTransition(transitionList);
            PlotLineChart("ere", transitionList.Take(1000).ToList(), LineChartDetailModel);
            Simulation.AvgMachine1List.Clear();
            Simulation.AvgMachine1List_model.Clear();
        }

        private void PlotScatterChart(List<Tuple<double, double, double>> intervalStatistics)
        {
            var lowerUpperPoints = new ChartValues<ObservablePoint>();
            var meanPoints = new ChartValues<ObservablePoint>();

            for (int i = 0; i < intervalStatistics.Count; i++)
            {
                var (lowerBound, upperBound, mean) = intervalStatistics[i];
                lowerUpperPoints.Add(new ObservablePoint(i, lowerBound));
                lowerUpperPoints.Add(new ObservablePoint(i, upperBound));
                meanPoints.Add(new ObservablePoint(i, mean));
            }

            ScatterChart.Series = new SeriesCollection
            {
                new ScatterSeries
                {
                    Values = lowerUpperPoints,
                    MinPointShapeDiameter = 5,
                    MaxPointShapeDiameter = 5,
                    Fill = Brushes.Red
                },
                new ScatterSeries
                {
                    Values = meanPoints,
                    MinPointShapeDiameter = 5,
                    MaxPointShapeDiameter = 5,
                    Fill = Brushes.Blue
                }
            };
        }

        private void PlotLineChart(string title, List<double> values, CartesianChart chart)
        {
            chart.Series = new SeriesCollection
            {
                new LineSeries
                {
                    Title = title,
                    Values = new ChartValues<double>(values),
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 8,
                    Stroke = Brushes.Blue,
                    Fill = Brushes.Transparent
                }
            };
        }

        private void PlotSenseChart()
        {
            List<double> senseValues = new();

            for (int i = 0; i < 100; i++)
            {
                RunSimulations(i + 1, transportTimeMultiplier: 2 * (i + 1));
                senseValues.Add(Simulation.AvgMachine1List.Average());
                Simulation.AvgMachine1List.Clear();
            }

            PlotLineChart("Sense", senseValues, LineChartSense);
        }

        private void RunSimulations(int count, int transportTimeMultiplier = 2)
        {
            for (int i = 0; i < count; i++)
            {
                var simulation = new Simulation { MeanTransportTimeToMachine = transportTimeMultiplier };
                simulation.Run();
            }
        }


        private Tuple<double, double, double> CalculateConfidenceInterval(List<float> data)
        {
            double alpha = 0.05;
            int sampleSize = data.Count;
            double mean = data.Average();
            double stdDev = Math.Sqrt(data.Sum(value => Math.Pow(value - mean, 2)) / (sampleSize - 1));

            double tCritical = GetTCriticalValue(sampleSize - 1, alpha);
            double marginOfError = tCritical * (stdDev / Math.Sqrt(sampleSize));
            double lowerBound = mean - marginOfError;
            double upperBound = mean + marginOfError;

            return Tuple.Create(lowerBound, upperBound, mean);
        }

        private double CalculateChiSquare(List<float> data, bool displayText)
        {
            int sampleSize = data.Count;
            double mean = data.Average();
            double stdDev = Math.Sqrt(data.Sum(value => Math.Pow(value - mean, 2)) / (sampleSize - 1));
            int intervalCount = (int)Math.Ceiling(Math.Sqrt(sampleSize));
            double intervalWidth = (data.Max() - data.Min()) / intervalCount;

            double chiSquareStatistic = 0;
            for (int i = 0; i < intervalCount; i++)
            {
                double lowerBound = data.Min() + i * intervalWidth;
                double upperBound = lowerBound + intervalWidth;
                int observedCount = data.Count(value => value >= lowerBound && value < upperBound);
                double expectedCount = sampleSize * (Normal.CDF(mean, stdDev, upperBound) - Normal.CDF(mean, stdDev, lowerBound));
                chiSquareStatistic += Math.Pow(observedCount - expectedCount, 2) / expectedCount;
            }

            if (displayText)
            {
                DisplayChiSquareResults(chiSquareStatistic, intervalCount - 3);
            }

            return chiSquareStatistic;
        }

        private void DisplayChiSquareResults(double chiSquareStatistic, int degreesOfFreedom)
        {
            double criticalValue = ChiSquared.InvCDF(degreesOfFreedom, 0.95);
            AppendOutput($"χ² статистика: {chiSquareStatistic}");
            AppendOutput($"Критическое значение χ²: {criticalValue}");
            AppendOutput(chiSquareStatistic < criticalValue
                ? "Не удалось отклонить гипотезу о нормальности распределения."
                : "Гипотеза о нормальности распределения отклонена.");
        }

        private double GetTCriticalValue(int degreesOfFreedom, double alpha)
        {
            return StudentT.InvCDF(0, 1, degreesOfFreedom, 1 - alpha / 2);
        }

        private void AppendOutput(string text)
        {
            OutputTextBlock.Text += text + Environment.NewLine;
        }

        private void CalculateAndDisplayStatistics(List<float> data)
        {
            var confidenceInterval = CalculateConfidenceInterval(data);
            AppendOutput($"Среднее значение: {confidenceInterval.Item3}");
            AppendOutput($"95% Доверительный интервал: ({confidenceInterval.Item1}, {confidenceInterval.Item2})");
            AppendOutput($"Стандартное отклонение: {Math.Sqrt(data.Sum(x => Math.Pow(x - confidenceInterval.Item3, 2)) / (data.Count - 1))}");

            CalculateChiSquare(data, true);
        }

        private void DisplayTransition(List<double> data)
        {
            // Считаем переходный период как первые 20% времени
            int transitionPeriod = (int)(data.Count * 0.2);

            // Данные до переходного периода
            var beforeTransition = data.Take(transitionPeriod).ToList();
            // Данные после переходного периода
            var afterTransition = data.Skip(transitionPeriod).ToList();

            // Сравниваем среднее значение до и после переходного периода
            double meanBefore = beforeTransition.Average();
            double meanAfter = afterTransition.Average();

            // Выполняем t-тест для проверки гипотезы о снижении времени
            double tStatistic = CalculateTTest(beforeTransition, afterTransition);

            // Выводим результаты
            AppendOutput($"Среднее время до переходного периода: {meanBefore}");
            AppendOutput($"Среднее время после переходного периода: {meanAfter}");
            AppendOutput($"t-статистика: {tStatistic}");

            if (tStatistic > 1.96) // 95% доверительный интервал
            {
                AppendOutput("Гипотеза о снижении времени прогона подтверждается.");
            }
            else
            {
                AppendOutput("Нет статистически значимого снижения времени.");
            }
        }

        private double CalculateTTest(List<double> before, List<double> after)
        {
            double meanBefore = before.Average();
            double meanAfter = after.Average();
            double varBefore = before.Select(v => Math.Pow(v - meanBefore, 2)).Sum() / (before.Count - 1);
            double varAfter = after.Select(v => Math.Pow(v - meanAfter, 2)).Sum() / (after.Count - 1);

            double pooledVariance = ((before.Count - 1) * varBefore + (after.Count - 1) * varAfter) / (before.Count + after.Count - 2);
            double standardError = Math.Sqrt(pooledVariance * (1.0 / before.Count + 1.0 / after.Count));

            return Math.Abs(meanBefore - meanAfter) / standardError;
        }
    }
}
