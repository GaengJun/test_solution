// ViewModels/ProductionStatusViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MES.Solution.Helpers;
using MES.Solution.Models;
using System.Windows;
using System.Diagnostics;
using MySql.Data.MySqlClient;
using Dapper;
using System.Configuration;
using System.Threading.Tasks;
using System.Linq;

namespace MES.Solution.ViewModels
{
    public class ProductionStatusViewModel : INotifyPropertyChanged
    {
        private readonly string _connectionString;
        private DateTime _productionDate = DateTime.Today;
        private string _selectedShift;
        private string _selectedLine;
        private ProductionStatusItem _selectedItem;
        private bool _includeCompleted;
        private readonly ObservableCollection<ProductionStatusItem> _items;
        private bool _isLoading;

        public ProductionStatusViewModel()
        {
            try
            {
                _connectionString = ConfigurationManager.ConnectionStrings["MESDatabase"].ConnectionString;

                // 컬렉션 초기화
                _items = new ObservableCollection<ProductionStatusItem>();
                Shifts = new ObservableCollection<string> { "전체", "주간1", "주간2", "야간1", "야간2" };
                ProductionLines = new ObservableCollection<string> { "전체", "라인1", "라인2", "라인3" };

                // 명령 초기화
                SearchCommand = new RelayCommand(async () => await LoadDataAsync(), () => !IsLoading);
                StartWorkCommand = new RelayCommand(ExecuteStartWork, CanExecuteStartWork);
                CompleteWorkCommand = new RelayCommand(ExecuteCompleteWork, CanExecuteCompleteWork);
                CancelWorkCommand = new RelayCommand(ExecuteCancelWork, CanExecuteCancelWork);
                RefreshCommand = new RelayCommand(async () => await LoadDataAsync(), () => !IsLoading);

                // 초기 데이터 로드
                _ = LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"초기화 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"ViewModel 초기화 오류: {ex}");
            }
        }

