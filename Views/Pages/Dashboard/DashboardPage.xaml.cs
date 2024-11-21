using System.Windows.Controls;
using MES.Solution.ViewModels;

namespace MES.Solution.Views.Pages
{
    public partial class DashboardPage : Page
    {
        private readonly DashboardViewModel _viewModel;

        public DashboardPage()
        {
            InitializeComponent();
            _viewModel = new DashboardViewModel();
            DataContext = _viewModel;
        }

        public void OnNavigatedFrom()
        {
            _viewModel?.Cleanup();
        }
    }
}