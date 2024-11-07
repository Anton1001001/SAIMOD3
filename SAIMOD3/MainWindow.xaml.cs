using System.Windows;
using LiveCharts;
using LiveCharts.Wpf;

namespace SAIMOD3
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            for (int i = 0; i < 40; i++)
            {
                Simulation simulation = new Simulation();
                simulation.Run();
            }
            var list = Simulation.AvgMachine1List;

            double x = 4;
            double y = 5;
            double delta = 0.05d;

            var counts = Enumerable
                .Range(0, (int)((y - x) / delta))
                .Select(i => new 
                {
                    Lower = x + i * delta,
                    Upper = x + (i + 1) * delta
                })
                .Select(interval => list.Count(num => num >= interval.Lower && num < interval.Upper))
                .ToList();

            var values = new ChartValues<int>(counts);

            MyChart.Series =
            [
                new ColumnSeries
                {
                    Title = "Гистограма",
                    Values = values
                }
            ];
            
        }
    }
}
