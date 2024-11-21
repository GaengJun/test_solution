using MES.Solution.Models;
using MES.Solution.ViewModels;
using MES.Solution.ViewModels.Equipment;
using System.Windows;

namespace MES.Solution.Views
{
    /// <summary>
    /// PlcDetailWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PlcDetailWindow : Window
    {
        private readonly PlcViewModel _plcViewModel;
        public PlcDetailWindow(PlcStatusModel plc, PlcViewModel plcViewModel)
        {
            InitializeComponent();
            DataContext = plc;
            _plcViewModel = plcViewModel;
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext is EquipmentViewModel viewModel)
            {
                _plcViewModel?.PLCCleanup();
            }
        }
    }
}
