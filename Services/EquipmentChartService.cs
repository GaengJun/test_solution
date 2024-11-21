using LiveCharts.Wpf;
using LiveCharts;
using MES.Solution.Models.Equipment;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace MES.Solution.Services
{
    public class EquipmentChartService
    {
        private SolidColorBrush GetLineColor(string line)
        {
            switch (line)
            {
                case "라인1":
                    return new SolidColorBrush(Color.FromRgb(30, 144, 255));  // 파란색
                case "라인2":
                    return new SolidColorBrush(Color.FromRgb(255, 69, 0));    // 빨간색
                case "라인3":
                    return new SolidColorBrush(Color.FromRgb(50, 205, 50));   // 초록색
                default:
                    return new SolidColorBrush(Colors.Gray);
            }
        }

        public SeriesCollection CreateChartData(List<MeasurementData> lineData, string line)
        {
            var lineColor = GetLineColor(line);
            return new SeriesCollection
        {
            new LineSeries
            {
                Title = $"{line} 온도",
                Values = new ChartValues<double>(
                    lineData.Select(e => (double)e.Temperature)
                ),
                PointGeometry = null,
                Fill = new SolidColorBrush(Color.FromArgb(50, lineColor.Color.R, lineColor.Color.G, lineColor.Color.B)),
                Stroke = lineColor
            }
        };
        }
    }
}
