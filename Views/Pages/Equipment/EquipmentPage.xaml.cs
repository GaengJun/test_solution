using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MES.Solution.Models;
using MES.Solution.Models.Equipment;
using MES.Solution.ViewModels;

namespace MES.Solution.Views.Pages
{
    public partial class EquipmentPage : Page
    {
        private readonly EquipmentViewModel _viewModel;

        public EquipmentPage()
        {
            InitializeComponent();
            _viewModel = new EquipmentViewModel();
            DataContext = _viewModel;
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is PlcStatusModel plc)
            {
                plc.ShowDetailsCommand.Execute(null);
            }
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            if (dataGrid?.SelectedItem != null)
            {
                var selectedSchedule = dataGrid.SelectedItem as MaintenanceScheduleModel;
                if (selectedSchedule != null)
                {
                    var window = new EquipmentMaintenanceScheduleAddWindow(true)
                    {
                        Owner = Application.Current.MainWindow,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Title = "장비점검 일정 수정"
                    };

                    window.LoadData(selectedSchedule);

                    if (window.ShowDialog() == true)
                    {
                        _viewModel.RefreshCommand.Execute(null);
                    }
                }
            }
        }
    }
}