        #region Properties

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                    UpdateCommandStates();
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
                    OnPropertyChanged();
                    _ = LoadDataAsync();
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
                    _ = LoadDataAsync();
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
                    _ = LoadDataAsync();
                }
            }
        }

        public ProductionStatusItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    OnPropertyChanged();
                    UpdateCommandStates();
                }
            }
        }

        public bool IncludeCompleted
        {
            get => _includeCompleted;
            set
            {
                if (_includeCompleted != value)
                {
                    _includeCompleted = value;
                    OnPropertyChanged();
                    _ = LoadDataAsync();
                }
            }
        }

        public ObservableCollection<ProductionStatusItem> Items => _items;
        public ObservableCollection<string> Shifts { get; }
        public ObservableCollection<string> ProductionLines { get; }

        #endregion

        #region Commands

        public ICommand SearchCommand { get; }
        public ICommand StartWorkCommand { get; }
        public ICommand CompleteWorkCommand { get; }
        public ICommand CancelWorkCommand { get; }
        public ICommand RefreshCommand { get; }

        #endregion

        #region Command Methods

        private bool CanExecuteStartWork()
        {
            return SelectedItem != null &&
                   SelectedItem.Status == "대기" &&
                   !IsLoading;
        }

        private async void ExecuteStartWork()
        {
            try
            {
                if (SelectedItem == null) return;

                var result = MessageBox.Show(
                    "작업을 시작하시겠습니까?",
                    "작업 시작",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;

                IsLoading = true;

                using (var conn = new MySqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // 작업 시작 업데이트
                            string sql = @"
                               UPDATE dt_production_plan 
                               SET status = '작업중',
                                   start_time = @StartTime,
                                   employee_name = @EmployeeName
                               WHERE work_order_code = @WorkOrderCode
                               AND status = '대기'";

                            await conn.ExecuteAsync(sql, new
                            {
                                StartTime = DateTime.Now,
                                EmployeeName = App.CurrentUser.Username,
                                WorkOrderCode = SelectedItem.WorkOrderCode
                            }, transaction);

                            // 생산관리 테이블도 업데이트
                            sql = @"
                               UPDATE dt_production_management
                               SET status = '작업중',
                                   employee_name = @EmployeeName
                               WHERE work_order_code = @WorkOrderCode";

                            await conn.ExecuteAsync(sql, new
                            {
                                EmployeeName = App.CurrentUser.Username,
                                WorkOrderCode = SelectedItem.WorkOrderCode
                            }, transaction);

                            transaction.Commit();
                            await LoadDataAsync();

                            MessageBox.Show("작업이 시작되었습니다.", "알림",
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
                MessageBox.Show($"작업 시작 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"작업 시작 오류: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanExecuteCompleteWork()
        {
            return SelectedItem != null &&
                   SelectedItem.Status == "작업중" &&
                   !IsLoading;
        }

        private async void ExecuteCompleteWork()
        {
            try
            {
                if (SelectedItem == null) return;

                var result = MessageBox.Show(
                    $"작업을 완료하시겠습니까?\n지시수량: {SelectedItem.OrderQuantity}",
                    "작업 완료",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;

                IsLoading = true;

                using (var conn = new MySqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // 생산계획 테이블 업데이트
                            string sql = @"
                               UPDATE dt_production_plan 
                               SET status = '완료',
                                   completion_time = @CompletionTime,
                                   production_quantity = @ProductionQuantity
                               WHERE work_order_code = @WorkOrderCode
                               AND status = '작업중'";

                            await conn.ExecuteAsync(sql, new
                            {
                                CompletionTime = DateTime.Now,
                                ProductionQuantity = SelectedItem.OrderQuantity,  // 임시로 지시수량으로 설정
                                WorkOrderCode = SelectedItem.WorkOrderCode
                            }, transaction);

                            // 생산관리 테이블 업데이트
                            sql = @"
                               UPDATE dt_production_management
                               SET status = '완료',
                                   production_quantity = @ProductionQuantity
                               WHERE work_order_code = @WorkOrderCode";

                            await conn.ExecuteAsync(sql, new
                            {
                                ProductionQuantity = SelectedItem.OrderQuantity,
                                WorkOrderCode = SelectedItem.WorkOrderCode
                            }, transaction);

                            // 재고 테이블 업데이트
                            sql = @"
                               INSERT INTO dt_inventory 
                                   (inventory_id, product_code, product_name, quantity, unit, employee_name)
                               VALUES 
                                   (@InventoryId, @ProductCode, @ProductName, @Quantity, @Unit, @EmployeeName)
                               ON DUPLICATE KEY UPDATE
                                   quantity = quantity + @Quantity,
                                   employee_name = @EmployeeName,
                                   last_updated = CURRENT_TIMESTAMP";

                            await conn.ExecuteAsync(sql, new
                            {
                                InventoryId = $"INV-{DateTime.Now:yyyyMMdd}-{SelectedItem.WorkOrderCode}",
                                SelectedItem.ProductCode,
                                SelectedItem.ProductName,
                                Quantity = SelectedItem.OrderQuantity,
                                SelectedItem.Unit,
                                EmployeeName = App.CurrentUser.Username
                            }, transaction);

                            transaction.Commit();
                            await LoadDataAsync();

                            MessageBox.Show("작업이 완료되었습니다.", "알림",
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
                MessageBox.Show($"작업 완료 처리 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"작업 완료 오류: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanExecuteCancelWork()
        {
            return SelectedItem != null &&
                   (SelectedItem.Status == "작업중" || SelectedItem.Status == "완료") &&
                   !IsLoading;
        }

        private async void ExecuteCancelWork()
        {
            try
            {
                if (SelectedItem == null) return;

                var result = MessageBox.Show(
                    "작업을 취소하시겠습니까?\n이미 등록된 생산수량은 삭제됩니다.",
                    "작업 취소",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes) return;

                IsLoading = true;

                using (var conn = new MySqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // 생산계획 테이블 업데이트
                            string sql = @"
                               UPDATE dt_production_plan 
                               SET status = '대기',
                                   production_quantity = 0,
                                   start_time = NULL,
                                   completion_time = NULL
                               WHERE work_order_code = @WorkOrderCode";

                            await conn.ExecuteAsync(sql, new
                            {
                                SelectedItem.WorkOrderCode
                            }, transaction);

                            // 생산관리 테이블 업데이트
                            sql = @"
                               UPDATE dt_production_management
                               SET status = '대기',
                                   production_quantity = 0
                               WHERE work_order_code = @WorkOrderCode";

                            await conn.ExecuteAsync(sql, new
                            {
                                SelectedItem.WorkOrderCode
                            }, transaction);

                            transaction.Commit();
                            await LoadDataAsync();

                            MessageBox.Show("작업이 취소되었습니다.", "알림",
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
                MessageBox.Show($"작업 취소 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"작업 취소 오류: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Helper Methods

        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;

                using (var conn = new MySqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var sql = @"
                       SELECT 
                           pp.work_order_code AS WorkOrderCode,
                           pp.production_date AS ProductionDate,
                           pp.production_line AS ProductionLine,
                           pp.work_order_sequence AS WorkOrderSequence,
                           pp.product_code AS ProductCode,
                           pp.product_name AS ProductName,
                           pp.unit AS Unit,
                           pp.order_quantity AS OrderQuantity,
                           pp.production_quantity AS ProductionQuantity,
                           pp.status AS Status,
                           pp.equipment_status AS EquipmentStatus,
                           pp.employee_name AS EmployeeName,
                           pp.start_time AS StartTime,
                           pp.completion_time AS CompletionTime
                       FROM dt_production_plan pp
                       WHERE pp.production_date = @ProductionDate";

                    var parameters = new DynamicParameters();
                    parameters.Add("@ProductionDate", ProductionDate);

                    if (!string.IsNullOrEmpty(SelectedShift) && SelectedShift != "전체")
                    {
                        sql += " AND pp.work_shift = @WorkShift";
                        parameters.Add("@WorkShift", SelectedShift);
                    }

                    if (!string.IsNullOrEmpty(SelectedLine) && SelectedLine != "전체")
                    {
                        sql += " AND pp.production_line = @ProductionLine";
                        parameters.Add("@ProductionLine", SelectedLine);
                    }

                    if (!IncludeCompleted)
                    {
                        sql += " AND pp.status != '완료'";
                    }

                    sql += @" 
                       ORDER BY 
                           CASE pp.status
                               WHEN '작업중' THEN 1
                               WHEN '대기' THEN 2
                               WHEN '지연' THEN 3
                               WHEN '완료' THEN 4
                           END,
                           pp.work_order_sequence";

                    var result = await conn.QueryAsync<ProductionStatusItem>(sql, parameters);

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Items.Clear();
                        foreach (var item in result)
                        {
                            Items.Add(item);
                        }
                    });

                    // 지연 상태 체크
                    await CheckDelayedItemsAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"데이터 로드 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"데이터 로드 오류: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task CheckDelayedItemsAsync()
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var sql = @"
                       UPDATE dt_production_plan
                       SET status = '지연'
                       WHERE production_date < CURRENT_DATE
                       AND status = '대기'";

                    await conn.ExecuteAsync(sql);

                    sql = @"
                       UPDATE dt_production_plan
                       SET status = '지연'
                       WHERE status = '작업중'
                       AND TIMESTAMPDIFF(HOUR, start_time, NOW()) > 8";

                    await conn.ExecuteAsync(sql);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"지연 상태 체크 오류: {ex}");
            }
        }

        private void UpdateCommandStates()
        {
            (StartWorkCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (CompleteWorkCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (CancelWorkCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (SearchCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (RefreshCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        public void Cleanup()
        {
            try
            {
                _items.Clear();
                SelectedItem = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Cleanup 중 오류 발생: {ex.Message}");
            }
        }
    }
}