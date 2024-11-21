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
using System.Linq;
using MES.Solution.Models;
using MES.Solution.Models.Equipment;
using MES.Solution.Services;


namespace MES.Solution.ViewModels
{
    public class WorkOrderViewModel : INotifyPropertyChanged
    {
        private readonly string _connectionString;
        private readonly SimulationService _simulationService;
        private DateTime _workDate = DateTime.Today;
        private string _selectedShift;
        private string _selectedLine;
        private WorkOrderModel _selectedWorkOrder;
        private OperationMode _operationMode = OperationMode.Manual;
        private ICommand _pauseWorkCommand;
        private ICommand _resumeWorkCommand;
        public ObservableCollection<Models.Equipment.Equipment> LineEquipments { get; }


        public WorkOrderViewModel()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["MESDatabase"].ConnectionString;
            _simulationService = SimulationService.Instance;

            // 초기값 설정
            IsAllWorkOrdersChecked = false;
            IsCompletedOnlyChecked = false;
            WorkDate = DateTime.Now.Date;

            // 컬렉션 초기화
            WorkOrders = new ObservableCollection<WorkOrderModel>();
            Shifts = new ObservableCollection<string> { "전체", "주간1", "주간2", "야간1", "야간2" };
            ProductionLines = new ObservableCollection<string> { "전체", "라인1", "라인2", "라인3" };
            LineEquipments = new ObservableCollection<Models.Equipment.Equipment>();


            // 상태 명령 초기화
            StartWorkCommand = new RelayCommand(ExecuteStartWork, CanExecuteStartWork);
            CompleteWorkCommand = new RelayCommand(ExecuteCompleteWork, CanExecuteCompleteWork);
            CancelWorkCommand = new RelayCommand(ExecuteCancel, CanExecuteCancel);
            RestartWorkCommand = new RelayCommand(ExecuteRestartWork, CanExecuteRestartWork);
            ToggleOperationModeCommand = new RelayCommand(ExecuteToggleOperationMode);
            PauseProductionCommand = new RelayCommand(ExecutePauseProduction, CanPauseProduction);

            // 명령 초기화
            SearchCommand = new RelayCommand(async () => await ExecuteSearch());
            AddCommand = new RelayCommand(ExecuteAdd);
            MoveUpCommand = new RelayCommand(ExecuteMoveUp, CanExecuteMove);
            MoveDownCommand = new RelayCommand(ExecuteMoveDown, CanExecuteMove);
            SaveSequenceCommand = new RelayCommand(ExecuteSaveSequence, CanExecuteSaveSequence);

            // 시뮬레이션 이벤트 구독
            _simulationService.ProductionCompleted += OnProductionCompleted;
            _simulationService.ProductionStarted += OnProductionStarted;
            _simulationService.ProductionPaused += OnProductionPaused;
            _simulationService.ProductionResumed += OnProductionResumed;
            _simulationService.ProductionError += OnProductionError;

