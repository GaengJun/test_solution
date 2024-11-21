using MES.Solution.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    /// InventoryManagementWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class InventoryManagementWindow : Window
    {
        private readonly InventoryManagementViewModel _viewModel;
        public InventoryManagementWindow(InventoryModel inventory = null)
        {
            InitializeComponent();
            _viewModel = new InventoryManagementViewModel();
            _viewModel.RequestClose += (s, e) =>
            {
                this.DialogResult = true;
                this.Close();
            };
            DataContext = _viewModel;

        }
        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {

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
    }
}
