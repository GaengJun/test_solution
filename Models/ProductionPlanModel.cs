using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MES.Solution.Models
{
    public class ProductionPlanModel : INotifyPropertyChanged
    {
        private bool _isSelected;
        public string PlanNumber { get; set; }          // WorkOrderCode
        public DateTime PlanDate { get; set; }          // ProductionDate
        public string ProductionLine { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public int PlannedQuantity { get; set; }        // OrderQuantity
        public int ProductionQuantity { get; set; }
        public decimal AchievementRate { get; set; }
        public string Status { get; set; }
        public string WorkShift { get; set; }           // 근무조 추가
        public string Remarks { get; set; }
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
