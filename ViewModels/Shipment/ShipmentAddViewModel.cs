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
    public class ShipmentAddViewModel : INotifyPropertyChanged
    {
        private readonly LogService _logService;

        private readonly string _connectionString;
        private readonly bool _isEditMode;
        private string _originalShipmentNumber;
        private DateTime _shipmentDate = DateTime.Today;
        private string _selectedCompanyCode;
        private string _selectedCompanyName;
        private ProductModel _selectedProduct;
        private DateTime _productionDate = DateTime.Today;
        private int _shipmentQuantity;
        private string _vehicleNumber;
        private string _windowTitle;
        private string _errormessage;
        private bool _haserror;

        // 유효성 검증 플래그
        private bool _shipmentDateValid = true;
        private bool _companyValid = true;
        private bool _productValid = true;
        private bool _productionDateValid = true;
        private bool _quantityValid = true;
        private bool _vehicleNumberValid = true;

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler RequestClose;

        public ShipmentAddViewModel(bool isEdit = false)
        {
            _connectionString = ConfigurationManager.ConnectionStrings["MESDatabase"].ConnectionString;
            _logService = new LogService(); // LogService 초기화
            _isEditMode = isEdit;
            Mode = isEdit ? FormMode.Edit : FormMode.Add;

            // 콤보박스 데이터 초기화
            Products = new ObservableCollection<ProductModel>();
            Companies = new ObservableCollection<CompanyModel>();

            // 커맨드 초기화    
            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            CancelCommand = new RelayCommand(ExecuteCancel);

            WindowTitle = isEdit ? "출하 수정" : "출하 등록";

            // 데이터 초기화
            InitializeData();

            // 초기 유효성 검사 실행
            ValidateAll();
        }

        #region Properties

        public FormMode Mode { get; set; }

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

        public DateTime ShipmentDate
        {
            get => _shipmentDate;
            set
            {
                if (_shipmentDate != value)
                {
                    _shipmentDate = value;
                    ValidateShipmentDate();
                    OnPropertyChanged();
                }
            }
        }

        public string SelectedCompanyCode
        {
            get => _selectedCompanyCode;
            set
            {
                if (_selectedCompanyCode != value)
                {
                    _selectedCompanyCode = value;
                    ValidateCompany();
                    OnPropertyChanged();
                }
            }
        }

        public string SelectedCompanyName
        {
            get => _selectedCompanyName;
            set
            {
                if (_selectedCompanyName != value)
                {
                    _selectedCompanyName = value;
                    ValidateCompany();  // 거래처명 유효성 검사
                    OnPropertyChanged();
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
                }
            }
        }

        public int ShipmentQuantity
        {
            get => _shipmentQuantity;
            set
            {
                if (_shipmentQuantity != value)
                {
                    _shipmentQuantity = value;
                    ValidateQuantity();
                    OnPropertyChanged();
                }
            }
        }

        public string VehicleNumber
        {
            get => _vehicleNumber;
            set
            {
                if (_vehicleNumber != value)
                {
                    _vehicleNumber = value;
                    ValidateVehicleNumber();
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

        #endregion

        #region Collections
        public ObservableCollection<ProductModel> Products { get; }
        public ObservableCollection<CompanyModel> Companies { get; }
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
                await LoadProducts();
                await LoadCompanies();
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

        private async Task LoadCompanies()
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                var sql = "SELECT DISTINCT company_code, company_name FROM dt_shipment ORDER BY company_name";
                var companies = await conn.QueryAsync<CompanyModel>(sql);

                Companies.Clear();
                foreach (var company in companies)
                {
                    Companies.Add(company);
                }
            }
        }

        public void LoadData(ShipmentModel shipment)
        {
            if (shipment == null) return;

            try
            {
                _originalShipmentNumber = shipment.ShipmentNumber;
                ShipmentDate = shipment.ShipmentDate;
                SelectedCompanyName = shipment.CompanyName;  // 거래처명 설정
                SelectedProduct = Products.FirstOrDefault(p => p.ProductCode == shipment.ProductCode);
                ProductionDate = shipment.ProductionDate;
                ShipmentQuantity = shipment.ShipmentQuantity;
                VehicleNumber = shipment.VehicleNumber;

                ValidateAll();
                Mode = FormMode.Edit;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"데이터 로드 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private bool ValidateAll()
        {
            ValidateShipmentDate();
            ValidateCompany();
            ValidateProduct();
            ValidateProductionDate();
            ValidateQuantity();
            ValidateVehicleNumber();

            return _shipmentDateValid && _companyValid && _productValid &&
                   _productionDateValid && _quantityValid && _vehicleNumberValid;
        }

        private void ValidateShipmentDate()
        {
            _shipmentDateValid = true;
            if (!_isEditMode && ShipmentDate < DateTime.Today)
            {
                _shipmentDateValid = false;
                ErrorMessage = "출하일자는 현재 날짜 이후여야 합니다.";
            }
            (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void ValidateCompany()
        {
            _companyValid = !string.IsNullOrEmpty(SelectedCompanyName);
            if (!_companyValid)
            {
                ErrorMessage = "거래처명을 입력해주세요.";
            }
            else
            {
                ErrorMessage = string.Empty;
            }
        (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void ValidateProduct()
        {
            _productValid = SelectedProduct != null;
            if (!_productValid)
            {
                ErrorMessage = "제품을 선택해주세요.";
            }
            (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void ValidateProductionDate()
        {
            _productionDateValid = ProductionDate <= DateTime.Today;
            if (!_productionDateValid)
            {
                ErrorMessage = "생산일자는 현재 날짜 이전이어야 합니다.";
            }
            (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void ValidateQuantity()
        {
            _quantityValid = ShipmentQuantity > 0;
            if (!_quantityValid)
            {
                ErrorMessage = "출하수량은 0보다 커야 합니다.";
            }
            (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void ValidateVehicleNumber()
        {
            _vehicleNumberValid = !string.IsNullOrEmpty(VehicleNumber);
            if (!_vehicleNumberValid)
            {
                ErrorMessage = "차량번호를 입력해주세요.";
            }
            else
            {
                if (string.IsNullOrEmpty(ErrorMessage) || ErrorMessage == "차량번호를 입력해주세요.")
                {
                    ErrorMessage = string.Empty;
                }
            }
    (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private bool CanExecuteSave()
        {
            return ValidateAll();
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
                    string shipmentNumber = _originalShipmentNumber;

                    if (Mode == FormMode.Edit)
                    {
                        sql = @"UPDATE dt_shipment 
                           SET shipment_date = @ShipmentDate,
                               company_code = @CompanyCode,
                               company_name = @CompanyName,
                               product_code = @ProductCode,
                               production_date = @ProductionDate,
                               shipment_quantity = @ShipmentQuantity,
                               vehicle_number = @VehicleNumber,
                               employee_name = @EmployeeName
                           WHERE shipment_number = @ShipmentNumber";

                        parameters.Add("@ShipmentNumber", _originalShipmentNumber);
                    }
                    else
                    {
                        var sequenceQuery = @"
                        SELECT IFNULL(MAX(CAST(SUBSTRING_INDEX(shipment_number, '-', -1) AS UNSIGNED)), 0) + 1
                        FROM dt_shipment 
                        WHERE DATE(shipment_date) = @ShipmentDate";

                        var nextSequence = await conn.QuerySingleAsync<int>(sequenceQuery, new { ShipmentDate });
                        shipmentNumber = $"SH-{ShipmentDate:yyyyMMdd}-{nextSequence:D3}";

                        sql = @"INSERT INTO dt_shipment (
                        shipment_number, company_code, company_name, product_code,
                        production_date, shipment_date, shipment_quantity,
                        vehicle_number, employee_name
                    ) VALUES (
                        @ShipmentNumber, @CompanyCode, @CompanyName, @ProductCode,
                        @ProductionDate, @ShipmentDate, @ShipmentQuantity,
                        @VehicleNumber, @EmployeeName
                    )";

                        parameters.Add("@ShipmentNumber", shipmentNumber);
                    }

                    // 회사 코드는 회사명에서 자동 생성 (예: 앞 3글자)
                    string companyCode = string.IsNullOrEmpty(SelectedCompanyName) ? "" :
                        SelectedCompanyName.Length > 3 ? SelectedCompanyName.Substring(0, 3) : SelectedCompanyName;

                    parameters.Add("@ShipmentDate", ShipmentDate);
                    parameters.Add("@CompanyCode", companyCode);
                    parameters.Add("@CompanyName", SelectedCompanyName);
                    parameters.Add("@ProductCode", SelectedProduct.ProductCode);
                    parameters.Add("@ProductionDate", ProductionDate);
                    parameters.Add("@ShipmentQuantity", ShipmentQuantity);
                    parameters.Add("@VehicleNumber", VehicleNumber);
                    parameters.Add("@EmployeeName", App.CurrentUser.Username);

                    await conn.ExecuteAsync(sql, parameters);

                    // 로그 저장
                    string actionType = Mode == FormMode.Edit ? "출하내역 수정" : "출하내역 등록";
                    string actionDetail = $"출하번호: {shipmentNumber}, " +
                                        $"출하일자: {ShipmentDate:yyyy-MM-dd}, " +
                                        $"거래처: {SelectedCompanyName}, " +
                                        $"제품: {SelectedProduct?.ProductName}, " +
                                        $"수량: {ShipmentQuantity}, " +
                                        $"차량번호: {VehicleNumber}";

                    await _logService.SaveLogAsync(App.CurrentUser.UserId, actionType, actionDetail);

                    MessageBox.Show(Mode == FormMode.Edit ? "수정되었습니다." : "등록되었습니다.",
                        "알림", MessageBoxButton.OK, MessageBoxImage.Information);

                    RequestClose?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"저장 중 오류가 발생했습니다: {ex.Message}",
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


    public class CompanyModel
    {
        public string CompanyCode { get; set; }
        public string CompanyName { get; set; }

        public override string ToString()
        {
            return CompanyName;
        }
    }
}