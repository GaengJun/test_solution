using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MES.Solution.ViewModels;

namespace MES.Solution.Views
{
    public partial class RegisterWindow : Window
    {
        private readonly TextBlock _passwordPlaceholder;
        private readonly TextBlock _confirmPasswordPlaceholder;
        private readonly RegisterViewModel _viewModel;

        public RegisterWindow()
        {
            InitializeComponent();
            _viewModel = new RegisterViewModel();
            DataContext = _viewModel;
            _passwordPlaceholder = FindName("PasswordPlaceholder") as TextBlock;
            _confirmPasswordPlaceholder = FindName("ConfirmPasswordPlaceholder") as TextBlock;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_passwordPlaceholder != null)
            {
                _passwordPlaceholder.Visibility = string.IsNullOrEmpty(PasswordBox.Password) ?
                    Visibility.Visible : Visibility.Hidden;
            }
            if (_viewModel != null)
            {
                _viewModel.Password = PasswordBox.Password;
            }
        }

        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_confirmPasswordPlaceholder != null)
            {
                _confirmPasswordPlaceholder.Visibility = string.IsNullOrEmpty(ConfirmPasswordBox.Password) ?
                    Visibility.Visible : Visibility.Hidden;
            }
            if (_viewModel != null)
            {
                _viewModel.ConfirmPassword = ConfirmPasswordBox.Password;
            }
        }

        private void PasswordBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (_passwordPlaceholder != null)
            {
                _passwordPlaceholder.Visibility = Visibility.Hidden;
            }
        }

        private void PasswordBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_passwordPlaceholder != null && string.IsNullOrEmpty(PasswordBox.Password))
            {
                _passwordPlaceholder.Visibility = Visibility.Visible;
            }
        }

        private void ConfirmPasswordBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (_confirmPasswordPlaceholder != null)
            {
                _confirmPasswordPlaceholder.Visibility = Visibility.Hidden;
            }
        }

        private void ConfirmPasswordBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_confirmPasswordPlaceholder != null && string.IsNullOrEmpty(ConfirmPasswordBox.Password))
            {
                _confirmPasswordPlaceholder.Visibility = Visibility.Visible;
            }
        }

        // 창 드래그 이동
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        // 뒤로가기
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // 창 닫기
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // 창 최소화
        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
    }
}