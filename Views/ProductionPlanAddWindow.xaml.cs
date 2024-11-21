using MES.Solution.Helpers;
using MES.Solution.ViewModels;
using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace MES.Solution.Views
{
    public partial class ProductionPlanAddWindow : Window
    {
        private readonly ProductionPlanAddViewModel _viewModel;
        public ProductionPlanAddWindow(bool isEdit = false)
        {
            InitializeComponent();
            _viewModel = new ProductionPlanAddViewModel(isEdit);
            _viewModel.RequestClose += (s, e) =>
            {
                //this.Loaded += OnWindowLoaded;


                this.DialogResult = true;
                this.Close();  // Loaded 이벤트에 연결하지 않고 직접 Close 호출
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

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
            this.Loaded -= OnWindowLoaded;
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
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }

}