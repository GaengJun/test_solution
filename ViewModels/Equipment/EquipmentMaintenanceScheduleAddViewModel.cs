using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MES.Solution.Helpers;
using MES.Solution.Models.Equipment;
using MES.Solution.Services;
using System.Threading.Tasks;
using System.Windows;
using MES.Solution.Models;

namespace MES.Solution.ViewModels.Equipment
{
    public class EquipmentMaintenanceScheduleAddViewModel : INotifyPropertyChanged
    {
        private readonly EquipmentMaintenanceScheduleService _service;
        private readonly LogService _logService;
        private readonly bool _isEditMode;
        private string _originalEquipmentCode;
        private string _windowTitle;
        private string _equipmentCode;
        private string _selectedProductionLine;
        private string _equipmentCompanyName;
        private string _equipmentContactNumber;
        private string _equipmentContactPerson;
        private DateTime _inspectionDate = DateTime.Today;
        private string _selectedInspectionFrequency;
        private decimal _temperature;
        private decimal _humidity;
        private string _errorMessage;
        private bool _hasError;
        private FormMode _mode;

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler RequestClose;

        public EquipmentMaintenanceScheduleAddViewModel(bool isEdit = false)
        {
            _service = new EquipmentMaintenanceScheduleService();
            _logService = new LogService();
            _isEditMode = isEdit;
            Mode = isEdit ? FormMode.Edit : FormMode.Add;

            // 초기화
            ProductionLines = new ObservableCollection<string> { "라인1", "라인2", "라인3" };
            InspectionFrequencies = new ObservableCollection<string> { "월간", "분기" };

            // 커맨드 초기화
            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            CancelCommand = new RelayCommand(ExecuteCancel);

            WindowTitle = isEdit ? "장비점검 일정 수정" : "장비점검 일정 등록";

            ValidateAll();
        }

        #region Properties

        public string WindowTitle
        {
            get => _windowTitle;
            set
            {
                _windowTitle = value;
                OnPropertyChanged();
            }
        }

        public string EquipmentCode
        {
            get => _equipmentCode;
            set
            {
                _equipmentCode = value;
                ValidateEquipmentCode();
                OnPropertyChanged();
            }
        }

        public string SelectedProductionLine
        {
            get => _selectedProductionLine;
            set
            {
                _selectedProductionLine = value;
                ValidateProductionLine();
                OnPropertyChanged();
            }
        }

        public string EquipmentCompanyName
        {
            get => _equipmentCompanyName;
            set
            {
                _equipmentCompanyName = value;
                ValidateEquipmentCompanyName();
                OnPropertyChanged();
            }
        }

        public string EquipmentContactNumber
        {
            get => _equipmentContactNumber;
            set
            {
                _equipmentContactNumber = value;
                ValidateEquipmentContactNumber();
                OnPropertyChanged();
            }
        }

        public string EquipmentContactPerson
        {
            get => _equipmentContactPerson;
            set
            {
                _equipmentContactPerson = value;
                ValidateEquipmentContactPerson();
                OnPropertyChanged();
            }
        }

        public DateTime InspectionDate
        {
            get => _inspectionDate;
            set
            {
                _inspectionDate = value;
                ValidateInspectionDate();
                OnPropertyChanged();
            }
        }

        public string SelectedInspectionFrequency
        {
            get => _selectedInspectionFrequency;
            set
            {
                _selectedInspectionFrequency = value;
                ValidateInspectionFrequency();
                OnPropertyChanged();
            }
        }

        public decimal Temperature
        {
            get => _temperature;
            set
            {
                _temperature = value;
                ValidateTemperature();
                OnPropertyChanged();
            }
        }

        public decimal Humidity
        {
            get => _humidity;
            set
            {
                _humidity = value;
                ValidateHumidity();
                OnPropertyChanged();
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
                HasError = !string.IsNullOrEmpty(value);
            }
        }

        public bool HasError
        {
            get => _hasError;
            set
            {
                _hasError = value;
                OnPropertyChanged();
            }
        }

