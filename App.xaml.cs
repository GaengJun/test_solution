using System;
using System.Diagnostics;
using System.Windows;
using MES.Solution.Models;
using MES.Solution.Properties;  // Settings 클래스를 사용하기 위해 추가

namespace MES.Solution
{
    public partial class App : Application
    {
        private static CurrentUser _currentUser;
        public static CurrentUser CurrentUser
        {
            get => _currentUser ?? CurrentUser.Empty;
            set => _currentUser = value;
        }

        public App()
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"예기치 않은 오류가 발생했습니다.\n{e.Exception.Message}",
                          "오류",
                          MessageBoxButton.OK,
                          MessageBoxImage.Error);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                MessageBox.Show($"심각한 오류가 발생했습니다.\n{ex.Message}",
                              "심각한 오류",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        public static bool IsUserLoggedIn => CurrentUser != CurrentUser.Empty;

        public static void LogOut()
        {
            try
            {
                _currentUser = CurrentUser.Empty;
                Settings.Default.AutoLogin = false;
                Settings.Default.LastUsername = string.Empty;
                Settings.Default.Save();

                // 메모리 정리
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Logout error: {ex.Message}");
                throw;
            }
        }
    }
}