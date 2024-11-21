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
using MES.Solution.Services;

namespace MES.Solution.ViewModels
{
    public class InventoryManagementViewModel : INotifyPropertyChanged
    {
        private readonly string _connectionString;
        private bool _isNewTransaction = true;
        private string _selectedTransactionType;
        private DateTime _transactionDate = DateTime.Now;
        private InventoryProductModel _selectedProduct;
        private int _quantity;
        private string _responsiblePerson;
        private string _adjustmentReason;
        private bool _hasError;
        private string _errorMessage;
        private int _currentStock;
        private FormMode _mode;
        private readonly LogService _logService;
        private string _windowTitle;
        private DateTime _originalTransactionDate;
        private string _originalTransactionType;
        private bool _isAddition;
        private bool _isSubtraction;

        public event EventHandler RequestClose;
        public event PropertyChangedEventHandler PropertyChanged;

        public InventoryManagementViewModel()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["MESDatabase"].ConnectionString;
            _logService = new LogService();
            Mode = FormMode.Add;

            // 컬렉션 초기화
            Products = new ObservableCollection<InventoryProductModel>();
            TransactionTypes = new ObservableCollection<string> { "입고", "출고", "재고조정" };

            // 명령 초기화
            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            CancelCommand = new RelayCommand(ExecuteCancel);

            // 담당자 초기화
            ResponsiblePerson = App.CurrentUser?.Username;

            // 초기 데이터 로드
            _ = LoadInitialData();

            // 윈도우 타이틀 초기화
            WindowTitle = "재고 등록";
        }

