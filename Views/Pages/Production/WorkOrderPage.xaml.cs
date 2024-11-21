using System.Windows.Controls;
using MES.Solution.ViewModels;

namespace MES.Solution.Views.Pages
{
    public partial class WorkOrderPage : Page
    {
        private readonly WorkOrderViewModel _viewModel;

        public WorkOrderPage()
        {
            InitializeComponent();
            _viewModel = new WorkOrderViewModel();
            DataContext = _viewModel;
        }

        public void OnNavigatedFrom()
        {
            _viewModel?.Cleanup();
        }
    }
}