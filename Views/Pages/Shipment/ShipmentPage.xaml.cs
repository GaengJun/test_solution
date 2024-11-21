using MES.Solution.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace MES.Solution.Views.Pages.Shipment
{
    /// <summary>
    /// ShipmentPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ShipmentPage : Page
    {
        private ShipmentViewModel _viewModel;
        public ShipmentPage()
        {
            InitializeComponent();
            _viewModel = new ShipmentViewModel();
            DataContext = _viewModel;
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            if (dataGrid != null && dataGrid.SelectedItem != null)
            {
                var selectedShipment = dataGrid.SelectedItem as ShipmentModel;
                if (selectedShipment != null)
                {
                    var viewModel = DataContext as ShipmentViewModel;
                    if (viewModel != null)
                    {
                        viewModel.LoadDataForEdit(selectedShipment);
                    }
                }
            }
        }

        public void OnNavigatedFrom()
        {
            _viewModel?.Cleanup();
        }
    }
}
