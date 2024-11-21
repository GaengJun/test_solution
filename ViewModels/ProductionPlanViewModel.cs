using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Dapper;
using MES.Solution.Helpers;
using MES.Solution.Models;
using MES.Solution.Services;
using MES.Solution.Views;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Asn1.Esf;
using MES.Solution.Models.Equipment;
using System.Windows.Data;
using System.Windows.Threading;


namespace MES.Solution.ViewModels
{
    public class ProductionPlanViewModel : INotifyPropertyChanged
    {
        private readonly LogService _logService;//로그저장

        private readonly string _connectionString;

        private readonly ProductionPlanService _service;
        private ObservableCollection<ProductionPlanModel> _productionPlans;
        private DateTime _startDate = DateTime.Now.AddDays(1 - DateTime.Now.Day); // 이번달 1일
        private DateTime _endDate = DateTime.Today;
        private string _selectedLine = "전체";
        private string _selectedProduct = "전체";
        private string _selectedStatus = "전체";
        private ProductionPlanModel _selectedPlan;
        private readonly SimulationService _simulationService;

        private bool _isAutoMode;
        public bool IsAutoMode
        {
            get => _isAutoMode;
            set
            {
                if (_isAutoMode != value)
                {
                    _isAutoMode = value;
                    OnPropertyChanged();
                    UpdateOperationMode();
                }
            }
        }

        public ObservableCollection<Models.Equipment.Equipment> LineEquipments { get; }
         = new ObservableCollection<Models.Equipment.Equipment>();

        public ICommand ToggleAutoModeCommand { get; }
        public ICommand MonitorEquipmentCommand { get; }

        public ICommand ViewDetailCommand { get; private set; }
        public ICommand AddCommand { get; }

        private bool _isAllWorkOrdersChecked;
        public bool IsAllWorkOrdersChecked
        {
            get => _isAllWorkOrdersChecked;
            set
            {
                if (_isAllWorkOrdersChecked != value)
                {
                    _isAllWorkOrdersChecked = value;
                    OnPropertyChanged();
                    _ = ExecuteSearch();
                }
            }
        }

        private bool _isCompletedOnlyChecked;
        public bool IsCompletedOnlyChecked
        {
            get => _isCompletedOnlyChecked;
            set
            {
                if (_isCompletedOnlyChecked != value)
                {
                    _isCompletedOnlyChecked = value;
                    OnPropertyChanged();
                    _ = ExecuteSearch();
                }
            }
        }

        public ObservableCollection<ProductionPlanModel> ProductionPlans
        {
            get => _productionPlans;
            set
            {
                _productionPlans = value;
                OnPropertyChanged();
            }
        }

        public ProductionPlanModel SelectedPlan
        {
            get => _selectedPlan;
            set
            {
                if (_selectedPlan != value)
                {
                    _selectedPlan = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                _startDate = value;
                OnPropertyChanged();
                if (_endDate < value)
                {
                    EndDate = value;
                }
            }
        }

        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                _endDate = value;
                OnPropertyChanged();
                if (_startDate > value)
                {
                    StartDate = value;
                }
            }
        }

        public string SelectedLine
        {
            get => _selectedLine;
            set
            {
                _selectedLine = value;
                OnPropertyChanged();
            }
        }

