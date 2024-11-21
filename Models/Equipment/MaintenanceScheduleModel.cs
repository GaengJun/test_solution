using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MES.Solution.Models
{
    public class MaintenanceScheduleModel : INotifyPropertyChanged
    {
        private string _equipmentCode;
        private string _productionLine;
        private string _equipmentCompanyName;
        private string _equipmentContactNumber;
        private string _equipmentContactPerson;
        private DateTime _inspectionDate;
        private string _inspectionFrequency;
        private decimal _temperature;
        private decimal _humidity;
        private string _employeeName;
        private DateTime _lastCheckDate;
        private DateTime _nextCheckDate;
        private string _status;
        private bool _isSelected;

        public string EquipmentCode
        {
            get => _equipmentCode;
            set
            {
                _equipmentCode = value;
                OnPropertyChanged();
            }
        }

        public string ProductionLine
        {
            get => _productionLine;
            set
            {
                _productionLine = value;
                OnPropertyChanged();
            }
        }

        public string EquipmentCompanyName
        {
            get => _equipmentCompanyName;
            set
            {
                _equipmentCompanyName = value;
                OnPropertyChanged();
            }
        }

        public string EquipmentContactNumber
        {
            get => _equipmentContactNumber;
            set
            {
                _equipmentContactNumber = value;
                OnPropertyChanged();
            }
        }

        public string EquipmentContactPerson
        {
            get => _equipmentContactPerson;
            set
            {
                _equipmentContactPerson = value;
                OnPropertyChanged();
            }
        }

        public DateTime InspectionDate
        {
            get => _inspectionDate;
            set
            {
                _inspectionDate = value;
                OnPropertyChanged();
            }
        }

        public string InspectionFrequency
        {
            get => _inspectionFrequency;
            set
            {
                _inspectionFrequency = value;
                OnPropertyChanged();
            }
        }

        public decimal Temperature
        {
            get => _temperature;
            set
            {
                _temperature = value;
                OnPropertyChanged();
            }
        }

        public decimal Humidity
        {
            get => _humidity;
            set
            {
                _humidity = value;
                OnPropertyChanged();
            }
        }

        public string EmployeeName
        {
            get => _employeeName;
            set
            {
                _employeeName = value;
                OnPropertyChanged();
            }
        }

        public DateTime LastCheckDate
        {
            get => _lastCheckDate;
            set
            {
                _lastCheckDate = value;
                OnPropertyChanged();
            }
        }

        public DateTime NextCheckDate
        {
            get => _nextCheckDate;
            set
            {
                _nextCheckDate = value;
                OnPropertyChanged();
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}