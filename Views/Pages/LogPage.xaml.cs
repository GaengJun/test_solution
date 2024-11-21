using MES.Solution.ViewModels;
using System.Windows.Controls;

namespace MES.Solution.Views.Pages
{
    /// <summary>
    /// LogPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LogPage : Page
    {
        private readonly LogViewModel _viewModel;
        public LogPage()
        {
            InitializeComponent();
            _viewModel = new LogViewModel();
            DataContext = _viewModel;
        }
        public void OnNavigatedFrom()
        {
            _viewModel?.Cleanup();
        }
    }
}
