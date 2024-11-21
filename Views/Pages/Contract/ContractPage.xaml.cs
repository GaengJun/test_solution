using MES.Solution.ViewModels;
using System.Windows.Controls;

namespace MES.Solution.Views.Pages.Contract
{
    /// <summary>
    /// ContractPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ContractPage : Page
    {
        private readonly ContractViewModel _viewModel;
        public ContractPage()
        {
            InitializeComponent();
            _viewModel = new ContractViewModel();
            DataContext = _viewModel;
        }

        public void OnNavigatedFrom()
        {
            _viewModel?.Cleanup();
        }
    }
}
