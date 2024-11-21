using Dapper;
using MES.Solution.Helpers;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using MES.Solution.Views;
using MES.Solution.Services;

namespace MES.Solution.ViewModels
{
    public class ShipmentViewModel : INotifyPropertyChanged
    {
        private readonly string _connectionString;
        private DateTime _startDate = DateTime.Now.AddDays(1 - DateTime.Now.Day); //이번달 1일
        private DateTime _endDate = DateTime.Now.AddDays(1 - DateTime.Now.Day).AddMonths(1).AddDays(-1);//이번달 말일
        private string _selectedCompany = "전체";
        private string _selectedProduct = "전체";
        private ShipmentModel _selectedShipment;

        private readonly LogService _logService;

        private ShipmentAddWindow _currentAddWindow;
        public ICommand DeleteCommand { get; }

        public ICommand ConfirmShipmentCommand { get; }

        public ICommand CancelShipmentCommand { get; }

        public ShipmentViewModel()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["MESDatabase"].ConnectionString;
            _logService = new LogService(); // LogService 초기화

            // 기본값 설정
            IsAllPeriodChecked = false;
            IsConfirmedOnlyChecked = false;
            StartDate = DateTime.Now.Date;
            EndDate = DateTime.Now.Date;

            // 컬렉션 초기화
            Shipments = new ObservableCollection<ShipmentModel>();
            Companies = new ObservableCollection<string>();
            Products = new ObservableCollection<string>();

            // 명령 초기화
            SearchCommand = new RelayCommand(async () => await ExecuteSearch());
            AddCommand = new RelayCommand(ExecuteAdd);
            DeleteCommand = new RelayCommand(async () => await ExecuteDelete(), CanExecuteDelete);
            ConfirmShipmentCommand = new AsyncRelayCommand(ExecuteConfirmShipment, CanExecuteConfirmShipment);
            CancelShipmentCommand = new AsyncRelayCommand(ExecuteCancelShipment, CanExecuteCancelShipment);


            // 초기 데이터 로드
            LoadInitialData();
        }

        private bool _isAllPeriodChecked;
        private bool _isConfirmedOnlyChecked;

        public bool IsAllPeriodChecked
        {
            get => _isAllPeriodChecked;
            set
            {
                if (_isAllPeriodChecked != value)
                {
                    _isAllPeriodChecked = value;
                    OnPropertyChanged();
                    _ = ExecuteSearch();
                }
            }
        }

        public bool IsConfirmedOnlyChecked
        {
            get => _isConfirmedOnlyChecked;
            set
            {
                if (_isConfirmedOnlyChecked != value)
                {
                    _isConfirmedOnlyChecked = value;
                    OnPropertyChanged();
                    _ = ExecuteSearch();
                }
            }
        }

        private bool CanExecuteConfirmShipment()
        {
            return Shipments.Any(x => x.IsSelected && x.Status == "대기");
        }

        private bool CanExecuteCancelShipment()
        {
            return Shipments.Any(x => x.IsSelected && x.Status == "확정");
        }