        public bool IsAddition
        {
            get => _isAddition;
            set
            {
                if (_isAddition != value)
                {
                    _isAddition = value;
                    OnPropertyChanged();
                    ValidateQuantity();
                    (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsSubtraction
        {
            get => _isSubtraction;
            set
            {
                if (_isSubtraction != value)
                {
                    _isSubtraction = value;
                    OnPropertyChanged();
                    ValidateQuantity();
                    (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }


        public FormMode Mode
        {
            get => _mode;
            set
            {
                if (_mode != value)
                {
                    _mode = value;
                    WindowTitle = value == FormMode.Add ? "재고 등록" : "재고 수정";
                    OnPropertyChanged();
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

        // 속성
        public bool IsNewTransaction
        {
            get => _isNewTransaction;
            set
            {
                if (_isNewTransaction != value)
                {
                    _isNewTransaction = value;
                    OnPropertyChanged();
                }
            }
        }

        //public string WindowTitle => IsNewTransaction ? "재고 등록" : "재고 수정";

        public string SelectedTransactionType
        {
            get => _selectedTransactionType;
            set
            {
                if (_selectedTransactionType != value)
                {
                    _selectedTransactionType = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsAdjustment));
                    ValidateQuantity();
                    (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public DateTime TransactionDate
        {
            get => _transactionDate;
            set
            {
                if (_transactionDate != value)
                {
                    _transactionDate = value;
                    OnPropertyChanged();
                    (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public InventoryProductModel SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                if (_selectedProduct != value)
                {
                    _selectedProduct = value;
                    OnPropertyChanged();
                    _ = LoadCurrentStock();
                    ValidateQuantity();
                    (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        private bool _dialogResult;
        public bool DialogResult
        {
            get => _dialogResult;
            private set
            {
                _dialogResult = value;
                OnPropertyChanged();
            }
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged();
                    ValidateQuantity();
                    (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public string ResponsiblePerson
        {
            get => _responsiblePerson;
            set
            {
                if (_responsiblePerson != value)
                {
                    _responsiblePerson = value;
                    OnPropertyChanged();
                    (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public string AdjustmentReason
        {
            get => _adjustmentReason;
            set
            {
                if (_adjustmentReason != value)
                {
                    _adjustmentReason = value;
                    OnPropertyChanged();
                    (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public bool HasError
        {
            get => _hasError;
            set
            {
                if (_hasError != value)
                {
                    _hasError = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value;
                    OnPropertyChanged();
                    HasError = !string.IsNullOrEmpty(value);
                }
            }
        }

        public int CurrentStock
        {
            get => _currentStock;
            set
            {
                if (_currentStock != value)
                {
                    _currentStock = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsAdjustment => SelectedTransactionType == "재고조정";

        // 컬렉션
        public ObservableCollection<InventoryProductModel> Products { get; }
        public ObservableCollection<string> TransactionTypes { get; }

        // 명령
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }



        public async Task LoadInventoryForEdit(InventoryModel selectedInventory)
        {
            await LoadInitialData();

            try
            {
                // 새로운 재고조정으로 설정
                IsNewTransaction = true;
                TransactionDate = DateTime.Now;
                SelectedProduct = Products.FirstOrDefault(p => p.ProductCode == selectedInventory.ProductCode);
                ResponsiblePerson = App.CurrentUser?.Username;

                // 라디오 버튼 초기화
                IsAddition = true;
                IsSubtraction = false;

                // 수량 초기화
                Quantity = 0;

                // 현재 재고 로드
                await LoadCurrentStock();

                // Mode 설정 - 신규 등록으로
                Mode = FormMode.Add;
                WindowTitle = "재고 조정";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"데이터 로드 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async Task LoadInitialData()
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    var sql = @"
                        SELECT 
                            product_code as ProductCode,
                            product_name as ProductName,
                            unit as Unit
                        FROM dt_product 
                        ORDER BY product_name";

                    var products = await conn.QueryAsync<InventoryProductModel>(sql);

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Products.Clear();
                        foreach (var product in products)
                        {
                            Products.Add(product);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"제품 목록을 불러오는 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadInventoryData(int inventoryId)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    var sql = @"
                        SELECT 
                            i.product_code,
                            i.inventory_quantity,
                            i.transaction_type,
                            i.transaction_date,
                            i.responsible_person,
                            i.remarks as adjustment_reason
                        FROM dt_inventory_management i
                        WHERE i.inventory_id = @InventoryId";

                    var inventory = await conn.QuerySingleOrDefaultAsync<dynamic>(sql, new { InventoryId = inventoryId });

                    if (inventory != null)
                    {
                        SelectedProduct = Products.FirstOrDefault(p => p.ProductCode == inventory.product_code);
                        Quantity = inventory.inventory_quantity;
                        SelectedTransactionType = inventory.transaction_type;
                        TransactionDate = inventory.transaction_date;
                        ResponsiblePerson = inventory.responsible_person;
                        AdjustmentReason = inventory.adjustment_reason;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"재고 정보를 불러오는 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadCurrentStock()
        {
            if (SelectedProduct == null) return;

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    var sql = @"
                        SELECT 
                            COALESCE(SUM(CASE 
                                WHEN transaction_type = '입고' THEN inventory_quantity
                                WHEN transaction_type = '출고' THEN -inventory_quantity
                                ELSE 0
                            END), 0) as current_stock
                        FROM dt_inventory_management
                        WHERE product_code = @ProductCode";

                    CurrentStock = await conn.QuerySingleAsync<int>(sql, new { ProductCode = SelectedProduct.ProductCode });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"현재 재고를 확인하는 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExecuteSave()
        {
            return !HasError &&
                   (IsAddition || IsSubtraction) && // 라디오버튼 선택 필수
                   SelectedProduct != null &&
                   Quantity > 0 &&
                   !string.IsNullOrEmpty(ResponsiblePerson) &&
                   !(IsSubtraction && Quantity > CurrentStock); // 제거 수량 체크
        }


        private void ValidateQuantity()
        {
            ErrorMessage = string.Empty;

            if (Quantity <= 0)
            {
                ErrorMessage = "수량은 0보다 커야 합니다.";
                return;
            }

            if (IsSubtraction && Quantity > CurrentStock)
            {
                ErrorMessage = "제거할 수량이 현재 재고보다 많습니다.";
                return;
            }
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

                    var parameters = new DynamicParameters();
                    parameters.Add("@ProductCode", SelectedProduct.ProductCode);
                    parameters.Add("@Quantity", Quantity);
                    parameters.Add("@Unit", SelectedProduct.Unit);
                    parameters.Add("@ResponsiblePerson", ResponsiblePerson);
                    parameters.Add("@TransactionDate", DateTime.Now);
                    parameters.Add("@TransactionType", $"{(IsAddition ? "입고" : "출고")}");
                    parameters.Add("@Remarks", $"{("재고관리")}  {AdjustmentReason}");

                    string sql = @"INSERT INTO dt_inventory_management (
                    product_code,
                    inventory_quantity,
                    unit,
                    responsible_person,
                    transaction_date,
                    transaction_type,
                    remarks
                ) VALUES (
                    @ProductCode,
                    @Quantity,
                    @Unit,
                    @ResponsiblePerson,
                    @TransactionDate,
                    @TransactionType,
                    @Remarks
                )";

                    await conn.ExecuteAsync(sql, parameters);

                    // 로그 저장
                    string actionDetail = $"제품: {SelectedProduct.ProductName}, " +
                                        $"조정유형: {(IsAddition ? "추가" : "제거")}, " +
                                        $"수량: {Quantity}, " +
                                        $"담당자: {ResponsiblePerson}";

                    await _logService.SaveLogAsync(App.CurrentUser.UserId, "재고조정", actionDetail);

                    MessageBox.Show("재고가 조정되었습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);

                    RequestClose?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"재고 조정 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void ExecuteCancel()
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        private bool ValidateAll()
        {
            ValidateQuantity();
            return !HasError &&
                   //!string.IsNullOrEmpty(SelectedTransactionType) &&
                   SelectedProduct != null &&
                   Quantity > 0 &&
                   !string.IsNullOrEmpty(ResponsiblePerson) &&
                   (!IsAdjustment || !string.IsNullOrEmpty(AdjustmentReason));
        }


        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class InventoryProductModel
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