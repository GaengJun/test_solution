using Dapper;
using LiveCharts;
using LiveCharts.Wpf;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace MES.Solution.Services
{
    public class InventoryChartService
    {
        private readonly string _connectionString;

        public InventoryChartService(string connectionString)
        {
            _connectionString = connectionString;
        }

        // 제품군별 색상 정의
        private readonly Dictionary<string, Color> _groupColors = new Dictionary<string, Color>
        {
            { "비빔밥", Color.FromRgb(30, 144, 255) },  // 파란색
            { "만두", Color.FromRgb(50, 205, 50) },     // 초록색
            { "김밥", Color.FromRgb(255, 140, 0) }      // 주황색
        };

        // 제품별 색상 정의
        private readonly Dictionary<string, Color> _productColors = new Dictionary<string, Color>
        {
            { "야채비빔밥", Color.FromRgb(135, 206, 250) }, // 연파랑
            { "불고기비빔밥", Color.FromRgb(70, 130, 180) }, // 진파랑
            { "참치비빔밥", Color.FromRgb(30, 144, 255) },  // 파랑
            { "김치만두", Color.FromRgb(50, 205, 50) },     // 초록
            { "고기만두", Color.FromRgb(34, 139, 34) },     // 진초록
            { "해물만두", Color.FromRgb(144, 238, 144) },   // 연초록
            { "김치김밥", Color.FromRgb(255, 165, 0) },     // 주황
            { "야채김밥", Color.FromRgb(255, 140, 0) },     // 진주황
            { "참치김밥", Color.FromRgb(255, 69, 0) }       // 빨강
        };

        public async Task<ChartData> LoadAllChartDataAsync()
        {
            var chartData = new ChartData();
            using (var conn = new MySqlConnection(_connectionString))
            {
                // 제품군별 재고 차트
                var (groupSeries, groupLabels) = await LoadProductGroupChartData();
                chartData.ProductGroupChart = groupSeries;
                chartData.GroupChartLabels = groupLabels;

                // 재고 동향 차트
                var (trendSeries, trendLabels) = await LoadInventoryTrendChartData();
                chartData.InventoryTrendChart = trendSeries;
                chartData.TrendChartLabels = trendLabels;

                // 파이 차트 데이터 (기존 코드 유지)
                var pieChartSql = @"
            WITH CurrentStock AS (
                SELECT 
                    p.product_code,
                    p.product_group,
                    COALESCE(SUM(CASE 
                        WHEN i.transaction_type = '입고' THEN i.inventory_quantity
                        WHEN i.transaction_type = '출고' THEN -i.inventory_quantity
                        ELSE 0
                    END), 0) as current_stock
                FROM dt_product p
                LEFT JOIN dt_inventory_management i ON p.product_code = i.product_code
                GROUP BY p.product_code, p.product_group
            )
            SELECT 
                cs.product_group,
                SUM(cs.current_stock) as total_stock
            FROM CurrentStock cs
            WHERE cs.current_stock > 0
            GROUP BY cs.product_group";

                var pieData = await conn.QueryAsync<dynamic>(pieChartSql);
                var pieDataList = pieData.ToList();

                foreach (var item in pieDataList)
                {
                    var groupColor = GetGroupColor(item.product_group.ToString());
                    chartData.QuantityPieChart.Add(new PieSeries
                    {
                        Title = item.product_group.ToString(),
                        Values = new ChartValues<double> { (double)item.total_stock },
                        DataLabels = true,
                        Fill = new SolidColorBrush(groupColor),
                        LabelPoint = point => $"{point.Y:N0}"
                    });
                }

                return chartData;
            }
        }
        private async Task<(SeriesCollection Series, string[] Labels)> LoadProductGroupChartData()
        {
            var seriesCollection = new SeriesCollection();

            using (var conn = new MySqlConnection(_connectionString))
            {
                var sql = @"
                        SELECT 
                            p.product_group AS ProductGroup,
                            p.product_name AS ProductName,
                            SUM(CASE 
                                WHEN i.transaction_type = '입고' THEN i.inventory_quantity
                                WHEN i.transaction_type = '출고' THEN -i.inventory_quantity
                                ELSE 0
                            END) AS TotalQuantity
                        FROM dt_inventory_management i
                        JOIN dt_product p ON i.product_code = p.product_code
                        GROUP BY p.product_group, p.product_name
                        ORDER BY p.product_group, p.product_name";

                var data = await conn.QueryAsync<ProductQuantityModel>(sql);

                // 제품군 라벨 가져오기
                var groupLabels = data.Select(x => x.ProductGroup).Distinct().ToArray();

                // 제품별로 시리즈 생성
                var productSeries = data
                    .GroupBy(x => x.ProductName)
                    .Select(g => new StackedColumnSeries
                    {
                        Title = g.Key,
                        Values = new ChartValues<double>(
                            groupLabels.Select(label =>
                                data.FirstOrDefault(d => d.ProductGroup == label && d.ProductName == g.Key)?.TotalQuantity ?? 0
                            )
                        ),
                        Fill = new SolidColorBrush(GetProductColor(g.Key)),
                        DataLabels = true,
                        LabelPoint = point => point.Y > 0 ? $"{point.Y:N0}" : "",
                    });

                foreach (var series in productSeries)
                {
                    seriesCollection.Add(series);
                }

                return (seriesCollection, groupLabels);
            }
        }

        private async Task<(SeriesCollection Series, string[] Labels)> LoadInventoryTrendChartData()
        {
            var seriesCollection = new SeriesCollection();

            using (var conn = new MySqlConnection(_connectionString))
            {
                var sql = @"
            WITH RECURSIVE dates AS (
                SELECT CURDATE() - INTERVAL 14 DAY AS date
                UNION ALL
                SELECT date + INTERVAL 1 DAY
                FROM dates
                WHERE date < CURDATE()
            ),
            inventory_totals AS (
                SELECT 
                    dt_product.product_group,
                    DATE(transaction_date) as trans_date,
                    SUM(CASE 
                        WHEN transaction_type = '입고' THEN inventory_quantity
                        WHEN transaction_type = '출고' THEN -inventory_quantity
                        ELSE 0
                    END) as daily_quantity
                FROM dt_inventory_management
                JOIN dt_product ON dt_inventory_management.product_code = dt_product.product_code
                GROUP BY dt_product.product_group, DATE(transaction_date)
            ),
            daily_totals AS (
                SELECT 
                    d.date,
                    p.product_group,
                    COALESCE((
                        SELECT SUM(it.daily_quantity)
                        FROM inventory_totals it
                        WHERE it.product_group = p.product_group
                        AND it.trans_date <= d.date
                    ), 0) as total_quantity
                FROM dates d
                CROSS JOIN (SELECT DISTINCT product_group FROM dt_product) p
            )
            SELECT 
                date AS Date,
                product_group AS ProductGroup,
                total_quantity AS TotalQuantity
            FROM daily_totals
            ORDER BY date, product_group";

                var data = await conn.QueryAsync<GroupInventoryTrendModel>(sql);

                var groupedData = data.GroupBy(x => x.ProductGroup)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var dateLabels = data.Select(x => x.Date)
                    .Distinct()
                    .OrderBy(x => x)
                    .Select(x => x.ToString("MM/dd"))
                    .ToArray();

                foreach (var group in groupedData)
                {
                    var color = GetGroupColor(group.Key);
                    var values = new ChartValues<double>();
                    var orderedDates = data.Select(x => x.Date).Distinct().OrderBy(x => x);

                    foreach (var date in orderedDates)
                    {
                        var value = group.Value
                            .FirstOrDefault(x => x.Date.Date == date.Date)
                            ?.TotalQuantity ?? 0;
                        values.Add(value);
                    }

                    var series = new LineSeries
                    {
                        Title = group.Key,
                        Values = values,
                        PointGeometry = DefaultGeometries.None,  // 점 없애기
                        LineSmoothness = 0,  // 직선으로
                        Stroke = new SolidColorBrush(color) { Opacity = 1 },  // 선 색상
                        StrokeThickness = 2,  // 선 두께
                        Fill = new SolidColorBrush(color) { Opacity = 0.2 },  // 반투명한 채우기 추가
                        DataLabels = false
                    };

                    seriesCollection.Add(series);
                }

                return (seriesCollection, dateLabels);
            }
        }

        private async Task<SeriesCollection> LoadQuantityPieChartData()
        {
            var seriesCollection = new SeriesCollection();

            using (var conn = new MySqlConnection(_connectionString))
            {
                var sql = @"
                    SELECT 
                        p.product_group AS ProductGroup,
                        COUNT(DISTINCT i.product_code) AS ProductCount,
                        SUM(CASE 
                            WHEN i.transaction_type = '입고' THEN i.inventory_quantity
                            WHEN i.transaction_type = '출고' THEN -i.inventory_quantity
                            ELSE 0
                        END) AS TotalQuantity
                    FROM dt_inventory_management i
                    JOIN dt_product p ON i.product_code = p.product_code
                    GROUP BY p.product_group";

                var data = await conn.QueryAsync<GroupQuantityModel>(sql);

                foreach (var item in data)
                {
                    seriesCollection.Add(new PieSeries
                    {
                        Title = item.ProductGroup,
                        Values = new ChartValues<double> { item.TotalQuantity },
                        DataLabels = true,
                        Foreground = Brushes.Black,
                        Fill = new SolidColorBrush(GetGroupColor(item.ProductGroup)),
                        LabelPoint = point => $"{item.ProductGroup}\n{point.Y:N0}개 ({point.Participation:P1})"
                    });
                }
            }

            return seriesCollection;
        }

        private Color GetGroupColor(string group)
        {
            return _groupColors.ContainsKey(group) ? _groupColors[group] : Color.FromRgb(128, 128, 128);
        }

        private Color GetProductColor(string product)
        {
            return _productColors.ContainsKey(product) ? _productColors[product] : Color.FromRgb(128, 128, 128);
        }


    }

    public class ChartData
    {
        public SeriesCollection ProductGroupChart { get; set; } = new SeriesCollection();
        public SeriesCollection InventoryTrendChart { get; set; } = new SeriesCollection();
        public SeriesCollection QuantityPieChart { get; set; } = new SeriesCollection(); // Fixed property name
        public string[] GroupChartLabels { get; set; }
        public string[] TrendChartLabels { get; set; }
    }

    public class ProductQuantityModel
    {
        public string ProductGroup { get; set; }
        public string ProductName { get; set; }
        public double TotalQuantity { get; set; }
    }

    public class GroupInventoryTrendModel
    {
        public DateTime Date { get; set; }
        public string ProductGroup { get; set; }
        public double TotalQuantity { get; set; }
    }

    public class GroupQuantityModel
    {
        public string ProductGroup { get; set; }
        public double TotalQuantity { get; set; }
        public int ProductCount { get; set; }
    }
}
