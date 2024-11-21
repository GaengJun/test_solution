using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MES.Solution.Helpers
{
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _executeAsync;
        private readonly Func<bool> _canExecute;

        public AsyncRelayCommand(Func<Task> executeAsync, Func<bool> canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute?.Invoke() ?? true;
        }

        public async void Execute(object parameter)
        {
            await ExecuteAsync();
        }

        private async Task ExecuteAsync()
        {
            try
            {
                await _executeAsync();
            }
            catch (Exception ex)
            {
                // 예외 처리 (필요시 적절히 구현)
                Console.WriteLine($"AsyncRelayCommand 실행 중 오류 발생: {ex.Message}");
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
