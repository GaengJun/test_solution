using MES.Solution.Helpers;
using MES.Solution.ViewModels.Equipment;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using System;
using System.Linq;
using System.Windows;
using MES.Solution.Views;

public class PlcStatusModel : INotifyPropertyChanged
{
    private readonly PlcViewModel _viewModel;

    public event PropertyChangedEventHandler PropertyChanged;

    private string _operationStatus;
    public string Name { get; set; }
    public int LineNumber { get; set; }
    public string OperationStatus
    {
        get => _operationStatus;
        set
        {
            if (_operationStatus != value)
            {
                _operationStatus = value;
                OnPropertyChanged();
            }
        }
    }


    private string _status;
    public string Status
    {
        get => _status;
        set
        {
            if (_status != value)
            {
                _status = value;
                OnPropertyChanged();
            }
        }
    }

    private SolidColorBrush _statusColor;
    public SolidColorBrush StatusColor
    {
        get => _statusColor;
        set
        {
            if (_statusColor != value)
            {
                _statusColor = value;
                OnPropertyChanged();
            }
        }
    }

    private string _selectedAction;
    public string SelectedAction
    {
        get => _selectedAction;
        set
        {
            if (_selectedAction != value)
            {
                _selectedAction = value;
                OnPropertyChanged();
            }
        }
    }

    public ObservableCollection<string> AvailableActions { get; set; }

    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand InspectionCommand { get; }
    public ICommand ExecuteActionCommand { get; }
    public ICommand ShowDetailsCommand { get; } // 추가
    public PlcStatusModel(PlcViewModel viewModel, int lineNumber)
    {
        _viewModel = viewModel;
        LineNumber = lineNumber;

        // 명령 초기화
        ConnectCommand = new RelayCommand(() => viewModel.ConnectPlc(this, LineNumber));
        DisconnectCommand = new RelayCommand(() => viewModel.DisconnectPlc(this, LineNumber));
        InspectionCommand = new RelayCommand(() => viewModel.InspectPlc(this, LineNumber));
        ExecuteActionCommand = new RelayCommand(ExecuteSelectedAction);
        ShowDetailsCommand = new RelayCommand(ShowPlcDetails); // 추가

        // 기본값 설정
        AvailableActions = new ObservableCollection<string> { "동작 1", "동작 2", "동작 3" };
        SelectedAction = AvailableActions.FirstOrDefault();

        OperationStatus = "대기 중";
    }

    private void ExecuteSelectedAction()
    {
        if (!string.IsNullOrEmpty(SelectedAction))
        {
            try
            {
                _viewModel.ExecutePlcAction(LineNumber, SelectedAction);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"동작 실행 중 오류 발생: {ex.Message}");
            }
        }
        else
        {
            MessageBox.Show("실행할 동작을 선택해주세요.");
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void ShowPlcDetails()
    {
        var detailWindow = new PlcDetailWindow(this, _viewModel);
        detailWindow.Owner = Application.Current.MainWindow;
        detailWindow.ShowDialog();
    }
}