        public FormMode Mode
        {
            get => _mode;
            set
            {
                if (_mode != value)
                {
                    _mode = value;
                    WindowTitle = value == FormMode.Add ? "장비점검 일정 등록" : "장비점검 일정 수정";
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region Collections

        public ObservableCollection<string> ProductionLines { get; }
        public ObservableCollection<string> InspectionFrequencies { get; }

        #endregion

        #region Commands

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        #endregion

        #region Methods

        public void LoadData(MaintenanceScheduleModel model)
        {
            if (model == null) return;

            try
            {
                _originalEquipmentCode = model.EquipmentCode;
                EquipmentCode = model.EquipmentCode;
                SelectedProductionLine = model.ProductionLine;
                EquipmentCompanyName = model.EquipmentCompanyName;
                EquipmentContactNumber = model.EquipmentContactNumber;
                EquipmentContactPerson = model.EquipmentContactPerson;
                InspectionDate = model.InspectionDate;
                SelectedInspectionFrequency = model.InspectionFrequency;
                Temperature = model.Temperature;
                Humidity = model.Humidity;

                ValidateAll();
                Mode = FormMode.Edit;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"데이터 로드 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateAll()
        {
            ValidateEquipmentCode();
            ValidateProductionLine();
            ValidateEquipmentCompanyName();
            ValidateEquipmentContactNumber();
            ValidateEquipmentContactPerson();
            ValidateInspectionDate();
            ValidateInspectionFrequency();
            ValidateTemperature();
            ValidateHumidity();

            return string.IsNullOrEmpty(ErrorMessage);
        }

        private void ValidateEquipmentCode()
        {
            if (string.IsNullOrEmpty(EquipmentCode))
            {
                ErrorMessage = "장비코드를 입력하세요.";
                return;
            }
            ErrorMessage = string.Empty;
        }

        private void ValidateProductionLine()
        {
            if (string.IsNullOrEmpty(SelectedProductionLine))
            {
                ErrorMessage = "생산라인을 선택하세요.";
                return;
            }
            ErrorMessage = string.Empty;
        }

        private void ValidateEquipmentCompanyName()
        {
            if (string.IsNullOrEmpty(EquipmentCompanyName))
            {
                ErrorMessage = "장비업체명을 입력하세요.";
                return;
            }
            ErrorMessage = string.Empty;
        }

        private void ValidateEquipmentContactNumber()
        {
            if (string.IsNullOrEmpty(EquipmentContactNumber))
            {
                ErrorMessage = "업체연락처를 입력하세요.";
                return;
            }
            ErrorMessage = string.Empty;
        }

        private void ValidateEquipmentContactPerson()
        {
            if (string.IsNullOrEmpty(EquipmentContactPerson))
            {
                ErrorMessage = "담당자를 입력하세요.";
                return;
            }
            ErrorMessage = string.Empty;
        }

        private void ValidateInspectionDate()
        {
            if (InspectionDate == DateTime.MinValue)
            {
                ErrorMessage = "점검일자를 선택하세요.";
                return;
            }
            ErrorMessage = string.Empty;
        }

        private void ValidateInspectionFrequency()
        {
            if (string.IsNullOrEmpty(SelectedInspectionFrequency))
            {
                ErrorMessage = "점검주기를 선택하세요.";
                return;
            }
            ErrorMessage = string.Empty;
        }

        private void ValidateTemperature()
        {
            if (Temperature < -50 || Temperature > 100)
            {
                ErrorMessage = "온도는 -50°C에서 100°C 사이여야 합니다.";
                return;
            }
            ErrorMessage = string.Empty;
        }

        private void ValidateHumidity()
        {
            if (Humidity < 0 || Humidity > 100)
            {
                ErrorMessage = "습도는 0%에서 100% 사이여야 합니다.";
                return;
            }
            ErrorMessage = string.Empty;
        }

        private bool CanExecuteSave()
        {
            return !HasError &&
                   !string.IsNullOrEmpty(EquipmentCode) &&
                   !string.IsNullOrEmpty(SelectedProductionLine) &&
                   !string.IsNullOrEmpty(EquipmentCompanyName) &&
                   !string.IsNullOrEmpty(EquipmentContactNumber) &&
                   !string.IsNullOrEmpty(EquipmentContactPerson) &&
                   !string.IsNullOrEmpty(SelectedInspectionFrequency) &&
                   Temperature >= -50 && Temperature <= 100 &&
                   Humidity >= 0 && Humidity <= 100;
        }

        private async void ExecuteSave()
        {
            try
            {
                if (!ValidateAll())
                {
                    return;
                }

                var schedule = new MaintenanceScheduleModel
                {
                    EquipmentCode = EquipmentCode,
                    ProductionLine = SelectedProductionLine,
                    EquipmentCompanyName = EquipmentCompanyName,
                    EquipmentContactNumber = EquipmentContactNumber,
                    EquipmentContactPerson = EquipmentContactPerson,
                    InspectionDate = InspectionDate,
                    InspectionFrequency = SelectedInspectionFrequency,
                    Temperature = Temperature,
                    Humidity = Humidity,
                    EmployeeName = App.CurrentUser.Username
                };

                if (Mode == FormMode.Edit)
                {
                    await _service.UpdateSchedule(schedule, _originalEquipmentCode);
                    await _logService.SaveLogAsync(App.CurrentUser.UserId, "장비점검 일정 수정",
                        $"장비코드: {schedule.EquipmentCode}, 생산라인: {schedule.ProductionLine}");
                }
                else
                {
                    await _service.AddSchedule(schedule);
                    await _logService.SaveLogAsync(App.CurrentUser.UserId, "장비점검 일정 등록",
                        $"장비코드: {schedule.EquipmentCode}, 생산라인: {schedule.ProductionLine}");
                }

                MessageBox.Show(Mode == FormMode.Edit ? "수정되었습니다." : "등록되었습니다.",
                    "알림", MessageBoxButton.OK, MessageBoxImage.Information);

                RequestClose?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"저장 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteCancel()
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        #endregion
    }
}