            // 선택된 라인 변경 시 설비 목록 업데이트
            this.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SelectedLine))
                {
                    UpdateEquipmentsList();
                }
            };


            // 초기 데이터 로드
            LoadInitialData();
            SelectedShift = "전체";
            SelectedLine = "전체";
        }

        public ICommand PauseWorkCommand
        {
            get
            {
                if (_pauseWorkCommand == null)
                {
                    _pauseWorkCommand = new RelayCommand(ExecutePauseWork, CanPauseWork);
                }
                return _pauseWorkCommand;
            }
        }

        public ICommand ResumeWorkCommand
        {
            get
            {
                if (_resumeWorkCommand == null)
                {
                    _resumeWorkCommand = new RelayCommand(ExecuteResumeWork, CanResumeWork);
                }
                return _resumeWorkCommand;
            }
        }

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

        private void UpdateOperationMode()
        {
            CurrentOperationMode = IsAutoMode ? OperationMode.Automatic : OperationMode.Manual;
            if (SelectedLine != "전체")
            {
                _simulationService.SetOperationMode(SelectedLine, CurrentOperationMode);
            }
        }

        private void UpdateEquipmentsList()
        {
            LineEquipments.Clear();
            if (SelectedLine != "전체")
            {
                var line = _simulationService.ProductionLines
                    .FirstOrDefault(l => l.LineId == SelectedLine);
                if (line != null)
                {
                    foreach (var equipment in line.Equipments)
                    {
                        LineEquipments.Add(equipment);
                    }
                }
            }
        }

        public OperationMode CurrentOperationMode
        {
            get => _operationMode;
            set
            {
                if (_operationMode != value)
                {
                    _operationMode = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool CanPauseWork()
        {
            if (SelectedWorkOrder == null || SelectedLine == "전체") return false;

            var equipment = _simulationService.ProductionLines
                .FirstOrDefault(l => l.LineId == SelectedLine)
                ?.Equipments.FirstOrDefault(e => e.CurrentWorkOrder == SelectedWorkOrder.WorkOrderNumber);

            return equipment?.Status == ProductionStatus.Operating || SelectedWorkOrder.Status == "작업중";
        }

        private void ExecutePauseWork()
        {
            if (SelectedWorkOrder == null || SelectedLine == "전체") return;

            var equipment = LineEquipments.FirstOrDefault(e =>
                e.CurrentWorkOrder == SelectedWorkOrder.WorkOrderNumber);

            if (equipment != null)
            {
                _simulationService.PauseProduction(SelectedLine, equipment.EquipmentId);
            }
        }

        private bool CanResumeWork()
        {
            var equipment = LineEquipments.FirstOrDefault(e =>
                e.CurrentWorkOrder == SelectedWorkOrder?.WorkOrderNumber);
            return equipment?.Status == Models.Equipment.ProductionStatus.Paused;
        }

        private void ExecuteResumeWork()
        {
            if (SelectedWorkOrder == null || SelectedLine == "전체") return;

            var equipment = LineEquipments.FirstOrDefault(e =>
                e.CurrentWorkOrder == SelectedWorkOrder.WorkOrderNumber);

            if (equipment != null)
            {
                _simulationService.ResumeProduction(SelectedLine, equipment.EquipmentId);
            }
        }

        private void OnProductionStarted(object sender, ProductionEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var workOrder = WorkOrders.FirstOrDefault(w => w.WorkOrderNumber == e.WorkOrderNumber);
                if (workOrder != null)
                {
                    workOrder.Status = "작업중";
                    UpdateEquipmentsList();
                }
            });
        }

        private void OnProductionPaused(object sender, ProductionEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var workOrder = WorkOrders.FirstOrDefault(w => w.WorkOrderNumber == e.WorkOrderNumber);
                if (workOrder != null)
                {
                    workOrder.Status = "일시정지";
                    UpdateEquipmentsList();
                }
            });
        }

        private void OnProductionResumed(object sender, ProductionEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var workOrder = WorkOrders.FirstOrDefault(w => w.WorkOrderNumber == e.WorkOrderNumber);
                if (workOrder != null)
                {
                    workOrder.Status = "작업중";
                    UpdateEquipmentsList();
                }
            });
        }

        public ICommand ToggleOperationModeCommand { get; }
        public ICommand PauseProductionCommand { get; }

        private void ExecuteToggleOperationMode()
        {
            CurrentOperationMode = CurrentOperationMode == OperationMode.Manual
                ? OperationMode.Automatic
                : OperationMode.Manual;

            if (SelectedLine != "전체")
            {
                _simulationService.SetOperationMode(SelectedLine, CurrentOperationMode);
            }
        }

        private void ExecutePauseProduction()
        {
            if (SelectedWorkOrder != null && SelectedLine != "전체")
            {
                var equipment = _simulationService.ProductionLines
                    .FirstOrDefault(l => l.LineId == SelectedLine)
                    ?.Equipments.FirstOrDefault(e => e.CurrentWorkOrder == SelectedWorkOrder.WorkOrderNumber);

                if (equipment != null)
                {
                    _simulationService.PauseProduction(SelectedLine, equipment.EquipmentId);
                }
            }
        }

        private bool CanPauseProduction()
        {
            if (SelectedWorkOrder == null || SelectedLine == "전체") return false;

            var equipment = _simulationService.ProductionLines
                .FirstOrDefault(l => l.LineId == SelectedLine)
                ?.Equipments.FirstOrDefault(e => e.CurrentWorkOrder == SelectedWorkOrder.WorkOrderNumber);

            return equipment?.Status == ProductionStatus.Operating;
        }

       private async void OnProductionCompleted(object sender, ProductionEventArgs e)
{
    try
    {
        using (var connection = ConnectionManager.Instance.GetConnection())
        {
            var sql = @"
                UPDATE dt_production_plan 
                SET production_quantity = production_quantity + 1,
                    process_status = CASE 
                        WHEN production_quantity + 1 >= order_quantity THEN '완료'
                        ELSE '작업중'
                    END
                WHERE work_order_code = @WorkOrderNumber";

            connection.Execute(sql, new { e.WorkOrderNumber });
        }

        await Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            var workOrder = WorkOrders.FirstOrDefault(w => w.WorkOrderNumber == e.WorkOrderNumber);
            if (workOrder != null)
            {
                workOrder.ProductionQuantity++;
                if (workOrder.ProductionQuantity >= workOrder.OrderQuantity)
                {
                    workOrder.Status = "완료";
                }
            }
            await ExecuteSearch();
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

        private bool _isallWorkOrdersChecked;
        public bool IsAllWorkOrdersChecked
        {
            get => _isallWorkOrdersChecked;
            set
            {
                if (_isallWorkOrdersChecked != value)
                {
                    _isallWorkOrdersChecked = value;
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

        private async void RefreshWorkOrders()
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    var sql = @"SELECT 
                pp.work_order_code as WorkOrderNumber,
                pp.production_date as ProductionDate,
                p.product_code as ProductCode,
                p.product_name as ProductName,
                pp.order_quantity as OrderQuantity,
                pp.production_quantity as ProductionQuantity,
                pp.work_order_sequence as Sequence,
                pp.work_shift as Shift,
                pp.process_status as Status,
                pp.production_line as ProductionLine
            FROM dt_production_plan pp
            JOIN dt_product p ON pp.product_code = p.product_code
            WHERE 1=1";

                    var parameters = new DynamicParameters();

                    // 전체 기간 보기가 체크되지 않은 경우, 오늘 날짜만 표시
                    if (!IsAllWorkOrdersChecked)
                    {
                        sql += " AND DATE(pp.production_date) = CURRENT_DATE()";
                    }

                    // 생산라인 필터
                    if (!string.IsNullOrEmpty(SelectedLine) && SelectedLine != "전체")
                    {
                        sql += " AND pp.production_line = @ProductionLine";
                        parameters.Add("@ProductionLine", SelectedLine);
                    }

                    // 근무조 필터
                    if (!string.IsNullOrEmpty(SelectedShift) && SelectedShift != "전체")
                    {
                        sql += " AND pp.work_shift = @Shift";
                        parameters.Add("@Shift", SelectedShift);
                    }

                    // 완료 항목 필터
                    if (IsCompletedOnlyChecked)
                    {
                        sql += " AND pp.process_status = '완료'";
                    }

                    // 정렬 조건
                    sql += " ORDER BY pp.production_date DESC, pp.work_order_sequence ASC";

                    var result = await conn.QueryAsync<WorkOrderModel>(sql, parameters);

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        WorkOrders.Clear();
                        foreach (var order in result)
                        {
                            WorkOrders.Add(order);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"데이터 로드 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private ICommand _restartWorkCommand; // 명령 객체 선언
        public ICommand RestartWorkCommand
        {
            get => _restartWorkCommand;
            set
            {
                if (_restartWorkCommand != value)
                {
                    _restartWorkCommand = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool CanExecuteRestartWork()
        {
            return SelectedWorkOrder != null && SelectedWorkOrder.Status == "지연";
        }

        private bool CanExecuteStartWork()
        {
            return SelectedWorkOrder != null && SelectedWorkOrder.Status == "대기";
        }

        private bool CanExecuteCompleteWork()
        {
            return SelectedWorkOrder != null && SelectedWorkOrder.Status == "작업중";
        }

        private bool CanExecuteCancel()
        {
            return SelectedWorkOrder != null && (SelectedWorkOrder.Status == "작업중" || SelectedWorkOrder.Status == "대기");
        }

        private async void ExecuteRestartWork()
        {
            // '작업중'으로 상태 변경 로직
            if (SelectedWorkOrder == null) return;
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    var sql = @"
                    UPDATE dt_production_plan 
                    SET process_status = '작업중',
                        start_time = @StartTime
                    WHERE work_order_code = @WorkOrderCode";

                    await conn.ExecuteAsync(sql, new
                    {
                        StartTime = DateTime.Now,
                        WorkOrderCode = SelectedWorkOrder.WorkOrderNumber
                    });

                    SelectedWorkOrder.StartTime = DateTime.Now;
                    SelectedWorkOrder.Status = "작업중";
                    await ExecuteSearch(); // 목록 새로고침
                    UpdateButtonStates();
                    MessageBox.Show("작업이 재시작되었습니다.", "알림");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"작업 재시작 중 오류가 발생했습니다: {ex.Message}", "오류");
            }
        }

        private ICommand _startWorkCommand;
        private ICommand _completeWorkCommand;
        private ICommand _cancelWorkCommand;

        public ICommand StartWorkCommand
        {
            get { return _startWorkCommand; }
            set
            {
                if (_startWorkCommand != value)
                {
                    _startWorkCommand = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand CompleteWorkCommand
        {
            get { return _completeWorkCommand; }
            set
            {
                if (_completeWorkCommand != value)
                {
                    _completeWorkCommand = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand CancelWorkCommand
        {
            get { return _cancelWorkCommand; }
            set
            {
                if (_cancelWorkCommand != value)
                {
                    _cancelWorkCommand = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        // 속성
        public DateTime WorkDate
        {
            get => _workDate;
            set
            {
                if (_workDate != value)
                {
                    _workDate = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SelectedShift
        {
            get => _selectedShift;
            set
            {
                if (_selectedShift != value)
                {
                    _selectedShift = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SelectedLine
        {
            get => _selectedLine;
            set
            {
                if (_selectedLine != value)
                {
                    _selectedLine = value;
                    OnPropertyChanged();
                }
            }
        }

        public WorkOrderModel SelectedWorkOrder
        {
            get => _selectedWorkOrder;
            set
            {
                if (_selectedWorkOrder != value)
                {
                    _selectedWorkOrder = value;
                    OnPropertyChanged();
                    // 선택된 작업지시 변경 시 이동 버튼 상태 갱신
                    (MoveUpCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (MoveDownCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (SaveSequenceCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        // 컬렉션
        public ObservableCollection<WorkOrderModel> WorkOrders { get; }
        public ObservableCollection<string> Shifts { get; }
        public ObservableCollection<string> ProductionLines { get; }

        // 명령
        public ICommand SearchCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand MoveUpCommand { get; }
        public ICommand MoveDownCommand { get; }
        public ICommand SaveSequenceCommand { get; }

        private async void LoadInitialData()
        {
            try
            {
                await ExecuteSearch();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"데이터 로드 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExecuteStartWork()
        {
            if (SelectedWorkOrder == null || SelectedLine == "전체")
            {
                MessageBox.Show("작업과 라인을 선택해주세요.", "알림");
                return;
            }

            try
            {
                if (!_simulationService.HasAvailableEquipment(SelectedLine))
                {
                    MessageBox.Show("사용 가능한 설비가 없습니다.", "알림");
                    return;
                }

                var line = _simulationService.ProductionLines.FirstOrDefault(l => l.LineId == SelectedLine);
                var availableEquipment = line?.Equipments.FirstOrDefault(e => e.Status == ProductionStatus.Idle);

                using (var connection = ConnectionManager.Instance.GetConnection())
                {
                    var sql = @"
                UPDATE dt_production_plan 
                SET process_status = '작업중',
                    start_time = @StartTime
                WHERE work_order_code = @WorkOrderCode";

                    connection.Execute(sql, new
                    {
                        StartTime = DateTime.Now,
                        WorkOrderCode = SelectedWorkOrder.WorkOrderNumber
                    });
                }

                _simulationService.StartProduction(SelectedLine, availableEquipment.EquipmentId,
                    SelectedWorkOrder.WorkOrderNumber);

                MessageBox.Show($"작업이 시작되었습니다.\n설비: {availableEquipment.EquipmentId}", "알림");

                await ExecuteSearch();
                UpdateEquipmentsList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"작업 시작 중 오류가 발생했습니다: {ex.Message}", "오류");
            }
        }

        private async void ExecuteCompleteWork()
        {
            try
            {
                if (SelectedWorkOrder == null) return;

                var result = MessageBox.Show(
                    $"작업을 완료하시겠습니까?\n생산수량: {SelectedWorkOrder.OrderQuantity}",
                    "작업 완료 확인",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;

                using (var conn = new MySqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // 1. 작업지시 상태 업데이트
                            var sql = @"
                        UPDATE dt_production_plan 
                        SET process_status = '완료',
                            completion_time = @CompletionTime,
                            production_quantity = @ProductionQuantity
                        WHERE work_order_code = @WorkOrderCode";

                            await conn.ExecuteAsync(sql, new
                            {
                                CompletionTime = DateTime.Now,
                                ProductionQuantity = SelectedWorkOrder.OrderQuantity,
                                WorkOrderCode = SelectedWorkOrder.WorkOrderNumber
                            }, transaction);

                            // 2. 재고 증가 처리
                            sql = @"
                        INSERT INTO dt_inventory_management 
                        (product_code, inventory_quantity, unit, 
                         responsible_person, transaction_date, transaction_type, remarks)
                        VALUES 
                        (@ProductCode, @Quantity, @Unit, 
                         @ResponsiblePerson, @TransactionDate, '입고', '생산완료입고')";

                            await conn.ExecuteAsync(sql, new
                            {
                                ProductCode = SelectedWorkOrder.ProductCode,
                                Quantity = SelectedWorkOrder.OrderQuantity,
                                Unit = "EA", // 단위는 실제 제품 단위에 맞게 수정 필요
                                ResponsiblePerson = App.CurrentUser.Username,
                                TransactionDate = DateTime.Now
                            }, transaction);

                            // 로그 기록
                            sql = @"
                        INSERT INTO dt_user_activity_log 
                        (user_id, action_type, action_detail, action_date)
                        VALUES 
                        (@UserId, @ActionType, @ActionDetail, @ActionDate)";

                            await conn.ExecuteAsync(sql, new
                            {
                                UserId = App.CurrentUser.UserId,
                                ActionType = "작업완료",
                                ActionDetail = $"작업번호: {SelectedWorkOrder.WorkOrderNumber}, " +
                                             $"제품: {SelectedWorkOrder.ProductName}, " +
                                             $"수량: {SelectedWorkOrder.OrderQuantity}",
                                ActionDate = DateTime.Now
                            }, transaction);

                            transaction.Commit();

                            SelectedWorkOrder.Status = "완료";
                            await ExecuteSearch(); // 목록 새로고침
                            MessageBox.Show("작업이 완료되었습니다.", "알림");
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw new Exception($"작업 완료 처리 중 오류 발생: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"작업 완료 처리 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExecuteCancel()
        {
            try
            {
                if (MessageBox.Show("작업을 취소하시겠습니까?", "확인",
                    MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                    return;

                using (var conn = new MySqlConnection(_connectionString))
                {
                    var sql = @"
                UPDATE dt_production_plan 
                SET process_status = '지연',
                    employee_name = @EmployeeName
                WHERE work_order_code = @WorkOrderCode";

                    await conn.ExecuteAsync(sql, new
                    {
                        EmployeeName = App.CurrentUser.Username,
                        WorkOrderCode = SelectedWorkOrder.WorkOrderNumber
                    });

                    SelectedWorkOrder.Status = "지연";
                    await ExecuteSearch();
                    UpdateButtonStates();
                    MessageBox.Show("작업이 취소되었습니다.", "알림");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"작업 취소 중 오류가 발생했습니다: {ex.Message}", "오류");
            }
        }

        private bool _canStartWork;
        public bool CanStartWork
        {
            get => _canStartWork;
            set
            {
                if (_canStartWork != value)
                {
                    _canStartWork = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _canCompleteWork;
        public bool CanCompleteWork
        {
            get => _canCompleteWork;
            set
            {
                if (_canCompleteWork != value)
                {
                    _canCompleteWork = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _canCancelWork;
        public bool CanCancelWork
        {
            get => _canCancelWork;
            set
            {
                if (_canCancelWork != value)
                {
                    _canCancelWork = value;
                    OnPropertyChanged();
                }
            }
        }

        private void UpdateButtonStates()
        {
            CanStartWork = SelectedWorkOrder != null && SelectedWorkOrder.Status == "대기";
            CanCompleteWork = SelectedWorkOrder != null && SelectedWorkOrder.Status == "작업중";
            CanCancelWork = SelectedWorkOrder != null && (SelectedWorkOrder.Status == "작업중" || SelectedWorkOrder.Status == "대기");
        }

        private async Task ExecuteSearch()
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    var sql = @"
                    SELECT 
                        pp.work_order_code as WorkOrderNumber,
                        pp.production_date as ProductionDate,
                        p.product_code as ProductCode,
                        p.product_name as ProductName,
                        pp.order_quantity as OrderQuantity,
                        pp.production_quantity as ProductionQuantity,
                        pp.work_order_sequence as Sequence,
                        pp.work_shift as Shift,
                        pp.process_status as Status,
                        pp.production_line as ProductionLine
                    FROM dt_production_plan pp
                    JOIN dt_product p ON pp.product_code = p.product_code
                    WHERE 1=1";

                    var parameters = new DynamicParameters();

                    // 전체 기간 보기가 체크되지 않은 경우, 선택한 날짜만 표시
                    if (!IsAllWorkOrdersChecked)
                    {
                        sql += " AND DATE(pp.production_date) = @WorkDate";
                        parameters.Add("@WorkDate", WorkDate.Date);
                    }

                    // 생산라인 필터
                    if (!string.IsNullOrEmpty(SelectedLine) && SelectedLine != "전체")
                    {
                        sql += " AND pp.production_line = @ProductionLine";
                        parameters.Add("@ProductionLine", SelectedLine);
                    }

                    // 근무조 필터
                    if (!string.IsNullOrEmpty(SelectedShift) && SelectedShift != "전체")
                    {
                        sql += " AND pp.work_shift = @Shift";
                        parameters.Add("@Shift", SelectedShift);
                    }

                    // 완료 항목 필터
                    if (IsCompletedOnlyChecked)
                    {
                        sql += " AND pp.process_status = '완료'";
                    }

                    // 정렬 조건
                    sql += " ORDER BY pp.production_date DESC, pp.work_order_sequence ASC";

                    var result = await conn.QueryAsync<WorkOrderModel>(sql, parameters);

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        WorkOrders.Clear();
                        foreach (var order in result)
                        {
                            WorkOrders.Add(order);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"데이터 로드 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    

    private void ExecuteAdd()
        {
            // TODO: 작업지시 등록 창 구현
            MessageBox.Show("작업지시 등록 기능은 추후 구현 예정입니다.");
        }

        private bool CanExecuteMove()
        {
            return SelectedWorkOrder != null;
        }

        private void ExecuteMoveUp()
        {
            if (SelectedWorkOrder == null) return;

            var index = WorkOrders.IndexOf(SelectedWorkOrder);
            if (index > 0)
            {
                WorkOrders.Move(index, index - 1);
                UpdateSequenceNumbers();
            }
        }

        private void ExecuteMoveDown()
        {
            if (SelectedWorkOrder == null) return;

            var index = WorkOrders.IndexOf(SelectedWorkOrder);
            if (index < WorkOrders.Count - 1)
            {
                WorkOrders.Move(index, index + 1);
                UpdateSequenceNumbers();
            }
        }

        private void UpdateSequenceNumbers()
        {
            for (int i = 0; i < WorkOrders.Count; i++)
            {
                WorkOrders[i].Sequence = i + 1;
            }
        }

        private bool CanExecuteSaveSequence()
        {
            return WorkOrders.Count > 0;
        }

        private async void ExecuteSaveSequence()
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            foreach (var order in WorkOrders)
                            {
                                var sql = @"
                            UPDATE dt_production_plan 
                            SET work_order_sequence = @Sequence 
                            WHERE work_order_code = @WorkOrderNumber";

                                await conn.ExecuteAsync(sql, new
                                {
                                    order.Sequence,
                                    order.WorkOrderNumber
                                }, transaction);
                            }

                            transaction.Commit();
                            MessageBox.Show("작업 순서가 저장되었습니다.", "알림",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"작업 순서 저장 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Cleanup()
        {
            _simulationService.ProductionCompleted -= OnProductionCompleted;
            _simulationService.ProductionStarted -= OnProductionStarted;
            _simulationService.ProductionPaused -= OnProductionPaused;
            _simulationService.ProductionResumed -= OnProductionResumed;
            _simulationService.ProductionError -= OnProductionError;
        }
    }

}