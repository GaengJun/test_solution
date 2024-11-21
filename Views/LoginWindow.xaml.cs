using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MES.Solution.Helpers;
using MES.Solution.ViewModels;

namespace MES.Solution.Views
{
    public partial class LoginWindow : Window
    {
        private LoginViewModel _viewModel;
        public LoginWindow()
        {
            InitializeComponent();
            _viewModel = new LoginViewModel();
            DataContext = _viewModel;

            // PasswordBox 이벤트 핸들러 등록
            if (PasswordBox != null)
            {
                PasswordBox.PasswordChanged += PasswordBox_PasswordChanged;
            }

            // 창이 로드된 후 패스워드 박스 초기화
            this.Loaded += LoginWindow_Loaded;
        }
        private void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // UI 요소들이 모두 로드된 후 초기화
            if (PasswordBox != null)
            {
                PasswordBox.Clear();
                if (UsernameBox != null)
                {
                    UsernameBox.Focus();
                }
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null && sender is PasswordBox passwordBox)
            {
                _viewModel.Password = passwordBox.Password;
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

        protected override void OnClosed(EventArgs e)
        {
            // 이벤트 핸들러 제거
            if (PasswordBox != null)
            {
                PasswordBox.PasswordChanged -= PasswordBox_PasswordChanged;
            }
            base.OnClosed(e);
        }

        // 창 닫기
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // 창 최소화
        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                _viewModel.LoginCommand.Execute(null);
            }
        }
    }
}