using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows;
using MES.Solution.Helpers;
using MySql.Data.MySqlClient;
using Dapper;
using System.Configuration;
using System.Threading.Tasks;
using System.Linq;
using MES.Solution.Models;
using MES.Solution.Services;


namespace MES.Solution.ViewModels
{
    public class ProductionPlanAddViewModel : INotifyPropertyChanged
    {
        private readonly LogService _logService; // LogService 추가

        private readonly string _connectionString;
        private readonly bool _isEditMode;
        private string _originalWorkOrderCode;
        private DateTime _productionDate = DateTime.Today;
        private string _selectedProductionLine;
        private ProductModel _selectedProduct;
        private int _orderQuantity;
        private string _selectedWorkShift;
        private string _remarks;
        private string _windowTitle;
        private string _errormessage;
        private bool _haserror;
        private DateTime _minimumDate = DateTime.Today;
        private FormMode _mode;

        // 유효성 검증 플래그
        private bool _productionDateValid = true;
        private bool _productionLineValid = true;
        private bool _productValid = true;
        private bool _orderQuantityValid = true;
        private bool _workShiftValid = true;

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler RequestClose;


        public ProductionPlanAddViewModel(bool isEdit = false)
        {
            _connectionString = ConfigurationManager.ConnectionStrings["MESDatabase"].ConnectionString;
            _logService = new LogService(); // LogService 초기화
            _isEditMode = isEdit;
            Mode = isEdit ? FormMode.Edit : FormMode.Add;

            // 콤보박스 데이터 초기화
            ProductionLines = new ObservableCollection<string> { "라인1", "라인2", "라인3" };
            WorkShifts = new ObservableCollection<string> { "주간1", "주간2", "야간1", "야간2" };
            Products = new ObservableCollection<ProductModel>();

            // 커맨드 초기화    
            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            CancelCommand = new RelayCommand(ExecuteCancel);

            WindowTitle = isEdit ? "생산계획 수정" : "생산계획 등록";

            // 데이터 초기화
            InitializeData();

            // 초기 유효성 검사 실행
            ValidateAll();
        }

        public FormMode Mode
        {
            get => _mode;
            set
            {
                if (_mode != value)
                {
                    _mode = value;
                    WindowTitle = value == FormMode.Add ? "생산계획 등록" : "생산계획 수정";
                    OnPropertyChanged();
                }
            }
        }

        #region Properties

