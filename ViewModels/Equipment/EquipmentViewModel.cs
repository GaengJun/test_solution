using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MES.Solution.Helpers;
using MySql.Data.MySqlClient;
using Dapper;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Linq;
using MES.Solution.Models.Equipment;
using MES.Solution.ViewModels.Equipment;
using MES.Solution.Services;
using MES.Solution.Views;
using MES.Solution.Models;

namespace MES.Solution.ViewModels
{
    public class EquipmentViewModel : INotifyPropertyChanged
    {
        private readonly string _connectionString;
        private MaintenanceScheduleModel _selectedSchedule;
        private readonly DispatcherTimer _refreshTimer;
        private readonly EquipmentChartService _chartService;
        private bool _isLoading;
        private LogService _logService;
        public PlcViewModel PlcViewModel { get; }

        public EquipmentViewModel()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["MESDatabase"].ConnectionString;
            _chartService = new EquipmentChartService();
            _logService = new LogService();

            // 컬렉션 초기화
            EquipmentCards = new ObservableCollection<EquipmentCardModel>();
            MaintenanceSchedules = new ObservableCollection<MaintenanceScheduleModel>();

            // 명령 초기화
            RefreshCommand = new RelayCommand(async () => await RefreshData());
            ManageScheduleCommand = new RelayCommand(ExecuteManageSchedule);
            DeleteScheduleCommand = new AsyncRelayCommand(async () => await ExecuteDeleteSchedule(), CanExecuteDeleteSchedule);

            // 타이머 설정 (30초마다 데이터 갱신)
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _refreshTimer.Tick += async (s, e) => await RefreshData();
            _refreshTimer.Start();

            // 초기 데이터 로드
            _ = LoadInitialData();

            // PLC ViewModel 초기화
            PlcViewModel = new PlcViewModel();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        // 컬렉션
        public ObservableCollection<EquipmentCardModel> EquipmentCards { get; }
        public ObservableCollection<MaintenanceScheduleModel> MaintenanceSchedules { get; }

        // 명령
        public ICommand RefreshCommand { get; }
        public ICommand ManageScheduleCommand { get; }
        public ICommand DeleteScheduleCommand { get; }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                }
            }
        }

        public MaintenanceScheduleModel SelectedSchedule
        {
            get => _selectedSchedule;
            set
            {
                _selectedSchedule = value;
                OnPropertyChanged();
            }
        }

        private bool CanExecuteDeleteSchedule()
        {
            return MaintenanceSchedules.Any(x => x.IsSelected);
        }
        private async Task ExecuteDeleteSchedule()
        {
            try
            {
                var selectedItems = MaintenanceSchedules.Where(x => x.IsSelected).ToList();
                if (!selectedItems.Any())
                {
                    MessageBox.Show("삭제할 항목을 선택해주세요.", "알림");
                    return;
                }

                if (MessageBox.Show("선택한 항목을 삭제하시겠습니까?", "삭제 확인",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                {
                    return;
                }

                var service = new EquipmentMaintenanceScheduleService();
                foreach (var item in selectedItems)
                {
                    await service.DeleteSchedule(item.EquipmentCode);

                    // 로그 저장
                    string actionDetail = $"장비코드: {item.EquipmentCode}, " +
                                        $"생산라인: {item.ProductionLine}, " +
                                        $"점검일자: {item.InspectionDate:yyyy-MM-dd}";

                    await _logService.SaveLogAsync(App.CurrentUser.UserId, "장비점검 일정 삭제", actionDetail);
                }

                MessageBox.Show("선택한 항목이 삭제되었습니다.", "알림");
                await LoadInitialData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"삭제 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadInitialData()
        {
            try
            {
                IsLoading = true;
                await Task.WhenAll(
                    LoadEquipmentStatus(),
                    LoadMaintenanceSchedules()
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show($"데이터 로드 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadEquipmentStatus()
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

                var measurements = await conn.QueryAsync<MeasurementData>(sql);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    EquipmentCards.Clear();
                    var groupedEquipments = measurements.GroupBy(e => e.ProductionLine);

                    foreach (var lineGroup in groupedEquipments)
                    {
                        var lineData = lineGroup.OrderBy(e => e.InspectionDate).ToList();

                        var equipmentCard = new EquipmentCardModel
                        {
                            ProductionLine = lineGroup.Key,
                            Temperature = (double)lineData.Last().Temperature,
                            Humidity = (double)lineData.Last().Humidity,
                            Status = lineData.Last().Status,
                            Dates = lineData.Select(e => e.InspectionDate).ToList(),
                            ChartData = _chartService.CreateChartData(lineData, lineGroup.Key)
                        };

                        EquipmentCards.Add(equipmentCard);
                    }
                });
            }
        }

        private async Task LoadMaintenanceSchedules()
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    var sql = @"
                SELECT 
                    equipment_code as EquipmentCode,
                    production_line as ProductionLine,
                    equipment_company_name as EquipmentCompanyName,
                    equipment_contact_number as EquipmentContactNumber,
                    equipment_contact_person as EquipmentContactPerson,
                    inspection_date as InspectionDate,
                    inspection_frequency as InspectionFrequency,
                    temperature as Temperature,
                    humidity as Humidity,
                    employee_name as EmployeeName,
                    inspection_date as LastCheckDate,
                    DATE_ADD(inspection_date, INTERVAL 
                        CASE 
                            WHEN inspection_frequency = '월간' THEN 30
                            WHEN inspection_frequency = '분기' THEN 90
                            ELSE 0
                        END DAY) as NextCheckDate,
                    CASE    
                        WHEN DATEDIFF(DATE_ADD(inspection_date, 
                            INTERVAL CASE 
                                WHEN inspection_frequency = '월간' THEN 30
                                WHEN inspection_frequency = '분기' THEN 90
                                ELSE 0
                            END DAY), CURRENT_DATE) < 0 
                            THEN '점검 연체'
                        WHEN DATEDIFF(DATE_ADD(inspection_date, 
                            INTERVAL CASE 
                                WHEN inspection_frequency = '월간' THEN 30
                                WHEN inspection_frequency = '분기' THEN 90
                                ELSE 0
                            END DAY), CURRENT_DATE) <= 7 
                            THEN '점검 예정'
                        ELSE '점검 완료'
                    END as Status
                FROM dt_facility_management
                ORDER BY NextCheckDate";

                    var schedules = await conn.QueryAsync<MaintenanceScheduleModel>(sql);

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        MaintenanceSchedules.Clear();
                        foreach (var schedule in schedules)
                        {
                            MaintenanceSchedules.Add(schedule);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"점검 일정 로드 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task RefreshData()
        {
            await LoadInitialData();
        }

        private void ExecuteManageSchedule()
        {
            try
            {
                var window = new EquipmentMaintenanceScheduleAddWindow()
                {
                    Owner = Application.Current.MainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                window.ShowDialog();

                // 창이 닫힌 후 데이터 새로고침
                if (window.DialogResult == true)
                {
                    _ = LoadInitialData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"장비점검 일정 관리 창을 여는 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

}