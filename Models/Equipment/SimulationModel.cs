using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MES.Solution.Models.Equipment
{
    public enum OperationMode
    {
        Manual,
        Automatic
    }

    public class ProductionLine : INotifyPropertyChanged
    {
        private string _lineId;
        private bool _isOperating;
        private DateTime _lastProductionTime;
        private int _totalProduced;
        private OperationMode _operationMode;
        private bool _isPaused;


        public string LineId
        {
            get => _lineId;
            set { _lineId = value; OnPropertyChanged(); }
        }

        public bool IsOperating
        {
            get => _isOperating;
            set { _isOperating = value; OnPropertyChanged(); }
        }

        public bool IsPaused
        {
            get => _isPaused;
            set { _isPaused = value; OnPropertyChanged(); }
        }

        public OperationMode OperationMode
        {
            get => _operationMode;
            set { _operationMode = value; OnPropertyChanged(); }
        }

        public DateTime LastProductionTime
        {
            get => _lastProductionTime;
            set { _lastProductionTime = value; OnPropertyChanged(); }
        }

        public int TotalProduced
        {
            get => _totalProduced;
            set { _totalProduced = value; OnPropertyChanged(); }
        }

        public Equipment[] Equipments { get; } = new Equipment[3];

        public ProductionLine(string lineId)
        {
            LineId = lineId;
            for (int i = 0; i < 3; i++)
            {
                Equipments[i] = new Equipment($"{lineId}-EQ{i + 1}");
            }
            OperationMode = OperationMode.Manual; // 기본값은 수동모드
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class Equipment : INotifyPropertyChanged
    {
        private string _equipmentId;
        private bool _isOperating;
        private int _producedCount;
        private DateTime _lastProductionTime;
        private ProductionStatus _status;
        private string _currentWorkOrder;
        private DateTime _startTime;
        private DateTime _estimatedCompletionTime;

        public string EquipmentId
        {
            get => _equipmentId;
            set { _equipmentId = value; OnPropertyChanged(); }
        }

        public bool IsOperating
        {
            get => _isOperating;
            set { _isOperating = value; OnPropertyChanged(); }
        }

        public int ProducedCount
        {
            get => _producedCount;
            set { _producedCount = value; OnPropertyChanged(); }
        }

        public DateTime LastProductionTime
        {
            get => _lastProductionTime;
            set { _lastProductionTime = value; OnPropertyChanged(); }
        }

        public ProductionStatus Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public string CurrentWorkOrder
        {
            get => _currentWorkOrder;
            set { _currentWorkOrder = value; OnPropertyChanged(); }
        }

        public DateTime StartTime
        {
            get => _startTime;
            set { _startTime = value; OnPropertyChanged(); }
        }

        public DateTime EstimatedCompletionTime
        {
            get => _estimatedCompletionTime;
            set { _estimatedCompletionTime = value; OnPropertyChanged(); }
        }

        public Equipment(string equipmentId)
        {
            EquipmentId = equipmentId;
            Status = ProductionStatus.Idle;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public enum ProductionStatus
    {
        Idle,           // 대기
        Operating,      // 작동중
        Completed,      // 완료
        Error,          // 에러
        Maintenance,    // 유지보수
        Paused          // 일시정지
    }
}