        public DateTime MinimumDate
        {
            get => _minimumDate;
            set
            {
                if (_minimumDate != value)
                {
                    _minimumDate = value;
                    OnPropertyChanged();
                    (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public string WindowTitle
        {
            get => _windowTitle;
            set
            {
                if (_windowTitle != value)
                {
                    _windowTitle = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime ProductionDate
        {
            get => _productionDate;
            set
            {
                if (_productionDate != value)
                {
                    _productionDate = value;
                    ValidateProductionDate();
                    OnPropertyChanged();
                    (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public string SelectedProductionLine
        {
            get => _selectedProductionLine;
            set
            {
                if (_selectedProductionLine != value)
                {
                    _selectedProductionLine = value;
                    ValidateProductionLine();
                    OnPropertyChanged();
                    (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public ProductModel SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                if (_selectedProduct != value)
                {
                    _selectedProduct = value;
                    ValidateProduct();
                    OnPropertyChanged();
                    (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public int OrderQuantity
        {
            get => _orderQuantity;
            set
            {
                if (_orderQuantity != value)
                {
                    _orderQuantity = value;
                    ValidateOrderQuantity();
                    OnPropertyChanged();
                    (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public string SelectedWorkShift
        {
            get => _selectedWorkShift;
            set
            {
                if (_selectedWorkShift != value)
                {
                    _selectedWorkShift = value;
                    ValidateWorkShift();
                    OnPropertyChanged();
                    (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public string Remarks
        {
            get => _remarks;
            set
            {
                if (_remarks != value)
                {
                    _remarks = value?.Length > 200 ? value.Substring(0, 200) : value;
                    OnPropertyChanged();
                }
            }
        }

        public string ErrorMessage
        {
            get => _errormessage;
            set
            {
                _errormessage = value;
                OnPropertyChanged();
                HasError = !string.IsNullOrEmpty(value);
            }
        }

        public bool HasError
        {
            get => _haserror;
            set
            {
                _haserror = value;
                OnPropertyChanged();
            }
        }

        // 유효성 검증 속성
        public bool ProductionDateValidation
        {
            get => _productionDateValid;
            set
            {
                _productionDateValid = value;
                OnPropertyChanged();
            }
        }

        public bool ProductionLineValidation
        {
            get => _productionLineValid;
            set
            {
                _productionLineValid = value;
                OnPropertyChanged();
            }
        }

        public bool ProductValidation
        {
            get => _productValid;
            set
            {
                _productValid = value;
                OnPropertyChanged();
            }
        }

        public bool OrderQuantityValidation
        {
            get => _orderQuantityValid;
            set
            {
                _orderQuantityValid = value;
                OnPropertyChanged();
            }
        }

        public bool WorkShiftValidation
        {
            get => _workShiftValid;
            set
            {
                _workShiftValid = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Collections

        public ObservableCollection<string> ProductionLines { get; } = new ObservableCollection<string>();
        public ObservableCollection<ProductModel> Products { get; } = new ObservableCollection<ProductModel>();
        public ObservableCollection<string> WorkShifts { get; } = new ObservableCollection<string>();

        #endregion

        #region Commands

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        #endregion

        #region Methods

        private async void InitializeData()
        {
            try
            {
                // 생산라인 초기화
                ProductionLines.Clear();
                ProductionLines.Add("라인1");
                ProductionLines.Add("라인2");
                ProductionLines.Add("라인3");

                // 근무조 초기화
                WorkShifts.Clear();
                WorkShifts.Add("주간1");
                WorkShifts.Add("주간2");
                WorkShifts.Add("야간1");
                WorkShifts.Add("야간2");

                // 제품 목록 로드
                await LoadProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"초기 데이터 로드 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadProducts()
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                var sql = "SELECT product_code as ProductCode, product_name as ProductName, unit as Unit FROM dt_product ORDER BY product_name";
                var products = await conn.QueryAsync<ProductModel>(sql);

                Products.Clear();
                foreach (var product in products)
                {
                    Products.Add(product);
                }
            }
        }

        public async void LoadData(ProductionPlanModel plan)
        {
            if (plan == null) return;

            try
            {
                _originalWorkOrderCode = plan.PlanNumber;  // 원본 작업지시 번호 저장
                ProductionDate = plan.PlanDate;
                SelectedProductionLine = plan.ProductionLine;

                // 제품 정보 로드
                await LoadProducts();  // 제품 목록을 먼저 로드

                // 제품 선택
                SelectedProduct = Products.FirstOrDefault(p => p.ProductCode == plan.ProductCode);

                OrderQuantity = plan.PlannedQuantity;
                SelectedWorkShift = plan.WorkShift;
                Remarks = plan.Remarks;

                // 유효성 검사 실행
                ValidateAll();

                // Mode 설정
                Mode = FormMode.Edit;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"데이터 로드 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadDataForEdit(string workOrderCode)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    var sql = @"
                        SELECT 
                            production_date as ProductionDate,
                            production_line as ProductionLine,
                            product_code as ProductCode,
                            order_quantity as OrderQuantity,
                            work_shift as WorkShift,
                            remarks as Remarks
                        FROM dt_production_plan 
                        WHERE work_order_code = @WorkOrderCode";

                    var plan = conn.QueryFirstOrDefault<dynamic>(sql, new { WorkOrderCode = workOrderCode });

                    if (plan != null)
                    {
                        _originalWorkOrderCode = workOrderCode;
                        ProductionDate = plan.ProductionDate;
                        SelectedProductionLine = plan.ProductionLine;
                        SelectedProduct = Products.FirstOrDefault(p => p.ProductCode == plan.ProductCode);
                        OrderQuantity = plan.OrderQuantity;
                        SelectedWorkShift = plan.WorkShift;
                        Remarks = plan.Remarks;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"데이터 로드 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateAll()
        {
            ValidateProductionDate();
            ValidateProductionLine();
            ValidateProduct();
            ValidateOrderQuantity();
            ValidateWorkShift();

            bool isValid = ProductionDateValidation &&
                          ProductionLineValidation &&
                          ProductValidation &&
                          OrderQuantityValidation &&
                          WorkShiftValidation &&
                          !string.IsNullOrEmpty(SelectedProductionLine) &&
                          SelectedProduct != null &&
                          OrderQuantity > 0 &&
                          !string.IsNullOrEmpty(SelectedWorkShift);

            // 명령 상태 업데이트
            (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();

            return isValid;
        }

        private void ValidateProductionDate()
        {
            if (_isEditMode)
            {
                ProductionDateValidation = true;
            }
            else
            {
                // 신규 등록의 경우에만 현재 날짜 이후로 제한
                ProductionDateValidation = ProductionDate >= DateTime.Today;
            }

            if (!ProductionDateValidation)
            {
                ErrorMessage = "생산일자는 현재 날짜 이후여야 합니다.";
            }
            else if (string.IsNullOrEmpty(ErrorMessage) || ErrorMessage.Contains("생산일자"))
            {
                ErrorMessage = string.Empty;
            }
                (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void ValidateProductionLine()
        {
            ProductionLineValidation = !string.IsNullOrEmpty(SelectedProductionLine);
            if (!ProductionLineValidation)
            {
                ErrorMessage = "생산라인을 선택해주세요.";
            }
            else if (string.IsNullOrEmpty(ErrorMessage) || ErrorMessage.Contains("생산라인"))
            {
                ErrorMessage = string.Empty;
            }
                 (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void ValidateProduct()
        {
            ProductValidation = SelectedProduct != null;
            if (!ProductValidation)
            {
                ErrorMessage = "제품을 선택해주세요.";
            }
            else if (string.IsNullOrEmpty(ErrorMessage) || ErrorMessage.Contains("제품"))
            {
                ErrorMessage = string.Empty;
            }
               (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void ValidateOrderQuantity()
        {
            OrderQuantityValidation = OrderQuantity > 0;
            if (!OrderQuantityValidation)
            {
                ErrorMessage = "지시수량은 0보다 커야 합니다.";
            }
            else if (string.IsNullOrEmpty(ErrorMessage) || ErrorMessage.Contains("지시수량"))
            {
                ErrorMessage = string.Empty;
            }
               (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();

        }

        private void ValidateWorkShift()
        {
            WorkShiftValidation = !string.IsNullOrEmpty(SelectedWorkShift);
            if (!WorkShiftValidation)
            {
                ErrorMessage = "근무조를 선택해주세요.";
            }
            else if (string.IsNullOrEmpty(ErrorMessage) || ErrorMessage.Contains("근무조"))
            {
                ErrorMessage = string.Empty;
            }
                 (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();

        }

        private bool CanExecuteSave()
        {
            bool dateValid = _isEditMode || ProductionDateValidation;

            bool isValid = ProductionDateValidation &&
                          ProductionLineValidation &&
                          ProductValidation &&
                          OrderQuantityValidation &&
                          WorkShiftValidation &&
                          !string.IsNullOrEmpty(SelectedProductionLine) &&
                          SelectedProduct != null &&
                          OrderQuantity > 0 &&
                          !string.IsNullOrEmpty(SelectedWorkShift);

            return isValid;
        }

        private async void ExecuteSave()
        {
            try
            {
                if (!ValidateAll())
                {
                    return;
                }

                using (var conn = new MySqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    string sql;
                    var parameters = new DynamicParameters();
                    string workOrderCode = _originalWorkOrderCode; // 수정 모드일 때 사용할 기존 코드

                    if (Mode == FormMode.Edit)
                    {
                        sql = @"UPDATE dt_production_plan 
                    SET production_date = @ProductionDate,
                        production_line = @ProductionLine,
                        product_code = @ProductCode,
                        order_quantity = @OrderQuantity,
                        work_shift = @WorkShift,
                        remarks = @Remarks,
                        employee_name = @EmployeeName
                    WHERE work_order_code = @WorkOrderCode";

                        parameters.Add("@WorkOrderCode", _originalWorkOrderCode);
                    }
                    else
                    {
                        // 신규 등록 시 시퀀스 번호를 먼저 생성
                        var sequenceQuery = @"
                    SELECT IFNULL(MAX(CAST(SUBSTRING_INDEX(work_order_code, '-', -1) AS UNSIGNED)), 0) + 1
                    FROM dt_production_plan 
                    WHERE DATE(production_date) = @ProductionDate";

                        var nextSequence = await conn.QuerySingleAsync<int>(sequenceQuery, new { ProductionDate });
                        workOrderCode = $"PP-{ProductionDate:yyyyMMdd}-{nextSequence:D3}";

                        sql = @"INSERT INTO dt_production_plan (
                        work_order_code,
                        production_date,
                        production_line,
                        product_code,
                        order_quantity,
                        work_shift,
                        process_status,
                        work_order_sequence,
                        remarks,
                        employee_name
                    ) VALUES (
                        @WorkOrderCode,
                        @ProductionDate,
                        @ProductionLine,
                        @ProductCode,
                        @OrderQuantity,
                        @WorkShift,
                        @ProcessStatus,
                        @WorkOrderSequence,
                        @Remarks,
                        @EmployeeName
                    )";

                        parameters.Add("@WorkOrderCode", workOrderCode);
                        parameters.Add("@ProcessStatus", "대기");
                        parameters.Add("@WorkOrderSequence", nextSequence);
                    }

                    // 공통 파라미터 추가
                    parameters.Add("@ProductionDate", ProductionDate);
                    parameters.Add("@ProductionLine", SelectedProductionLine);
                    parameters.Add("@ProductCode", SelectedProduct.ProductCode);
                    parameters.Add("@OrderQuantity", OrderQuantity);
                    parameters.Add("@WorkShift", SelectedWorkShift);
                    parameters.Add("@Remarks", Remarks);
                    parameters.Add("@EmployeeName", App.CurrentUser.Username);

                    // 로그에 필요한 정보 준비 (이제 workOrderCode 사용 가능)
                    string actionType = Mode == FormMode.Edit ? "생산계획 수정" : "생산계획 등록";
                    string actionDetail = $"작업지시번호: {workOrderCode}, " +
                                        $"생산일자: {ProductionDate:yyyy-MM-dd}, " +
                                        $"생산라인: {SelectedProductionLine}, " +
                                        $"제품: {SelectedProduct?.ProductName}, " +
                                        $"수량: {OrderQuantity}, " +
                                        $"근무조: {SelectedWorkShift}";

                    await conn.ExecuteAsync(sql, parameters);

                    // 로그 저장
                    await _logService.SaveLogAsync(App.CurrentUser.UserId, actionType, actionDetail);

                    MessageBox.Show(Mode == FormMode.Edit ? "수정되었습니다." : "등록되었습니다.",
                        "알림", MessageBoxButton.OK, MessageBoxImage.Information);

                    RequestClose?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteCancel()
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    public enum FormMode
    {
        Add,
        Edit
    }
    public class ProductModel
    {
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public string Unit { get; set; }

        public override string ToString()
        {
            return ProductName;
        }
    }
}