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
    public partial class ShipmentAddWindow : Window
    {
        private readonly ShipmentAddViewModel _viewModel;

        public ShipmentAddWindow(bool isEdit = false)
        {
            InitializeComponent();
            _viewModel = new ShipmentAddViewModel(isEdit);
            _viewModel.RequestClose += (s, e) =>
            {
                this.DialogResult = true;
                this.Close();
            };
            DataContext = _viewModel;

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
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

    }

}