        public string SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                _selectedProduct = value;
                OnPropertyChanged();
            }
        }

        public string SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                _selectedStatus = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<string> ProductionLines { get; private set; }
        public ObservableCollection<string> Products { get; private set; }
        public ObservableCollection<string> Statuses { get; private set; }

        public ICommand SearchCommand { get; }
        public ICommand DeleteCommand { get; }

        public ProductionPlanViewModel()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["MESDatabase"].ConnectionString;
            _service = new ProductionPlanService();
            _logService = new LogService();
            _simulationService = SimulationService.Instance;

            ToggleAutoModeCommand = new RelayCommand(ExecuteToggleAutoMode);
            MonitorEquipmentCommand = new RelayCommand<string>(ExecuteMonitorEquipment);

            _simulationService.ProductionCompleted += OnProductionCompleted;
            _simulationService.ProductionError += OnProductionError;

            LoadEquipmentStatus();

            // 컬렉션 초기화
            ProductionPlans = new ObservableCollection<ProductionPlanModel>();
            ProductionLines = new ObservableCollection<string>();
            Products = new ObservableCollection<string>();
            Statuses = new ObservableCollection<string>();

            // 기본값 설정
            IsAllWorkOrdersChecked = false;
            IsCompletedOnlyChecked = false;
            StartDate = DateTime.Now.Date;
            EndDate = DateTime.Now.Date;

            // 명령 초기화
            SearchCommand = new AsyncRelayCommand(async () => await ExecuteSearch());
            DeleteCommand = new AsyncRelayCommand(async () => await ExecuteDelete(), CanExecuteDelete);
            AddCommand = new RelayCommand(ExecuteAdd);
            ViewDetailCommand = new RelayCommand<ProductionPlanModel>(ExecuteViewDetail);

            // 초기 데이터 로드
            InitializeAsync();


        }

        private void ExecuteToggleAutoMode()
        {
            IsAutoMode = !IsAutoMode;
        }

        private void UpdateOperationMode()
        {
            foreach (var line in _simulationService.ProductionLines)
            {
                _simulationService.SetOperationMode(line.LineId,
                    IsAutoMode ? OperationMode.Automatic : OperationMode.Manual);
            }
        }

        private void ExecuteMonitorEquipment(string lineId)
        {
            var line = _simulationService.ProductionLines.FirstOrDefault(l => l.LineId == lineId);
            if (line == null) return;

            LineEquipments.Clear();
            foreach (var equipment in line.Equipments)
            {
                LineEquipments.Add(equipment);
            }
        }

        private async void InitialDataLoad()
        {
            await LoadComboBoxData();
            await ExecuteSearch();
        }

        public void Dispose()
        {
            _simulationService.ProductionCompleted -= OnProductionCompleted;
            _simulationService.ProductionError -= OnProductionError;
        }

        private void ExecuteViewDetail(ProductionPlanModel plan)
        {
            if (plan != null)
            {
                // 비고 내용을 보여주는 메시지 박스 표시
                MessageBox.Show($"비고 내용:\n{plan.Remarks ?? "비고 없음"}",
                              "비고 보기",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);
            }
        }

        private async void OnProductionCompleted(object sender, ProductionEventArgs e)
        {
            try
            {
                using (var connection = ConnectionManager.Instance.GetConnection())
                {
                    var sql = @"
                UPDATE dt_production_plan 
                SET production_quantity = production_quantity + @ProductCount,
                    process_status = CASE 
                        WHEN production_quantity + @ProductCount >= order_quantity THEN '완료'
                        ELSE '작업중'
                    END
                WHERE work_order_code = @WorkOrderNumber";

                    connection.Execute(sql, new
                    {
                        e.ProductCount,
                        e.WorkOrderNumber
                    });

                    await _logService.SaveLogAsync(
                        App.CurrentUser.UserId,
                        "생산완료",
                        $"작업지시번호: {e.WorkOrderNumber}, 라인: {e.LineId}, 설비: {e.EquipmentId}, 수량: {e.ProductCount}");
                }

                // Task로 실행하고 완료를 기다립니다
                await Task.Run(() =>
                {
                    Application.Current.Dispatcher.Invoke(() => ExecuteSearch());
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"생산 완료 처리 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnProductionError(object sender, ProductionEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show($"생산 오류 발생: {e.ErrorMessage}\n라인: {e.LineId}, 설비: {e.EquipmentId}",
                    "생산 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
            });
        }

        private void LoadEquipmentStatus()
        {
            foreach (var line in _simulationService.ProductionLines)
            {
                foreach (var equipment in line.Equipments)
                {
                    equipment.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(Models.Equipment.Equipment.Status))
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                OnPropertyChanged(nameof(LineEquipments));
                            });
                        }
                    };
                }
            }
        }

        private void ShowAddWindow(bool isEdit = false)
        {
            try
            {
                if (_currentAddWindow != null && _currentAddWindow.IsLoaded)
                {
                    _currentAddWindow.Activate();
                    return;
                }

                _currentAddWindow = new ProductionPlanAddWindow(isEdit)
                {
                    Owner = Application.Current.MainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Title = isEdit ? "생산계획 수정" : "생산계획 등록"  // 창 제목 설정
                };

                _currentAddWindow.Closed += async (s, e) =>
                {
                    if (_currentAddWindow.DialogResult == true)  // DialogResult 확인
                    {
                        await ExecuteSearch();
                    }
                    _currentAddWindow = null;
                };

                _currentAddWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{(isEdit ? "수정" : "등록")} 창을 여는 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ExecuteAdd 메서드 수정
        public void ExecuteAdd()
        {
            ShowAddWindow(false);
        }

        private async void InitializeAsync()
        {
            await LoadComboBoxData();  // 콤보박스 데이터 로드
            await ExecuteSearch();     // 초기 검색 수행
        }

        private async Task LoadComboBoxData()
        {
            try
            {
                var lines = await _service.GetProductionLines();
                ProductionLines.Clear();
                ProductionLines.Add("전체");
                foreach (var line in lines) ProductionLines.Add(line);

                var products = await _service.GetProducts();
                Products.Clear();
                Products.Add("전체");
                foreach (var product in products) Products.Add(product);

                var statuses = await _service.GetStatuses();
                Statuses.Clear();
                foreach (var status in statuses) Statuses.Add(status);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"데이터 로드 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteSearch()
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    var sql = @"
                SELECT 
                    pp.work_order_code AS PlanNumber,
                    pp.production_date AS PlanDate,
                    pp.production_line AS ProductionLine,
                    pp.product_code AS ProductCode,
                    p.product_name AS ProductName,
                    pp.order_quantity AS PlannedQuantity,
                    pp.production_quantity AS ProductionQuantity,
                    pp.work_shift AS WorkShift,
                    IF(pp.order_quantity > 0, 
                       (pp.production_quantity * 100 / pp.order_quantity), 
                       0) AS AchievementRate,
                    pp.process_status AS Status,
                    pp.remarks AS Remarks
                FROM dt_production_plan pp
                JOIN dt_product p ON pp.product_code = p.product_code
                WHERE 1=1";

                    var parameters = new DynamicParameters();

                    // 전체 기간 보기가 체크되지 않은 경우, 오늘 날짜만 표시
                    if (!IsAllWorkOrdersChecked)
                    {
                        sql += " AND pp.production_date BETWEEN @StartDate AND @EndDate";
                        parameters.Add("@StartDate", StartDate.Date);
                        parameters.Add("@EndDate", EndDate.Date);
                    }

                    // 생산라인 필터
                    if (!string.IsNullOrEmpty(SelectedLine) && SelectedLine != "전체")
                    {
                        sql += " AND pp.production_line = @ProductionLine";
                        parameters.Add("@ProductionLine", SelectedLine);
                    }

                    // 제품 필터
                    if (!string.IsNullOrEmpty(SelectedProduct) && SelectedProduct != "전체")
                    {
                        sql += " AND p.product_name = @ProductName";
                        parameters.Add("@ProductName", SelectedProduct);
                    }

                    // 완료 항목 필터
                    if (IsCompletedOnlyChecked)
                    {
                        sql += " AND pp.process_status = '완료'";
                    }
                    else if (!string.IsNullOrEmpty(SelectedStatus) && SelectedStatus != "전체")
                    {
                        sql += " AND pp.process_status = @Status";
                        parameters.Add("@Status", SelectedStatus);
                    }

                    sql += " ORDER BY pp.production_date DESC, pp.work_order_sequence ASC";

                    var result = await conn.QueryAsync<ProductionPlanModel>(sql, parameters);

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        ProductionPlans.Clear();
                        foreach (var plan in result)
                        {
                            ProductionPlans.Add(plan);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"검색 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExecuteDelete()
        {
            return ProductionPlans.Any(x => x.IsSelected);
        }

        private async Task ExecuteDelete()
        {
            if(MessageBox.Show("선택한 항목들을 삭제하시겠습니까?", "삭제", MessageBoxButton.YesNo)== MessageBoxResult.No)
            {
                MessageBox.Show("취소되었습니다","취소");
                return; 
            }
            try
            {
                var selectedItems = ProductionPlans.Where(x => x.IsSelected).ToList();
                if (!selectedItems.Any())
                {
                    MessageBox.Show("삭제할 항목이 선택되지 않았습니다.", "알림");
                    return;
                }

                foreach (var item in selectedItems)
                {
                    await _service.DeleteProductionPlan(item.PlanNumber);

                    // 로그 저장
                    string actionDetail = $"작업지시번호: {item.PlanNumber}, " +
                                        $"생산일자: {item.PlanDate:yyyy-MM-dd}, " +
                                        $"생산라인: {item.ProductionLine}, " +
                                        $"수량: {item.ProductionQuantity}, " +
                                        $"제품: {item.ProductName}";

                    await _logService.SaveLogAsync(App.CurrentUser.UserId, "생산계획 삭제", actionDetail);
                }

                MessageBox.Show("선택한 항목이 삭제되었습니다.", "알림");
                await ExecuteSearch();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"삭제 중 오류가 발생했습니다: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private ProductionPlanAddWindow _currentAddWindow;


        public void LoadDataForEdit(ProductionPlanModel selectedPlan)
        {
            if (selectedPlan == null)
                return;

            try
            {
                if (_currentAddWindow != null && _currentAddWindow.IsLoaded)
                {
                    _currentAddWindow.Activate();
                    return;
                }

                if (IsAutoMode && selectedPlan.Status == "대기")
                {
                    var line = _simulationService.ProductionLines
                        .FirstOrDefault(l => l.LineId == selectedPlan.ProductionLine);
                    var availableEquipment = line?.Equipments
                        .FirstOrDefault(e => e.Status == ProductionStatus.Idle);

                    if (availableEquipment != null)
                    {
                        _simulationService.StartProduction(
                            selectedPlan.ProductionLine,
                            availableEquipment.EquipmentId,
                            selectedPlan.PlanNumber);
                    }
                }

                _currentAddWindow = new ProductionPlanAddWindow(true) // isEdit = true로 설정
                {
                    Owner = Application.Current.MainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Title = "생산계획 수정"
                };

                var viewModel = _currentAddWindow.DataContext as ProductionPlanAddViewModel;
                if (viewModel != null)
                {
                    viewModel.Mode = FormMode.Edit;  // 수정 모드로 설정
                    viewModel.LoadData(new ProductionPlanModel
                    {
                        PlanNumber = selectedPlan.PlanNumber,
                        PlanDate = selectedPlan.PlanDate,
                        ProductionLine = selectedPlan.ProductionLine,
                        ProductCode = selectedPlan.ProductCode,
                        ProductName = selectedPlan.ProductName,
                        PlannedQuantity = selectedPlan.PlannedQuantity,
                        WorkShift = selectedPlan.WorkShift,
                        Status = selectedPlan.Status,
                        Remarks = selectedPlan.Remarks
                    });
                }

                _currentAddWindow.Closed += async (s, e) =>
                {
                    if (_currentAddWindow.DialogResult == true)
                    {
                        await ExecuteSearch();  // 목록 새로고침
                    }
                    _currentAddWindow = null;
                };

                _currentAddWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"수정 창을 여는 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public void Cleanup()
        {
            _simulationService.ProductionCompleted -= OnProductionCompleted;
            _simulationService.ProductionError -= OnProductionError;
        }
    }

}
