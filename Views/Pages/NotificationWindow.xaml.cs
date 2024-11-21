using MES.Solution.ViewModels;
using System.Collections.ObjectModel;
using System.Windows;

namespace MES.Solution.Views
{
    public partial class NotificationWindow : Window
    {
        public NotificationWindow(ObservableCollection<NotificationItem> notifications)
        {
            InitializeComponent();
            DataContext = notifications;
        }
    }
}