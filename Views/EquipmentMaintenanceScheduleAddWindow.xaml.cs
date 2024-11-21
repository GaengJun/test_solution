using MES.Solution.Helpers;
using MES.Solution.ViewModels.Equipment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MES.Solution.Views
{
    /// <summary>
    /// EquipmentMaintenanceScheduleAddWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class EquipmentMaintenanceScheduleAddWindow : Window
    {
        private readonly EquipmentMaintenanceScheduleAddViewModel _viewModel;
        public EquipmentMaintenanceScheduleAddWindow(bool isEdit = false)
        {
            InitializeComponent();
            _viewModel = new EquipmentMaintenanceScheduleAddViewModel(isEdit);
            _viewModel.RequestClose += (s, e) =>
            {
                this.DialogResult = true;
                this.Close();
            };
            DataContext = _viewModel;

            // ESC 키로 창 닫기 방지
            this.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    e.Handled = true;
                }
            };
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            WindowHelper.RemoveMinimizeMaximizeButtons(this);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"^-?\d*\.?\d*$");
            e.Handled = !regex.IsMatch(e.Text);
        }

        public void LoadData(Models.MaintenanceScheduleModel model)
        {
            _viewModel.LoadData(model);
        }
    }
}
