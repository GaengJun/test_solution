// Views/Pages/Production/ProductionStatusPage.xaml.cs
using System.Windows.Controls;
using MES.Solution.ViewModels;

namespace MES.Solution.Views.Pages.Production
{
    public partial class ProductionStatusPage : Page
    {
        private readonly ProductionStatusViewModel _viewModel;

        public ProductionStatusPage()
        {
            InitializeComponent();
            _viewModel = new ProductionStatusViewModel();
            DataContext = _viewModel;
        }

        public void OnNavigatedFrom()
        {
            _viewModel?.Cleanup();
        }
    }
}