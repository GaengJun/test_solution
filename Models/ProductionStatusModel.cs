using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MES.Solution.Models
{
    public class ProductionStatusItem : INotifyPropertyChanged
    {
        private string _workOrderCode;
        private DateTime _productionDate;
        private string _productionLine;
        private int _workOrderSequence;
        private string _productCode;
        private string _productName;
        private string _unit;
        private int _orderQuantity;
        private int _productionQuantity;
        private string _status;
        private string _employeeName;

        public string WorkOrderCode
        {
            get => _workOrderCode;
            set
            {
                if (_workOrderCode != value)
                {
                    _workOrderCode = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime ProductionDate
        {
            get => _productionDate;
            set
            {
                if (_productionDate != value)
                {
                    _productionDate = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ProductionLine
        {
            get => _productionLine;
            set
            {
                if (_productionLine != value)
                {
                    _productionLine = value;
                    OnPropertyChanged();
                }
            }
        }

        public int WorkOrderSequence
        {
            get => _workOrderSequence;
            set
            {
                if (_workOrderSequence != value)
                {
                    _workOrderSequence = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ProductCode
        {
            get => _productCode;
            set
            {
                if (_productCode != value)
                {
                    _productCode = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ProductName
        {
            get => _productName;
            set
            {
                if (_productName != value)
                {
                    _productName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Unit
        {
            get => _unit;
            set
            {
                if (_unit != value)
                {
                    _unit = value;
                    OnPropertyChanged();
                }
            }
        }

        public int OrderQuantity
        {
            get => _orderQuantity;
            set
            {
                if (_orderQuantity != value)
                {
                    _orderQuantity = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ProductionQuantity
        {
            get => _productionQuantity;
            set
            {
                if (_productionQuantity != value)
                {
                    _productionQuantity = value;
                    OnPropertyChanged();
                }
            }
        }

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

        public string EmployeeName
        {
            get => _employeeName;
            set
            {
                if (_employeeName != value)
                {
                    _employeeName = value;
                    OnPropertyChanged();
                }
            }
        }



        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}