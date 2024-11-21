using System.Diagnostics;
using System.Windows.Controls;
using LiveCharts.Wpf;
using System.Windows.Media;
using MES.Solution.ViewModels;

namespace MES.Solution.Views.Pages
{
    public partial class InventoryPage : Page
    {
        private readonly InventoryViewModel _viewModel;

        public InventoryPage()
        {
            InitializeComponent();
            _viewModel = new InventoryViewModel();
            DataContext = _viewModel;
        }

        private void DataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            if (dataGrid?.SelectedItem is InventoryModel selectedInventory)
            {
                _viewModel.LoadDataForEdit(selectedInventory);
            }
        }
    }
}