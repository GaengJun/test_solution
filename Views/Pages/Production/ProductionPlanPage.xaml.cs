using System.Windows.Controls;
using MES.Solution.Models;
using System.Windows.Input;
using MES.Solution.ViewModels;

namespace MES.Solution.Views.Pages
{
    public partial class ProductionPlanPage : Page
    {
        private readonly ProductionPlanViewModel _viewModel;

        public ProductionPlanPage()
        {
            InitializeComponent();
            _viewModel = new ProductionPlanViewModel();
            DataContext = _viewModel;
        }

        public void OnNavigatedFrom()
        {
            _viewModel?.Cleanup();
        }
        /* private void DataGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            if (dataGrid != null && dataGrid.SelectedItem != null)
            {
                var selectedPlan = dataGrid.SelectedItem as ProductionPlanModel;
                if (selectedPlan != null)
                {
                    var viewModel = DataContext as ProductionPlanViewModel;
                    viewModel?.LoadDataForEdit(selectedPlan);
                    dataGrid.UnselectAll();  // 선택 해제
                }
            }
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            if (dataGrid != null && dataGrid.SelectedItem != null)
            {
                var selectedPlan = dataGrid.SelectedItem as ProductionPlanModel;
                if (selectedPlan != null)
                {
                    var viewModel = DataContext as ProductionPlanViewModel;
                    if (viewModel != null)
                    {
                        viewModel.ExecuteAdd();  // 창 열기
                        viewModel.LoadDataForEdit(selectedPlan);  // 데이터 로드
                    }
                }
            }
        }*/

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            if (dataGrid != null && dataGrid.SelectedItem != null)
            {
                var selectedPlan = dataGrid.SelectedItem as ProductionPlanModel;
                if (selectedPlan != null)
                {
                    var viewModel = DataContext as ProductionPlanViewModel;
                    if (viewModel != null)
                    {
                        viewModel.LoadDataForEdit(selectedPlan);  // 수정 데이터 로드
                    }
                }
            }
        }
    }
}