        private async Task ExecuteConfirmShipment()
        {
            try
            {
                var selectedShipments = Shipments.Where(x => x.IsSelected && x.Status == "대기").ToList();
                if (!selectedShipments.Any())
                {
                    MessageBox.Show("확정할 출하 건을 선택해주세요.", "알림");
                    return;
                }

                if (MessageBox.Show("선택한 출하 건을 확정하시겠습니까?", "출하 확정",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                {
                    return;
                }

                using (var conn = new MySqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            foreach (var shipment in selectedShipments)
                            {
                                // 재고 확인
                                var stockSql = @"
                            SELECT COALESCE(SUM(CASE 
                                WHEN transaction_type = '입고' THEN inventory_quantity
                                WHEN transaction_type = '출고' THEN -inventory_quantity
                                ELSE 0
                            END), 0) as current_stock
                            FROM dt_inventory_management
                            WHERE product_code = @ProductCode";

                                var currentStock = await conn.QuerySingleAsync<int>(stockSql,
                                    new { ProductCode = shipment.ProductCode }, transaction);

                                if (currentStock < shipment.ShipmentQuantity)
                                {
                                    throw new Exception($"재고가 부족합니다. 제품: {shipment.ProductName}, " +
                                        $"현재고: {currentStock}, 출하수량: {shipment.ShipmentQuantity}");
                                }

                                // 상태 업데이트
                                var updateSql = @"
                            UPDATE dt_shipment 
                            SET status = '확정'
                            WHERE shipment_number = @ShipmentNumber";

                                await conn.ExecuteAsync(updateSql,
                                    new { ShipmentNumber = shipment.ShipmentNumber }, transaction);

                                // 재고 차감
                                var inventorySql = @"
                            INSERT INTO dt_inventory_management 
                            (product_code, inventory_quantity, unit,
                             responsible_person, transaction_date, transaction_type, remarks)
                            VALUES 
                            (@ProductCode, @Quantity, @Unit,
                             @ResponsiblePerson, @TransactionDate, '출고', @Remarks)";

                                await conn.ExecuteAsync(inventorySql, new
                                {
                                    ProductCode = shipment.ProductCode,
                                    Quantity = shipment.ShipmentQuantity,
                                    Unit = "EA",
                                    ResponsiblePerson = App.CurrentUser.Username,
                                    TransactionDate = DateTime.Now,
                                    Remarks = $"출하번호: {shipment.ShipmentNumber} 확정"
                                }, transaction);

                                await _logService.SaveLogAsync(
                                    App.CurrentUser.UserId,
                                    "출하확정",
                                    $"출하번호: {shipment.ShipmentNumber}, 제품: {shipment.ProductName}, " +
                                    $"수량: {shipment.ShipmentQuantity}");
                            }

                            transaction.Commit();
                            MessageBox.Show("선택한 출하 건이 확정되었습니다.", "알림");
                            await ExecuteSearch();
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
                MessageBox.Show($"출하 확정 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteCancelShipment()
        {
            try
            {
                var selectedShipments = Shipments.Where(x => x.IsSelected && x.Status == "확정").ToList();
                if (!selectedShipments.Any())
                {
                    MessageBox.Show("취소할 출하 건을 선택해주세요.", "알림");
                    return;
                }

                if (MessageBox.Show("선택한 출하 건을 취소하시겠습니까?", "출하 취소",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                {
                    return;
                }

                using (var conn = new MySqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            foreach (var shipment in selectedShipments)
                            {
                                // 상태 업데이트
                                var updateSql = @"
                            UPDATE dt_shipment 
                            SET status = '취소'
                            WHERE shipment_number = @ShipmentNumber";

                                await conn.ExecuteAsync(updateSql,
                                    new { ShipmentNumber = shipment.ShipmentNumber }, transaction);

                                // 재고 복원
                                var inventorySql = @"
                            INSERT INTO dt_inventory_management 
                            (product_code, inventory_quantity, unit,
                             responsible_person, transaction_date, transaction_type, remarks)
                            VALUES 
                            (@ProductCode, @Quantity, @Unit,
                             @ResponsiblePerson, @TransactionDate, '입고', @Remarks)";

                                await conn.ExecuteAsync(inventorySql, new
                                {
                                    ProductCode = shipment.ProductCode,
                                    Quantity = shipment.ShipmentQuantity,
                                    Unit = "EA",
                                    ResponsiblePerson = App.CurrentUser.Username,
                                    TransactionDate = DateTime.Now,
                                    Remarks = $"출하번호: {shipment.ShipmentNumber} 취소"
                                }, transaction);

                                await _logService.SaveLogAsync(
                                    App.CurrentUser.UserId,
                                    "출하취소",
                                    $"출하번호: {shipment.ShipmentNumber}, 제품: {shipment.ProductName}, " +
                                    $"수량: {shipment.ShipmentQuantity}");
                            }

                            transaction.Commit();
                            MessageBox.Show("선택한 출하 건이 취소되었습니다.", "알림");
                            await ExecuteSearch();
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
                MessageBox.Show($"출하 취소 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        // 컬렉션
        public ObservableCollection<ShipmentModel> Shipments { get; }
        public ObservableCollection<string> Companies { get; }
        public ObservableCollection<string> Products { get; }

        // 속성
        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                if (_startDate != value)
                {
                    _startDate = value;
                    OnPropertyChanged();
                    //날짜 강제선택
                    if (_startDate > EndDate)
                    {
                        EndDate = _startDate;
                    }
                }
            }
        }

        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                if (_endDate != value)
                {
                    _endDate = value;
                    OnPropertyChanged();
                    //날짜 강제선택
                    if (_endDate < StartDate)
                    {
                        StartDate = _endDate;
                    }
                }
            }
        }

        public string SelectedCompany
        {
            get => _selectedCompany;
            set
            {
                if (_selectedCompany != value)
                {
                    _selectedCompany = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                if (_selectedProduct != value)
                {
                    _selectedProduct = value;
                    OnPropertyChanged();
                }
            }
        }

        public ShipmentModel SelectedShipment
        {
            get => _selectedShipment;
            set
            {
                if (_selectedShipment != value)
                {
                    _selectedShipment = value;
                    OnPropertyChanged();
                }
            }
        }

        // 명령
        public ICommand SearchCommand { get; }
        public ICommand AddCommand { get; }


        private async void LoadInitialData()
        {
            try
            {
                await LoadCompanies();
                await LoadProducts();
                await ExecuteSearch();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"데이터 로드 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadCompanies()
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                var sql = "SELECT DISTINCT company_name FROM dt_shipment ORDER BY company_name";
                var companies = await conn.QueryAsync<string>(sql);

                Companies.Clear();
                Companies.Add("전체");
                foreach (var company in companies)
                {
                    Companies.Add(company);
                }
            }
        }

        private async Task LoadProducts()
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                var sql = @"
                            SELECT DISTINCT p.product_name 
                            FROM dt_shipment s
                            JOIN dt_product p ON s.product_code = p.product_code 
                            ORDER BY p.product_name";
                var products = await conn.QueryAsync<string>(sql);

                Products.Clear();
                Products.Add("전체");
                foreach (var product in products)
                {
                    Products.Add(product);
                }
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
                        s.shipment_number as ShipmentNumber,
                        s.company_code as CompanyCode,
                        s.company_name as CompanyName,
                        s.product_code as ProductCode,
                        p.product_name as ProductName,
                        s.production_date as ProductionDate,
                        s.shipment_date as ShipmentDate,
                        s.shipment_quantity as ShipmentQuantity,
                        s.vehicle_number AS VehicleNumber,
                        s.employee_name as EmployeeName,
                        s.status as Status
                    FROM dt_shipment s
                    JOIN dt_product p ON s.product_code = p.product_code
                    WHERE 1=1";

                    var parameters = new DynamicParameters();

                    // 전체 기간 보기가 체크되지 않은 경우, 선택한 날짜 범위만 표시
                    if (!IsAllPeriodChecked)
                    {
                        sql += " AND s.shipment_date BETWEEN @StartDate AND @EndDate";
                        parameters.Add("@StartDate", StartDate.Date);
                        parameters.Add("@EndDate", EndDate.Date);
                    }

                    // 거래처 필터
                    if (!string.IsNullOrEmpty(SelectedCompany) && SelectedCompany != "전체")
                    {
                        sql += " AND s.company_name = @CompanyName";
                        parameters.Add("@CompanyName", SelectedCompany);
                    }

                    // 제품 필터
                    if (!string.IsNullOrEmpty(SelectedProduct) && SelectedProduct != "전체")
                    {
                        sql += " AND p.product_name = @ProductName";
                        parameters.Add("@ProductName", SelectedProduct);
                    }

                    // 확정 항목 필터
                    if (IsConfirmedOnlyChecked)
                    {
                        sql += " AND s.status = '확정'";
                    }

                    sql += " ORDER BY s.shipment_date DESC, s.shipment_number";

                    var result = await conn.QueryAsync<ShipmentModel>(sql, parameters);

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Shipments.Clear();
                        foreach (var shipment in result)
                        {
                            Shipments.Add(shipment);
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

        private void ExecuteAdd()
        {
            ShowAddWindow(false);
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

                _currentAddWindow = new ShipmentAddWindow(isEdit)
                {
                    Owner = Application.Current.MainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                _currentAddWindow.Closed += async (s, e) =>
                {
                    if (_currentAddWindow.DialogResult == true)
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


        private bool CanExecuteDelete()
        {
            return Shipments.Any(x => x.IsSelected);
        }

        private async Task ExecuteDelete()
        {
            if (MessageBox.Show("선택한 항목들을 삭제하시겠습니까?", "삭제 확인",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
            {
                return;
            }

            try
            {
                var selectedItems = Shipments.Where(x => x.IsSelected).ToList();
                if (!selectedItems.Any())
                {
                    MessageBox.Show("삭제할 항목이 선택되지 않았습니다.", "알림");
                    return;
                }

                using (var conn = new MySqlConnection(_connectionString))
                {
                    foreach (var item in selectedItems)
                    {
                        var sql = "DELETE FROM dt_shipment WHERE shipment_number = @ShipmentNumber";
                        await conn.ExecuteAsync(sql, new { ShipmentNumber = item.ShipmentNumber });

                        // 로그 저장
                        string actionDetail = $"출하번호: {item.ShipmentNumber}, " +
                                           $"출하일자: {item.ShipmentDate:yyyy-MM-dd}, " +
                                           $"거래처: {item.CompanyName}, " +
                                           $"제품: {item.ProductName}, " +
                                           $"수량: {item.ShipmentQuantity}"+
                                           $"차량번호: {item.VehicleNumber}";

                        await _logService.SaveLogAsync(App.CurrentUser.UserId, "출하내역 삭제", actionDetail);

                    }
                }

                MessageBox.Show("선택한 항목이 삭제되었습니다.", "알림");
                await ExecuteSearch();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"삭제 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadDataForEdit(ShipmentModel selectedShipment)
        {
            if (selectedShipment == null)
                return;

            try
            {
                if (_currentAddWindow != null && _currentAddWindow.IsLoaded)
                {
                    _currentAddWindow.Activate();
                    return;
                }

                _currentAddWindow = new ShipmentAddWindow(true)
                {
                    Owner = Application.Current.MainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Title = "출하 수정"
                };

                var viewModel = _currentAddWindow.DataContext as ShipmentAddViewModel;
                if (viewModel != null)
                {
                    viewModel.LoadData(selectedShipment);
                }

                _currentAddWindow.Closed += async (s, e) =>
                {
                    if (_currentAddWindow.DialogResult == true)
                    {
                        await ExecuteSearch();
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

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Cleanup()
        {
            // 리소스 정리 필요시 여기에 구현
        }
    }

    public class ShipmentModel
    {
        public string ShipmentNumber { get; set; }
        public string CompanyCode { get; set; }
        public string CompanyName { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public DateTime ProductionDate { get; set; }
        public DateTime ShipmentDate { get; set; }
        public int ShipmentQuantity { get; set; }
        public string VehicleNumber { get; set; }
        public string EmployeeName { get; set; }
        public bool IsSelected { get; set; }
        public string Status { get; set; } = "대기";
    }
}
