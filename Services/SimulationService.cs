using MES.Solution.Models.Equipment;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace MES.Solution.Services
{
    public class SimulationService
    {
        private static readonly Lazy<SimulationService> _instance =
            new Lazy<SimulationService>(() => new SimulationService());


        public static SimulationService Instance => _instance.Value;

        private readonly Timer _productionTimer;
        public ObservableCollection<ProductionLine> ProductionLines { get; }

        public event EventHandler<ProductionEventArgs> ProductionCompleted;
        public event EventHandler<ProductionEventArgs> ProductionStarted;
        public event EventHandler<ProductionEventArgs> ProductionPaused;
        public event EventHandler<ProductionEventArgs> ProductionResumed;
        public event EventHandler<ProductionEventArgs> ProductionError;

        private const int PRODUCTION_CYCLE_TIME = 30; // 30초

        private SimulationService()
        {
            ProductionLines = new ObservableCollection<ProductionLine>
        {
            new ProductionLine("라인1"),
            new ProductionLine("라인2"),
            new ProductionLine("라인3")
        };

            _productionTimer = new Timer(1000); // 1초마다 체크
            _productionTimer.Elapsed += OnProductionTimerElapsed;
        }

        public void StartProduction(string lineId, string equipmentId, string workOrderNumber)
        {
            try
            {
                var line = ProductionLines.FirstOrDefault(l => l.LineId == lineId);
                var equipment = line?.Equipments.FirstOrDefault(e => e.EquipmentId == equipmentId);

                if (equipment == null) return;

                equipment.CurrentWorkOrder = workOrderNumber;
                equipment.Status = ProductionStatus.Operating;
                equipment.LastProductionTime = DateTime.Now;
                equipment.IsOperating = true;

                _productionTimer.Start();

                ProductionStarted?.Invoke(this, new ProductionEventArgs
                {
                    LineId = lineId,
                    EquipmentId = equipmentId,
                    WorkOrderNumber = workOrderNumber,
                    ProductionTime = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"생산 시작 중 오류 발생: {ex.Message}", "오류");
            }
        }

        public void PauseProduction(string lineId, string equipmentId)
        {
            var line = ProductionLines.FirstOrDefault(l => l.LineId == lineId);
            var equipment = line?.Equipments.FirstOrDefault(e => e.EquipmentId == equipmentId);

            if (equipment != null && equipment.Status == ProductionStatus.Operating)
            {
                equipment.Status = ProductionStatus.Paused;
                equipment.IsOperating = false;

                var hasRunningEquipment = ProductionLines.Any(l =>
                    l.Equipments.Any(e => e.Status == ProductionStatus.Operating));

                if (!hasRunningEquipment)
                {
                    _productionTimer.Stop();
                }

                ProductionPaused?.Invoke(this, new ProductionEventArgs
                {
                    LineId = lineId,
                    EquipmentId = equipmentId,
                    WorkOrderNumber = equipment.CurrentWorkOrder,
                    ProductionTime = DateTime.Now
                });
            }
        }

        public void ResumeProduction(string lineId, string equipmentId)
        {
            var line = ProductionLines.FirstOrDefault(l => l.LineId == lineId);
            var equipment = line?.Equipments.FirstOrDefault(e => e.EquipmentId == equipmentId);

            if (equipment != null && equipment.Status == ProductionStatus.Paused)
            {
                equipment.Status = ProductionStatus.Operating;
                equipment.IsOperating = true;
                equipment.LastProductionTime = DateTime.Now;

                _productionTimer.Start();

                ProductionResumed?.Invoke(this, new ProductionEventArgs
                {
                    LineId = lineId,
                    EquipmentId = equipmentId,
                    WorkOrderNumber = equipment.CurrentWorkOrder,
                    ProductionTime = DateTime.Now
                });
            }
        }

        public void StopSimulation()
        {
            _productionTimer.Stop();
            foreach (var line in ProductionLines)
            {
                line.IsOperating = false;
                foreach (var equipment in line.Equipments)
                {
                    equipment.IsOperating = false;
                    equipment.Status = ProductionStatus.Idle;
                }
            }
        }

        public void ManualComplete(string lineId, string equipmentId)
        {
            var line = ProductionLines.FirstOrDefault(l => l.LineId == lineId);
            var equipment = line?.Equipments.FirstOrDefault(e => e.EquipmentId == equipmentId);

            if (equipment != null && equipment.Status == ProductionStatus.Operating)
            {
                CompleteProduction(line, equipment);
            }
        }

        private void OnProductionTimerElapsed(object sender, ElapsedEventArgs e)
        {
            foreach (var line in ProductionLines)
            {
                foreach (var equipment in line.Equipments)
                {
                    if (equipment.Status != ProductionStatus.Operating) continue;

                    // 30초마다 제품 생산
                    if ((DateTime.Now - equipment.LastProductionTime).TotalSeconds >= 30)
                    {
                        equipment.ProducedCount++;
                        equipment.LastProductionTime = DateTime.Now;

                        ProductionCompleted?.Invoke(this, new ProductionEventArgs
                        {
                            LineId = line.LineId,
                            EquipmentId = equipment.EquipmentId,
                            WorkOrderNumber = equipment.CurrentWorkOrder,
                            ProductionTime = DateTime.Now,
                            ProductCount = 1
                        });
                    }
                }
            }
        }

        private void CompleteProduction(ProductionLine line, Equipment equipment)
        {
            equipment.ProducedCount++;
            equipment.LastProductionTime = DateTime.Now;
            equipment.Status = ProductionStatus.Completed;
            equipment.IsOperating = false;
            line.TotalProduced++;
            line.LastProductionTime = DateTime.Now;

            ProductionCompleted?.Invoke(this, new ProductionEventArgs
            {
                LineId = line.LineId,
                EquipmentId = equipment.EquipmentId,
                WorkOrderNumber = equipment.CurrentWorkOrder,
                ProductionTime = DateTime.Now,
                ProductCount = 1
            });

            // 자동 모드일 경우 다음 작업 시작
            if (line.OperationMode == OperationMode.Automatic)
            {
                equipment.Status = ProductionStatus.Idle;
                // TODO: 다음 작업 있을 경우 자동 시작
            }
        }

        public bool HasAvailableEquipment(string lineId)
        {
            var line = ProductionLines.FirstOrDefault(l => l.LineId == lineId);
            return line?.Equipments.Any(e => e.Status == ProductionStatus.Idle) ?? false;
        }

        public void SetOperationMode(string lineId, OperationMode mode)
        {
            var line = ProductionLines.FirstOrDefault(l => l.LineId == lineId);
            if (line != null)
            {
                line.OperationMode = mode;
            }
        }

        public void SimulateError(string lineId, string equipmentId)
        {
            var line = ProductionLines.FirstOrDefault(l => l.LineId == lineId);
            var equipment = line?.Equipments.FirstOrDefault(e => e.EquipmentId == equipmentId);

            if (equipment != null)
            {
                equipment.Status = ProductionStatus.Error;
                equipment.IsOperating = false;

                ProductionError?.Invoke(this, new ProductionEventArgs
                {
                    LineId = lineId,
                    EquipmentId = equipmentId,
                    ProductionTime = DateTime.Now,
                    ErrorMessage = "Simulated Error"
                });
            }
        }
    }

    public class ProductionEventArgs : EventArgs
    {
    public string LineId { get; set; }
    public string EquipmentId { get; set; }
    public string WorkOrderNumber { get; set; }
    public DateTime ProductionTime { get; set; }
    public int ProductCount { get; set; }
    public string ErrorMessage { get; set; }
    }
}
