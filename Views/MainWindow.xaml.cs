using System;
using System.Windows;
using MES.Solution.ViewModels;

namespace MES.Solution.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            // 로그인 체크
            if (!App.IsUserLoggedIn)
            {
                MessageBox.Show("로그인이 필요합니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                var loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
                return;
            }

            // ViewModel 초기화 및 DataContext 설정
            _viewModel = new MainViewModel(MainFrame);
            DataContext = _viewModel;

            // 창이 닫힐 때 정리 작업
            this.Closed += MainWindow_Closed;
        }

        private void MainWindow_Closed(object sender, System.EventArgs e)
        {
            _viewModel.Cleanup();
        }

        // Windows FormStyle 설정
        protected override void OnSourceInitialized(System.EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Windows 상태 복원
            if (Properties.Settings.Default.MainWindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Maximized;
            }
            else
            {
                Width = Properties.Settings.Default.MainWindowWidth;
                Height = Properties.Settings.Default.MainWindowWidth;
                WindowState = Properties.Settings.Default.MainWindowState;
            }
        }

        // 창 크기 및 상태 저장
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // 창 상태가 Normal일 때만 크기 저장
            if (WindowState == WindowState.Normal)
            {
                Properties.Settings.Default.MainWindowWidth = Width;
                Properties.Settings.Default.MainWindowWidth = Height;
            }
            Properties.Settings.Default.MainWindowState = WindowState;
            Properties.Settings.Default.Save();

            base.OnClosing(e);
        }

        public void LogOut()
        {
            try
            {
                App.LogOut(); // 애플리케이션 레벨의 로그아웃 처리

                // 새로운 로그인 창을 생성하고 표시
                var loginWindow = new LoginWindow();
                loginWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                // 현재 창 닫기
                loginWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"로그아웃 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ProductionRadio_Checked(object sender, RoutedEventArgs e)
        {
            ProductionSubMenu.Visibility = Visibility.Visible;
        }

        private void ProductionRadio_Unchecked(object sender, RoutedEventArgs e)
        {
            ProductionSubMenu.Visibility = Visibility.Collapsed;
        }

        private void MaterialManagementRadio_Checked(object sender, RoutedEventArgs e)
        {
            MaterialManagementSubMenu.Visibility = Visibility.Visible;
        }

        private void MaterialManagementRadio_Unchecked(object sender, RoutedEventArgs e)
        {
            MaterialManagementSubMenu.Visibility = Visibility.Collapsed;
        }

        private void InventoryRadio_Checked(object sender, RoutedEventArgs e)
        {
            InventorySubMenu.Visibility = Visibility.Visible;
        }

        private void InventoryRadio_Unchecked(object sender, RoutedEventArgs e)
        {
            InventorySubMenu.Visibility=Visibility.Collapsed;
        }

        private void ShipmentRadio_Checked(object sender, RoutedEventArgs e)
        {
            ShipmentSubMenu.Visibility = Visibility.Visible;
        }

        private void ShipmentRadio_Unchecked(object sender, RoutedEventArgs e)
        {
            ShipmentSubMenu.Visibility=Visibility.Collapsed;
        }
    }
}