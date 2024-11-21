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

namespace MES.Solution.ViewModels
{
    public class ContractViewModel : INotifyPropertyChanged
    {
        private readonly string _connectionString;
        private DateTime _startDate = DateTime.Now.AddDays(1 - DateTime.Now.Day); // 이번달 1일
        private DateTime _endDate = DateTime.Today;
        private string _selectedCompany = "전체";
        private string _selectedProduct = "전체";
        private string _selectedPlan = "전체";
        private ContractModel _selectedContract;

        public ContractViewModel()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["MESDatabase"].ConnectionString;

            // 컬렉션 초기화
            Contracts = new ObservableCollection<ContractModel>();
            Companies = new ObservableCollection<string>();
            Products = new ObservableCollection<string>();
            SelectedProductionPlan = new ObservableCollection<string>() { "전체","필요","불필요"};

            // 명령 초기화
            SearchCommand = new RelayCommand(async () => await ExecuteSearch());
            AddCommand = new RelayCommand(ExecuteAdd);
            ExportCommand = new RelayCommand(ExecuteExport);
            //ViewDetailCommand = new RelayCommand<ContractModel>(ExecuteViewDetail);

            // 초기 데이터 로드
            LoadInitialData();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        // 컬렉션
        public ObservableCollection<ContractModel> Contracts { get; }
        public ObservableCollection<string> Companies { get; }
        public ObservableCollection<string> Products { get; }
        public ObservableCollection<string> SelectedProductionPlan { get; }

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

        public string SelectedPlan
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

        public ContractModel SelectedContract
        {
            get => _selectedContract;
            set
            {
                if (_selectedContract != value)
                {
                    _selectedContract = value;
                    OnPropertyChanged();
                }
            }
        }

        // 명령
        public ICommand SearchCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand ExportCommand { get; }
        //public ICommand ViewDetailCommand { get; }

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
                var sql = "SELECT DISTINCT company_name FROM dt_contract ORDER BY company_name";
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
FROM dt_contract c
JOIN dt_product p ON c.product_code = p.product_code 
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
    c.order_number as OrderNumber,
    c.order_date as OrderDate,
    c.company_code as CompanyCode,
    c.company_name as CompanyName,
    c.product_code as ProductCode,
    p.product_name as ProductName,
    c.quantity as Quantity,
    c.delivery_date as DeliveryDate,
    c.production_plan as ProductionPlan,
    c.remarks as Remarks,
    c.employee_name as EmployeeName,
    p.unit as Unit
FROM dt_contract c
JOIN dt_product p ON c.product_code = p.product_code
WHERE c.order_date BETWEEN @StartDate AND @EndDate";

                    var parameters = new DynamicParameters();
                    parameters.Add("@StartDate", StartDate.Date);
                    parameters.Add("@EndDate", EndDate.Date);

                    if (SelectedCompany != "전체")
                    {
                        sql += " AND c.company_name = @CompanyName";
                        parameters.Add("@CompanyName", SelectedCompany);
                    }

                    if (SelectedProduct != "전체")
                    {
                        sql += " AND p.product_name = @ProductName";
                        parameters.Add("@ProductName", SelectedProduct);
                    }

                    if (SelectedPlan != "전체")
                    {
                        sql += " AND c.production_plan = @PlanStatus";
                        parameters.Add("@PlanStatus", SelectedPlan);
                    }

                    sql += " ORDER BY c.order_date DESC, c.order_number";

                    var result = await conn.QueryAsync<ContractModel>(sql, parameters);

                    Contracts.Clear();
                    foreach (var contract in result)
                    {
                        Contracts.Add(contract);
                    }
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
            MessageBox.Show("수주 등록 기능은 추후 구현 예정입니다.");
        }

        private void ExecuteExport()
        {
            MessageBox.Show("엑셀 내보내기 기능은 추후 구현 예정입니다.");
        }

        //private void ExecuteViewDetail(ContractModel contract)
        //{
        //    if (contract != null)
        //    {
        //        MessageBox.Show($"주문번호 {contract.OrderNumber}의 상세 정보 창은 추후 구현 예정입니다.");
        //    }
        //}

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Cleanup()
        {
            // 리소스 정리 필요시 여기에 구현
        }
    }

    public class ContractModel
    {
        public string OrderNumber { get; set; }
        public DateTime OrderDate { get; set; }
        public string CompanyCode { get; set; }
        public string CompanyName { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public DateTime DeliveryDate { get; set; }
        public string ProductionPlan { get; set; }
        public string Remarks { get; set; }
        public string EmployeeName { get; set; }
        public string Unit { get; set; }
    }
}
