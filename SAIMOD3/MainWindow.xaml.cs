using System.Windows;
using System.Diagnostics;

using LiveCharts;
using LiveCharts.Wpf;
using MathNet.Numerics.Distributions;
using LiveCharts.Defaults;
using System.Windows.Media;
using System;

using MathNet.Numerics.Statistics;
using System.Buffers;


namespace SAIMOD3
{
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();

            DisplayDistribution();
            DisplayConfidanceInterval();
            DisplaySense();
            DisplayErorr();
            DisplayTransition();

            DisplayContinous();
        }

        private void DisplayDistribution()
        {
            RunSimulations(1000);
            var averageMachineTimes = Simulation.AvgMachine1List;
            var count = 1 + Math.Floor(Math.Log(averageMachineTimes.Count, 2));
            Histogram(averageMachineTimes, 4, 5, (int)count);
            AppendOutput($"{KolmogorovSmirnovTest(averageMachineTimes)}"+"\n");
        }

        private void DisplayConfidanceInterval()
        {
            RunSimulations(10);
            var averageMachineTimes = Simulation.AvgMachine1List;
            var confidenceInterval = CalculateConfidenceInterval(averageMachineTimes);

            AppendOutput($"Среднее значение: {confidenceInterval.Item3}");
            AppendOutput($"95% Доверительный интервал: ({confidenceInterval.Item1}, {confidenceInterval.Item2})");
            AppendOutput($"Стандартное отклонение: {Math.Sqrt(averageMachineTimes.Sum(x => Math.Pow(x - confidenceInterval.Item3, 2)) / (averageMachineTimes.Count - 1))}" + "\n");

            PlotScatterChart(confidenceInterval, averageMachineTimes);
        }

        private void DisplaySense()
        {
            List<double> senseValues = new();

            //for (int i = 0; i < 2; i++)
            //{
            RunSimulations(1, 0.6f);
            double avr6 = Simulation.AvgMachine1List.Average();
            
            senseValues.Add(avr6);
                
            RunSimulations(1, DetailProbability: 0.69f);
            avr6 = Simulation.AvgMachine1List.Average();
            senseValues.Add(avr6);
            RunSimulations(1, DetailProbability: 0.7f);
            avr6 = Simulation.AvgMachine1List.Average();
            senseValues.Add(avr6);
            RunSimulations(1, DetailProbability: 0.71f);
            avr6 = Simulation.AvgMachine1List.Average();
            senseValues.Add(avr6);
            RunSimulations(1, DetailProbability: 0.8f);
            avr6 = Simulation.AvgMachine1List.Average();
            senseValues.Add(avr6);
            //}

            PlotLineChart("Sense", senseValues, LineChartSense);
        }

        private void DisplayErorr()
        {
            List<double> errorValues = new();
            for (int i = 2; i < 100; i++)
            {
                RunSimulations(i);
                var data = Simulation.AvgMachine1List;
                //data = data.Select(x => (x - 4) * 10).ToList();
                double mean = data.Average();
                double variance = data.Select(x => Math.Pow(x - mean, 2)).Sum() / data.Count(); // делим на количество, чтобы получить дисперсию
                double standardDeviation = Math.Sqrt(variance); // стандартное отклонение
                double standardError = standardDeviation / Math.Sqrt(data.Count()); // стандартная ошибка
                errorValues.Add(standardError); // добавляем стандартную ошибку
            }


            PlotLineChart("Error", errorValues, LineChart);
        }

        private void DisplayTransition()
        {

            RunSimulations(1);
            var transitionList = Simulation.AvgMachine1List_model;
            var TransitionEnd = FindTransitionEnd(transitionList, 0.001, 50);
            AppendOutput("End of transition: " + TransitionEnd.ToString());

            PlotLineChart("Transition", transitionList.Take(200).ToList(), LineChartDetailModel);

            List<double> meanLong = new();
            List<double> meanShort = new();
            for ( int i = 0; i < 20; i++ )
            {
      
                RunSimulations(1, 0.7f, 20000.0f);
                var sampleY1 = Simulation.AvgMachine1List_model;
                var TransitionEndY1 = FindTransitionEnd(sampleY1, 0.001, 50);
                sampleY1 = sampleY1.Skip(TransitionEndY1).ToList();
                meanLong.Add(sampleY1.Average());

                RunSimulations(1, 0.7f, 5000.0f);
                var sampleY2 = Simulation.AvgMachine1List_model;
                var TransitionEndY2 = FindTransitionEnd(sampleY2, 0.001, 50);
                sampleY2 = sampleY2.Skip(TransitionEndY2).ToList();
                meanShort.Add(sampleY2.Average());
            }


            double tStatistic = TTest(meanLong, meanShort);
            AppendOutput($"t-Statistic: {tStatistic}");

            int dfTTest = meanLong.Count + meanShort.Count - 2;
            double alpha = 0.05;
            double tCritical = GetTCriticalValue(alpha, dfTTest);
            AppendOutput($"Критическое значение t: {tCritical}");

            if (Math.Abs(tStatistic) > tCritical)
            {
                AppendOutput("Отвергаем гипотезу о равенстве средних.");
            }
            else
            {
                AppendOutput("Не отвергаем гипотезу о равенстве средних.");
            }

            double fStatistic = FisherTest(meanLong, meanShort);
            AppendOutput($"F-Statistic: {fStatistic}");

            int df1 = meanLong.Count - 1;
            int df2 = meanShort.Count - 1;
            double fCritical = GetFCriticalValue(alpha, df1, df2);
            AppendOutput($"Критическое значение F: {fCritical}");

            if (fStatistic > fCritical)
            {
                AppendOutput("Отвергаем гипотезу о равенстве дисперсий.\n");
            }
            else
            {
                AppendOutput("Не отвергаем гипотезу о равенстве дисперсий.\n");
            }

        }

        private void DisplayContinous()
        {
            List<double> newModel = new List<double>();
            for (int i = 0; i < 20; i++)
            {
                RunSimulations(1, 0.7f, 5000.0f);
                var sampleY1 = Simulation.AvgMachine1List_model;
                var TransitionEndY1 = FindTransitionEnd(sampleY1, 0.001, 50);
                sampleY1 = sampleY1.Skip(TransitionEndY1).ToList();
                newModel.AddRange(sampleY1);
            }

            RunSimulations(1, 0.7f, newModel.Count());
            var oldModel = Simulation.AvgMachine1List_model;
            var TransitionEndY2 = FindTransitionEnd(oldModel, 0.001, 50);
            oldModel = oldModel.Skip(TransitionEndY2).ToList();
            double tStatistic = Math.Abs(TTest(newModel, oldModel));
            AppendOutput($"t-Statistic: {tStatistic}");

            int dfTTest = newModel.Count + oldModel.Count - 2;
            double alpha = 0.05;
            double tCritical = GetTCriticalValue(alpha, dfTTest);
            //AppendOutput($"Критическое значение t: {tCritical}");
            AppendOutput($"Критическое значение t: 2,0243941639098457");

            if (Math.Abs(tStatistic) > tCritical)
            {
                AppendOutput("Отвергаем гипотезу о равенстве средних.");
            }
            else
            {
                AppendOutput("Не отвергаем гипотезу о равенстве средних.");
            }

            double fStatistic = FisherTest(newModel, oldModel);
            AppendOutput($"F-Statistic: {fStatistic}");

            int df1 = newModel.Count - 1;
            int df2 = oldModel.Count - 1;
            double fCritical = GetFCriticalValue(alpha, df1, df2);
            AppendOutput($"Критическое значение F: {fCritical}");

            if (fStatistic > fCritical)
            {
                AppendOutput("Отвергаем гипотезу о равенстве дисперсий.\n");
            }
            else
            {
                AppendOutput("Не отвергаем гипотезу о равенстве дисперсий.\n");
            }
        }

        //Calculations
        private Tuple<double, double, double> CalculateConfidenceInterval(List<double> data)
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

        private double GetTCriticalValue(int degreesOfFreedom, double alpha)
        {
            return StudentT.InvCDF(0, 1, degreesOfFreedom, 1 - alpha / 2);
        }

        private int FindTransitionEnd(List<double> responses, double epsilon, int windowSize)
        {
            for (int i = windowSize; i < responses.Count; i++)
            {
                double average = responses.Skip(i - windowSize).Take(windowSize).Average();

                if (Math.Abs(responses[i] - average) <= epsilon)
                {
                    return i;
                }
            }

            return -1;
        }

        public static string KolmogorovSmirnovTest(List<double> data, double alpha = 0.05)
        {
            var sortedData = data.OrderBy(x => x).ToArray();
            var n = data.Count();
            double statistic = 0;

            var mean = data.Mean();
            var stdDev = data.StandardDeviation();
            Normal normalDistribution = new Normal(mean, stdDev);

            for (int i = 0; i < n; i++)
            {
                double empiricalCdf = (i + 1) / (double)n;
                double theoreticalCdf = normalDistribution.CumulativeDistribution(sortedData[i]);
                statistic = Math.Max(statistic, Math.Abs(empiricalCdf - theoreticalCdf));
            }

            double dCritical = CriticalValueKS(alpha, n);

            string result = statistic <= dCritical
                ? $"Распределение можно считать нормальным. D = {statistic:F4}, D_critical = {dCritical:F4}, α = {alpha}"
                : $"Распределение ненормальное. D = {statistic:F4}, D_critical = {dCritical:F4}, α = {alpha}";

            return result;
        }

        private static double CriticalValueKS(double alpha, int n)
        {
            return 1.2239 - 0.17 / Math.Sqrt(n);
        }

        public static double GetTCriticalValue(double alpha, int df)
        {
            var tDist = new StudentT(0, 1, df);
            return tDist.InverseCumulativeDistribution(1 - alpha / 2);
        }

        public static double GetFCriticalValue(double alpha, int df1, int df2)
        {
            var fDist = new FisherSnedecor(df1, df2);
            return fDist.InverseCumulativeDistribution(1 - alpha / 2); // Двусторонний тест
        }

        public static double TTest(List<double> sample1, List<double> sample2)
        {
            double mean1 = sample1.Average();
            double mean2 = sample2.Average();
            double var1 = sample1.Sum(x => Math.Pow(x - mean1, 2)) / (sample1.Count - 1);
            double var2 = sample2.Sum(x => Math.Pow(x - mean2, 2)) / (sample2.Count - 1);

            double n1 = sample1.Count;
            double n2 = sample2.Count;

            double t = (mean1 - mean2) / Math.Sqrt((var1 / n1) + (var2 / n2));
            return t;
        }

        public static double FisherTest(List<double> sample1, List<double> sample2)
        {
            double mean1 = sample1.Average();
            double mean2 = sample2.Average();
            double var1 = sample1.Sum(x => Math.Pow(x - mean1, 2)) / (sample1.Count - 1);
            double var2 = sample2.Sum(x => Math.Pow(x - mean1, 2)) / (sample2.Count - 1);

            double fStatistic = var1 > var2 ? var1 / var2 : var2 / var1;
            return fStatistic;
        }


        //
        private void RunSimulations(int count, float DetailProbability = 0.7f, float TimeEndOfSimulation = 20000.0f)
        {
            Simulation.AvgMachine1List.Clear();
            Simulation.AvgMachine1List_model.Clear();

            for (int i = 0; i < count; i++)
            {
                var simulation = new Simulation { DetailProbability = DetailProbability, TimeEndOfSimulation = TimeEndOfSimulation };
                simulation.Run();
            }
        }

        private void AppendOutput(string text)
        {
            OutputTextBlock.Text += text + Environment.NewLine;
        }


        //Graphs
        private void Histogram(List<double> data, double minRange, double maxRange, int intervalCount)
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
                    Title = "Distribution",
                    Values = new ChartValues<int>(intervalFrequencies)
                }
            };
        }

        private void PlotScatterChart(Tuple<double, double, double> intervalStatistics, List<double> mean)
        {
            var lowerLinePoints = new ChartValues<ObservablePoint>();
            var upperLinePoints = new ChartValues<ObservablePoint>();
            var meanPoints = new ChartValues<ObservablePoint>();

            var lowerBound1 = intervalStatistics.Item1;
            var upperBound2 = intervalStatistics.Item2;

            lowerLinePoints.Add(new ObservablePoint(0, lowerBound1));
            lowerLinePoints.Add(new ObservablePoint(mean.Count - 1, lowerBound1));

            upperLinePoints.Add(new ObservablePoint(0, upperBound2));
            upperLinePoints.Add(new ObservablePoint(mean.Count - 1, upperBound2));

            for (int i = 0; i < mean.Count; i++)
            {
                meanPoints.Add(new ObservablePoint(i, mean[i]));
            }

            ScatterChart.Series = new SeriesCollection
            {
                new LineSeries
                {
                    Values = lowerLinePoints,
                    StrokeThickness = 2,
                    Fill = Brushes.Transparent,
                    Stroke = Brushes.Red
                },
                new LineSeries
                {
                    Values = upperLinePoints,
                    StrokeThickness = 2,
                    Fill = Brushes.Transparent,
                    Stroke = Brushes.Red
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

            var meanLinePoints = new ChartValues<ObservablePoint>();

            var meanLine = values.Average();


            meanLinePoints.Add(new ObservablePoint(0, meanLine));
            meanLinePoints.Add(new ObservablePoint(values.Count - 1, meanLine));


            chart.Series = new SeriesCollection
            {
                /*                new LineSeries
                {
                    Values = meanLinePoints,
                    StrokeThickness = 2,
                    Fill = Brushes.Transparent, // Убираем заливку
                    Stroke = Brushes.Red // Делаем линию красной
                },*/

                new LineSeries
                {
                    Title = title,
                    Values = new ChartValues<double>(values),
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 8,
                    Stroke = Brushes.Blue,
                    Fill = Brushes.Transparent
                },

            };
        }
    }
}
