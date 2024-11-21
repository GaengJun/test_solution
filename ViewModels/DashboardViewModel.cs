using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using System.Windows;
using LiveCharts;
using LiveCharts.Wpf;
using System.Windows.Media;
using MySql.Data.MySqlClient;
using Dapper;
using System.Configuration;
using System.Threading.Tasks;
using System.Linq;
using MES.Solution.Services;

namespace MES.Solution.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly string _connectionString;
        private readonly DispatcherTimer _refreshTimer;
        private readonly EquipmentChartService _equipmentChartService;

        // 상태 필드
        private string _todayProduction;
        private double _todayProductionRate;
        private string _weeklyProduction;
        private double _weeklyProductionRate;
        private double _equipmentOperationRate;
        private double _achievementRate;
        private int _operatingEquipmentCount;
        private string[] _timeLabels;

        public event PropertyChangedEventHandler PropertyChanged;

        // Properties
        public string TodayProduction
        {
            get => _todayProduction;
            set
            {
                if (_todayProduction != value)
                {
                    _todayProduction = value;
                    OnPropertyChanged();
                }
            }
        }

        public double TodayProductionRate
        {
            get => _todayProductionRate;
            set
            {
                if (_todayProductionRate != value)
                {
                    _todayProductionRate = value;
                    OnPropertyChanged();
                }
            }
        }

        public string WeeklyProduction
        {
            get => _weeklyProduction;
            set
            {
                if (_weeklyProduction != value)
                {
                    _weeklyProduction = value;
                    OnPropertyChanged();
                }
            }
        }

        public double WeeklyProductionRate
        {
            get => _weeklyProductionRate;
            set
            {
                if (_weeklyProductionRate != value)
                {
                    _weeklyProductionRate = value;
                    OnPropertyChanged();
                }
            }
        }

        public double EquipmentOperationRate
        {
            get => _equipmentOperationRate;
            set
            {
                if (_equipmentOperationRate != value)
                {
                    _equipmentOperationRate = value;
                    OnPropertyChanged();
                }
            }
        }

        public double AchievementRate
        {
            get => _achievementRate;
            set
            {
                if (_achievementRate != value)
                {
                    _achievementRate = value;
                    OnPropertyChanged();
                }
            }
        }

        public int OperatingEquipmentCount
        {
            get => _operatingEquipmentCount;
            set
            {
                if (_operatingEquipmentCount != value)
                {
                    _operatingEquipmentCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public string[] TimeLabels
        {
            get => _timeLabels;
            set
            {
                if (_timeLabels != value)
                {
                    _timeLabels = value;
                    OnPropertyChanged();
                }
            }
        }

        // Collections
        public ObservableCollection<LineStatusModel> LineStatus { get; }
        public ObservableCollection<EquipmentStatusModel> EquipmentStatus { get; }
        public SeriesCollection ProductionTrendSeries { get; }

        public Func<double, string> NumberFormatter { get; set; }

        // Constructor
        public DashboardViewModel()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["MESDatabase"].ConnectionString;
            _equipmentChartService = new EquipmentChartService();
            NumberFormatter = value => value.ToString("N0");

            // Collection 초기화
            LineStatus = new ObservableCollection<LineStatusModel>();
            EquipmentStatus = new ObservableCollection<EquipmentStatusModel>();
            ProductionTrendSeries = new SeriesCollection();

            // 타이머 설정 (30초마다 새로고침)
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _refreshTimer.Tick += async (s, e) => await RefreshDataAsync();
            _refreshTimer.Start();

            // 초기 데이터 로드
            _ = InitializeDataAsync();
        }

        private async Task InitializeDataAsync()
        {
            try
            {
                await Task.WhenAll(
                    LoadProductionStatusAsync(),
                    LoadEquipmentStatusAsync(),
                    LoadProductionTrendAsync()
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show($"데이터 초기화 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadProductionStatusAsync()
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                var sql = @"
                        SELECT 
                            SUM(order_quantity) as TotalPlanned,
                            SUM(production_quantity) as TotalProduced
                        FROM dt_production_plan 
                        WHERE DATE(production_date) = CURDATE();

                        SELECT 
                            SUM(order_quantity) as WeeklyPlanned,
                            SUM(production_quantity) as WeeklyProduced
                        FROM dt_production_plan 
                        WHERE production_date >= DATE_SUB(CURDATE(), INTERVAL 7 DAY);

                        SELECT 
                            production_line as LineName,
                            CASE 
                                WHEN COUNT(DISTINCT process_status) > 1 THEN '혼합'
                                ELSE MAX(process_status)
                            END as Status,
                            SUM(production_quantity) as Production
                        FROM dt_production_plan
                        WHERE DATE(production_date) = CURDATE()
                        GROUP BY production_line;";

                using (var multi = await conn.QueryMultipleAsync(sql))
                {
                    var dailyStats = await multi.ReadFirstAsync();
                    var weeklyStats = await multi.ReadFirstAsync();
                    var lineStats = await multi.ReadAsync<LineStatusModel>();

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        TodayProduction = $"{dailyStats.TotalProduced:N0}";
                        // decimal을 double로 변환
                        TodayProductionRate = dailyStats.TotalPlanned > 0
                            ? Convert.ToDouble(dailyStats.TotalProduced) * 100.0 / Convert.ToDouble(dailyStats.TotalPlanned)
                            : 0;

                        WeeklyProduction = $"{weeklyStats.WeeklyProduced:N0}";
                        // decimal을 double로 변환
                        WeeklyProductionRate = weeklyStats.WeeklyPlanned > 0
                            ? Convert.ToDouble(weeklyStats.WeeklyProduced) * 100.0 / Convert.ToDouble(weeklyStats.WeeklyPlanned)
                            : 0;

                        AchievementRate = TodayProductionRate;

                        LineStatus.Clear();
                        foreach (var line in lineStats)
                        {
                            LineStatus.Add(line);
                        }
                    });
                }
            }
        }

        private async Task LoadEquipmentStatusAsync()//설비
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                var sql = @"
 SELECT 
     equipment_code as EquipmentCode,
     production_line as ProductionLine,
     CAST(temperature as DECIMAL(5,2)) as Temperature,
     CAST(humidity as DECIMAL(5,2)) as Humidity,
     inspection_date as InspectionDate,
     CASE 
         WHEN temperature > 28 THEN '경고'
         WHEN temperature < 18 THEN '경고'
         ELSE '정상'
     END as Status
 FROM dt_facility_management
 ORDER BY production_line, inspection_date";

                var equipments = await conn.QueryAsync<EquipmentStatusModel>(sql);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    EquipmentStatus.Clear();
                    var groupedEquipments = equipments.GroupBy(e => e.ProductionLine);

                    foreach (var lineGroup in groupedEquipments)
                    {
                        var lineData = lineGroup.OrderBy(e => e.InspectionDate).ToList();
                        var equipmentCard = new EquipmentStatusModel
                        {
                            ProductionLine = lineGroup.Key,
                            Temperature = (decimal)(double)lineData.Last().Temperature,
                            Status = lineData.Last().Status,
                        };
                        EquipmentStatus.Add(equipmentCard);
                    }

                    // 고유한 장비코드 중에서 상태가 정상인 것들의 수
                    var distinctEquipments = equipments
                        .GroupBy(e => e.EquipmentCode)
                        .Select(g => g.OrderByDescending(e => e.InspectionDate).First()) // 각 장비의 최신 상태
                        .ToList();

                    // 정상 상태인 장비 수
                    OperatingEquipmentCount = distinctEquipments.Count(e => e.Status == "정상");

                    // 전체 고유 장비 수
                    TotalEquipmentCount = distinctEquipments.Count;

                    // 가동률 계산 (정상 장비 수 / 전체 고유 장비 수 * 100)
                    EquipmentOperationRate = TotalEquipmentCount > 0
                        ? (double)OperatingEquipmentCount * 100 / TotalEquipmentCount
                        : 0;
                });
            }
        }

        private int _totalEquipmentCount;
        public int TotalEquipmentCount
        {
            get => _totalEquipmentCount;
            set
            {
                _totalEquipmentCount = value;
                OnPropertyChanged(nameof(TotalEquipmentCount));
            }
        }

        private async Task LoadProductionTrendAsync()
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                var sql = @"
            SELECT 
                DATE_FORMAT(production_date, '%m-%d') as Date,
                SUM(production_quantity) as Quantity,
                production_date  -- GROUP BY에 사용될 컬럼 추가
            FROM dt_production_plan
            WHERE production_date >= DATE_SUB(CURDATE(), INTERVAL 7 DAY)
                AND production_date <= CURDATE()
            GROUP BY DATE(production_date), production_date  -- production_date 추가
            ORDER BY production_date;";

                var trends = await conn.QueryAsync<ProductionTrendModel>(sql);
                var data = trends.ToList();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ProductionTrendSeries.Clear();
                    ProductionTrendSeries.Add(new LineSeries
                    {
                        Title = "일일 생산량",
                        Values = new ChartValues<double>(data.Select(x => (double)x.Quantity)),
                        PointGeometry = DefaultGeometries.Circle,
                        Stroke = new SolidColorBrush(Color.FromRgb(24, 90, 189))
                    });

                    TimeLabels = data.Select(x => x.Date).ToArray();
                });
            }
        }

        private async Task RefreshDataAsync()
        {
            await InitializeDataAsync();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Cleanup()
        {
            _refreshTimer?.Stop();
        }
    }

    public class LineStatusModel
    {
        public string LineName { get; set; }
        public string Status { get; set; }
        public int Production { get; set; }
    }

    public class EquipmentStatusModel
    {
        public string EquipmentCode { get; set; }
        public string ProductionLine { get; set; }
        public decimal Temperature { get; set; }
        public string Status { get; set; }
        public decimal Humidity { get; set; }
        public DateTime InspectionDate { get; set; }

        // 표시용 속성 추가
        public string DisplayName => $"{ProductionLine}-{EquipmentCode.Split('-').Last()}";
    }

    public class ProductionTrendModel
    {
        public string Date { get; set; }
        public int Quantity { get; set; }
    }
}