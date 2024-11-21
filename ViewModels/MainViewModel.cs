using System;
using System.ComponentModel;
using System.Windows.Threading;
using System.Windows.Input;
using System.Windows.Controls;
using MES.Solution.Helpers;
using MES.Solution.Views.Pages;
using System.Collections.ObjectModel;
using System.Windows;
using MES.Solution.Models;
using MES.Solution.Views;
using System.Runtime.CompilerServices;
using System.Windows.Markup.Localizer;
using MES.Solution.Views.Pages.Contract;
using MES.Solution.Views.Pages.Shipment;


namespace MES.Solution.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly DispatcherTimer _timer;
        private Page _currentPage;
        private readonly Frame _mainFrame;
        private int _notificationCount;
        private ObservableCollection<NotificationItem> _notifications;

        

        public MainViewModel(Frame mainFrame)
        {
            _mainFrame = mainFrame;
            CurrentUser = App.CurrentUser;

            // 타이머 초기화 (1초 간격으로 시간 업데이트)
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            // 명령 초기화
            NavigateCommand = new RelayCommand<string>(ExecuteNavigate);
            LogoutCommand = new RelayCommand(ExecuteLogout);
            ShowNotificationsCommand = new RelayCommand(ExecuteShowNotifications);

            // 알림 초기화
            Notifications = new ObservableCollection<NotificationItem>();
            InitializeNotifications();

            // 기본 페이지 설정 (대시보드)
            ExecuteNavigate("Dashboard");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public CurrentUser CurrentUser { get; }

        public string CurrentTime => DateTime.Now.ToString("HH:mm:ss");
        public string CurrentDate => DateTime.Now.ToString("yyyy년 MM월 dd일 dddd");

        public int NotificationCount
        {
            get => _notificationCount;
            set
            {
                _notificationCount = value;
                OnPropertyChanged(nameof(NotificationCount));
            }
        }

        public ObservableCollection<NotificationItem> Notifications
        {
            get => _notifications;
            set
            {
                _notifications = value;
                OnPropertyChanged(nameof(Notifications));
            }
        }

        public Page CurrentPage
        {
            get => _currentPage;
            set
            {
                _currentPage = value;
                OnPropertyChanged(nameof(CurrentPage));
            }
        }

        public ICommand NavigateCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand ShowNotificationsCommand { get; }

        private void Timer_Tick(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(CurrentTime));
            OnPropertyChanged(nameof(CurrentDate));
            CheckForNewNotifications(); // 주기적으로 새 알림 확인
        }

        private void ExecuteNavigate(string pageName)
        {
            Page page = null;

            switch (pageName)
            {
                case "Dashboard":
                    page = new DashboardPage();
                    break;
                case "ProductionPlan":
                    page = new ProductionPlanPage();
                    break;
                // WorkOrder 추가
                case "WorkOrder":
                    page = new WorkOrderPage();
                    break;
                case "Equipment":
                    page = new EquipmentPage();
                    break;
                case "Inventory":
                    page = new InventoryPage();
                    break;

                //11/14

                case "Shipment":
                    page = new ShipmentPage();
                    break;
                case "Contract":
                    page = new ContractPage();
                    break;
                case "Log":
                    page = new LogPage();
                    break;
                default:
                    page = new DashboardPage();
                    break;
            }

            if (page != null)
            {
                CurrentPage = page;
            }
        }

        private void ExecuteLogout()
        {
            try
            {
                var result = MessageBox.Show(
                    "로그아웃 하시겠습니까?",
                    "로그아웃",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    App.LogOut();
                    _timer.Stop();

                    var loginWindow = new LoginWindow();
                    loginWindow.Show();

                    // 현재 창 닫기
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window is MainWindow)
                        {
                            window.Close();
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"로그아웃 중 오류가 발생했습니다: {ex.Message}",
                    "오류",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void InitializeNotifications()
        {
            // 초기 알림 설정 (예시)
            Notifications.Add(new NotificationItem
            {
                Title = "설비 점검 필요",
                Message = "라인1 설비 A의 정기 점검 일정이 도래했습니다.",
                Time = DateTime.Now.AddHours(-1),
                Type = NotificationType.Warning
            });

            UpdateNotificationCount();
        }

        private void CheckForNewNotifications()
        {
            // 실제 구현에서는 DB나 서비스에서 새 알림을 확인
            // 여기서는 예시로 매 시간 정각에 테스트 알림 추가
            if (DateTime.Now.Minute == 0 && DateTime.Now.Second == 0)
            {
                Notifications.Add(new NotificationItem
                {
                    Title = "생산 목표 달성",
                    Message = "오늘의 생산 목표가 달성되었습니다.",
                    Time = DateTime.Now,
                    Type = NotificationType.Success
                });

                UpdateNotificationCount();
            }
        }

        private void UpdateNotificationCount()
        {
            NotificationCount = Notifications.Count;
        }

        private void ExecuteShowNotifications()
        {
            // 알림 팝업 표시 로직
            var notificationWindow = new NotificationWindow(Notifications);
            notificationWindow.Owner = Application.Current.MainWindow;
            notificationWindow.ShowDialog();
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Cleanup()
        {
            _timer.Stop();
        }
    }

    public class NotificationItem
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public DateTime Time { get; set; }
        public NotificationType Type { get; set; }
    }

    public enum NotificationType
    {
        Info,
        Warning,
        Success,
        Error
    }
}