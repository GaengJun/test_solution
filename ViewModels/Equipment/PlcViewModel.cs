using ActUtlTypeLib;
using MES.Solution.Helpers;
using MES.Solution.Models;
using MES.Solution.Services;
using MES.Solution.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace MES.Solution.ViewModels.Equipment
{
    public class PlcViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<PlcStatusModel> PlcStatuses { get; } = new ObservableCollection<PlcStatusModel>();
        private readonly Dictionary<int, ActUtlType> PLCs = new Dictionary<int, ActUtlType>();
        private readonly Dictionary<int, DispatcherTimer> ConnectionCheckTimers = new Dictionary<int, DispatcherTimer>();
        private readonly Dictionary<int, DispatcherTimer> WatchJobCheckTimers = new Dictionary<int, DispatcherTimer>();
        private readonly Dictionary<int, bool> ConnectionStates = new Dictionary<int, bool>();//실행저장용
        private readonly Dictionary<int, string> CurrentActions = new Dictionary<int, string>();//동작저장용
        private readonly Dictionary<int, int[]> MonitorData = new Dictionary<int, int[]>();

        private readonly LogService _logService;//로그사용

        private static readonly Dictionary<int, int> PlcStationNumbers = new Dictionary<int, int>
        {
            { 1, 1 }, // 라인 1의 스테이션 번호
            { 2, 2 }, // 라인 2의 스테이션 번호
            { 3, 3 }  // 라인 3의 스테이션 번호
        };

        private string _operationStatus;

        public string OperationStatus
        {
            get { return _operationStatus; }
            set
            {
                _operationStatus = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<string> _availableActions;
        public ObservableCollection<string> AvailableActions
        {
            get { return _availableActions; }
            set
            {
                _availableActions = value;
                OnPropertyChanged();
            }
        }

        private string _selectedAction;
        public string SelectedAction
        {
            get { return _selectedAction; }
            set
            {
                _selectedAction = value;
                OnPropertyChanged();
            }
        }

        public ICommand ExecuteActionCommand { get; }
        public PlcViewModel()
        {
            // LogService 초기화
            _logService = new LogService();

            // 동작 리스트 초기화
            AvailableActions = new ObservableCollection<string>
            {
                "동작 1",
                "동작 2",
                "동작 3"
            };

            // 기본 동작 설정
            SelectedAction = AvailableActions[0];

            // 실행 명령 설정
            ExecuteActionCommand = new RelayCommand(ExecuteAction);

            // PLC 인스턴스 초기화
            for (int i = 1; i <= 3; i++)
            {
                PLCs[i] = new ActUtlType();
                MonitorData[i] = new int[1];
                ConnectionStates[i] = false;

                // 연결 감시용 타이머
                var connectionTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
                int lineNumber = i;
                connectionTimer.Tick += (s, e) => ConnectionCheckTimer_Tick(lineNumber);
                ConnectionCheckTimers[i] = connectionTimer;

                // 동작 감시용 타이머
                var watchJobTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
                watchJobTimer.Tick += (s, e) => WatchJobCheckTimer_Tick(lineNumber);
                WatchJobCheckTimers[i] = watchJobTimer;
            }
            Application.Current.Dispatcher.Invoke(() =>
            {
                for (int i = 1; i <= 3; i++)
                {
                    PLCs[i] = new ActUtlType();
                    MonitorData[i] = new int[1];
                    ConnectionStates[i] = false;

                    // 연결 감시용 타이머
                    var connectionTimer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(1)
                    };
                    int lineNumber = i;
                    connectionTimer.Tick += (s, e) => ConnectionCheckTimer_Tick(lineNumber);
                    ConnectionCheckTimers[i] = connectionTimer;

                    // 동작 감시용 타이머
                    var watchJobTimer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(1)
                    };
                    watchJobTimer.Tick += (s, e) => WatchJobCheckTimer_Tick(lineNumber);
                    WatchJobCheckTimers[i] = watchJobTimer;
                }

                InitializePlcStatuses();
            });
        }

        private void ExecuteAction()
        {
            // 실행 버튼 클릭 시 동작을 실행
            if (!string.IsNullOrEmpty(SelectedAction))
            {
                // 선택된 동작 처리
                MessageBox.Show($"선택된 동작: {SelectedAction} 실행!");
                // 실제 PLC에 동작을 실행하는 로직을 추가할 수 있습니다.
            }
            else
            {
                MessageBox.Show("동작을 선택하세요.");
            }
        }

        public async void ExecutePlcAction(int lineNumber, string action)
        {
            try
            {
                // PLC 연결 상태 확인
                if (!ConnectionStates[lineNumber])
                {
                    MessageBox.Show("PLC가 연결되어 있지 않습니다. 먼저 PLC를 연결해주세요.");
                    return;
                }

                var plc = PLCs[lineNumber];
                UpdatePlcStatus(lineNumber, $"{action} 실행 중", Colors.Orange);

                switch (action)
                {
                    case "동작 1":
                        try
                        {
                            // 예: 특정 디바이스에 값 쓰기
                            int ret = plc.SetDevice("M1", 1);  // D100 주소에 1 쓰기
                            if (ret == 0)
                            {
                                CurrentActions[lineNumber] = action;
                                MessageBox.Show($"라인 {lineNumber}의 {action} 실행 시작");
                                WatchJobCheckTimers[lineNumber].Start(); // 동작 감시 타이머 시작
                                // 동작 1 실행 로그
                                string actionDetail = $"라인: {lineNumber}, 실행: {action}";
                                await _logService.SaveLogAsync(App.CurrentUser.UserId, "PLC 동작 실행", actionDetail);
                            }
                            else
                            {
                                throw new Exception($"PLC 쓰기 실패 (에러코드: {ret})");
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"동작 1 실행 중 오류: {ex.Message}");
                        }
                        break;

                    case "동작 2":
                        try
                        {
                            // 예: 특정 디바이스에 값 쓰기
                            int ret = plc.SetDevice("M2", 1);  // D100 주소에 2 쓰기
                            if (ret == 0)
                            {
                                CurrentActions[lineNumber] = action;
                                MessageBox.Show($"라인 {lineNumber}의 {action} 실행 시작");
                                WatchJobCheckTimers[lineNumber].Start(); // 동작 감시 타이머 시작
                                // 동작 2 실행 로그
                                string actionDetail = $"라인: {lineNumber}, 실행: {action}";
                                await _logService.SaveLogAsync(App.CurrentUser.UserId, "PLC 동작 실행", actionDetail);
                            }
                            else
                            {
                                throw new Exception($"PLC 쓰기 실패 (에러코드: {ret})");
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"동작 1 실행 중 오류: {ex.Message}");
                        }
                        break;

                    case "동작 3":
                        try
                        {
                            // 예: 특정 디바이스에 값 쓰기
                            int ret = plc.SetDevice("M3", 1);  // D100 주소에 2 쓰기
                            if (ret == 0)
                            {
                                CurrentActions[lineNumber] = action;
                                MessageBox.Show($"라인 {lineNumber}의 {action} 실행 시작");
                                WatchJobCheckTimers[lineNumber].Start(); // 동작 감시 타이머 시작
                                // 동작 3 실행 로그
                                string actionDetail = $"라인: {lineNumber}, 실행: {action}";
                                await _logService.SaveLogAsync(App.CurrentUser.UserId, "PLC 동작 실행", actionDetail);
                            }
                            else
                            {
                                throw new Exception($"PLC 쓰기 실패 (에러코드: {ret})");
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"동작 1 실행 중 오류: {ex.Message}");
                        }
                        break;

                    default:
                        MessageBox.Show("지원하지 않는 동작입니다.");
                        return;
                }

                // 동작 완료 후 상태 업데이트
                //UpdatePlcStatus(lineNumber, "연결됨", Colors.Green);
            }
            catch (Exception ex)
            {
                UpdatePlcStatus(lineNumber, "오류 발생", Colors.Red);
                MessageBox.Show($"동작 실행 중 오류가 발생했습니다: {ex.Message}");
            }
        }
        private void InitializePlcStatuses()
        {
            for (int i = 1; i <= 3; i++)
            {
                // lineNumber를 생성자에 전달
                var plc = new PlcStatusModel(this, i)
                {
                    Name = $"라인 {i}",
                    Status = "연결 끊김",
                    StatusColor = new SolidColorBrush(Colors.Gray),
                    AvailableActions = new ObservableCollection<string> { "동작 1", "동작 2", "동작 3" },
                    SelectedAction = "동작 1"
                };

                // 추가 속성이나 명령 설정
                PlcStatuses.Add(plc);
            }
        }


        private void StartConnectionMonitoring(int lineNumber)//시작시 한번만 돎
        {
            try
            {
                var plc = PLCs[lineNumber];
                string deviceLabel = "SM400";//PLC시작신호로 변경요망
                int size = 1;
                int monitorCycle = 1;
                MonitorData[lineNumber][0] = 1;

                int ret = plc.EntryDeviceStatus(deviceLabel, size, monitorCycle, ref MonitorData[lineNumber][0]);

                if (ret == 0)
                {
                    ConnectionStates[lineNumber] = true;
                    ConnectionCheckTimers[lineNumber].Start();
                    UpdatePlcStatus(lineNumber, "연결됨", Colors.Green);
                }
                else
                {
                    ConnectionStates[lineNumber] = false;
                    UpdatePlcStatus(lineNumber, "연결 실패", Colors.Red);
                    MessageBox.Show($"라인 {lineNumber} 모니터링 시작 실패. 에러코드: {ret}");
                }
            }
            catch (Exception ex)
            {
                ConnectionStates[lineNumber] = false;
                UpdatePlcStatus(lineNumber, "오류 발생", Colors.Red);
                MessageBox.Show($"라인 {lineNumber} 모니터링 설정 중 오류 발생: {ex.Message}");
            }
        }

        static int iReconnetCount = 0;

        private async void ConnectionCheckTimer_Tick(int lineNumber)//시간마다 체크(연결감시용)
        {
            try
            {
                var plc = PLCs[lineNumber];
                int data = 0;
                int ret = plc.GetDevice("SM400", out data);
                

                await Application.Current.Dispatcher.Invoke(async () =>
                {
                    if (ret == 0 && data == 1)
                    {
                        if (!ConnectionStates[lineNumber])
                        {
                            ConnectionStates[lineNumber] = true;
                            UpdatePlcStatus(lineNumber, "연결됨", Colors.Green);
                        }
                    }
                    else
                    {
                        if (ConnectionStates[lineNumber])
                        {
                            if (iReconnetCount > 4)//5번 돌때까지 기달려봄(자동재접속유지됨)
                            {
                                ConnectionStates[lineNumber] = false;
                                UpdatePlcStatus(lineNumber, "연결 끊김", Colors.Gray);
                                MessageBox.Show($"라인 {lineNumber} PLC 연결이 끊어졌습니다!");
                                plc.Close();//완전히 끊음
                                ConnectionCheckTimers[lineNumber].Stop();//연결감시 타이머 정지
                                WatchJobCheckTimers[lineNumber].Stop();//동작감시 타이머 정지
                                iReconnetCount = 0;
                            }
                            iReconnetCount++;
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ConnectionStates[lineNumber] = false;
                    UpdatePlcStatus(lineNumber, "오류 발생", Colors.Red);
                    ConnectionCheckTimers[lineNumber].Stop();
                    WatchJobCheckTimers[lineNumber].Stop();
                    MessageBox.Show($"라인 {lineNumber} 연결 확인 중 오류 발생: {ex.Message}");
                });
            }
        }

        private void WatchJobCheckTimer_Tick(int lineNumber)//시간마다 체크(동작감시용)
        {
            try
            {
                var plc = PLCs[lineNumber];
                int watchdata = 0;
                int watchret = plc.GetDevice("M8164", out watchdata);//완료신호

                Application.Current.Dispatcher.Invoke(async () =>
                {
                    if (watchdata == 1)
                    {
                        string currentAction = "알 수 없는 동작";
                        if (CurrentActions.ContainsKey(lineNumber))
                        {
                            currentAction = CurrentActions[lineNumber];
                        }
                        WatchJobCheckTimers[lineNumber].Stop();
                        MessageBox.Show($"라인 {lineNumber}의 {currentAction} 실행 완료");
                        plc.SetDevice("d100", 0);
                        plc.SetDevice("M1", 0);
                        plc.SetDevice("M2", 0);
                        plc.SetDevice("M3", 0);
                        UpdatePlcStatus(lineNumber, "동작 완료", Colors.LawnGreen);

                        // 동작 완료 로그
                        string actionDetail = $"라인: {lineNumber}, 동작: {currentAction} 완료";
                        await _logService.SaveLogAsync(App.CurrentUser.UserId, "PLC 동작 완료", actionDetail);
                    }
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    WatchJobCheckTimers[lineNumber].Stop();
                    if (CurrentActions.ContainsKey(lineNumber))//오류시에 동작저장삭제
                    {
                        CurrentActions.Remove(lineNumber);
                    }
                    MessageBox.Show($"라인 {lineNumber} 동작 감시 중 오류 발생: {ex.Message}");
                });
            }
        }

        private void UpdatePlcStatus(int lineNumber, string status, Color color)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var plc = PlcStatuses[lineNumber - 1];
                if (plc != null)
                {
                    plc.Status = status;
                    plc.StatusColor = new SolidColorBrush(color);
                }
            });
        }

        // PLC 연결 메서드
        public void ConnectPlc(PlcStatusModel plc, int lineNumber)
        {
            try
            {
                var plcInstance = PLCs[lineNumber];
                plcInstance.ActLogicalStationNumber = PlcStationNumbers[lineNumber];
                int ret = plcInstance.Open();

                if (ret == 0)
                {
                    StartConnectionMonitoring(lineNumber);
                    OperationStatus = "연결됨";  // 연결 상태 업데이트
                    UpdatePlcStatus(lineNumber, "연결됨", Colors.Green);  // 상태 업데이트
                    MessageBox.Show($"라인 {lineNumber} PLC 연결 성공");
                }
                else
                {
                    // 연결 실패시 타이머 중지
                    ConnectionCheckTimers[lineNumber].Stop();
                    UpdatePlcStatus(lineNumber, "연결 실패", Colors.Red);
                    OperationStatus = "연결 실패";  // 연결 실패 상태 업데이트
                    MessageBox.Show($"라인 {lineNumber} PLC 연결 실패\n에러코드: 0x{ret:x8} [HEX]");
                }
            }
            catch (Exception ex)
            {
                // 오류 발생시 타이머 중지
                ConnectionCheckTimers[lineNumber].Stop();
                UpdatePlcStatus(lineNumber, "오류 발생", Colors.Red);
                OperationStatus = "오류 발생";  // 오류 상태 업데이트
                MessageBox.Show($"라인 {lineNumber} 연결 중 오류 발생: {ex.Message}");
            }
        }

        // PLC 연결 해제 메서드
        public void DisconnectPlc(PlcStatusModel plc, int lineNumber)
        {
            try
            {
                ConnectionCheckTimers[lineNumber].Stop();//연결 감시 타이머 중지
                WatchJobCheckTimers[lineNumber].Stop();//동작 감시 타이머 중지
                PLCs[lineNumber].FreeDeviceStatus();
                PLCs[lineNumber].Close();
                ConnectionStates[lineNumber] = false;
                UpdatePlcStatus(lineNumber, "연결 끊김", Colors.Gray);
                OperationStatus = "연결 끊김";  // 연결 해제 상태 업데이트
                MessageBox.Show($"라인 {lineNumber} PLC 연결 해제 완료");
            }
            catch (Exception ex)
            {
                OperationStatus = "연결 해제 오류";  // 해제 오류 상태 업데이트
                MessageBox.Show($"라인 {lineNumber} 연결 해제 중 오류 발생: {ex.Message}");
            }
        }

        // PLC 점검 메서드
        public void InspectPlc(PlcStatusModel plc, int lineNumber)
        {
            OperationStatus = "점검 중";  // 점검 중 상태 업데이트
            UpdatePlcStatus(lineNumber, "점검 중", Colors.Cyan);

            // PLC 점검 로직 구현
            // 예시로 임시 메시지 출력
            MessageBox.Show($"라인 {lineNumber} PLC 점검 중...");

            // 점검 완료 후 상태 업데이트
            //UpdatePlcStatus(lineNumber, "점검 완료", Colors.Green);
            //OperationStatus = "점검 완료";  // 점검 완료 상태 업데이트
        }

        // PLC 세부 정보 보기 명령 처리
        private void ShowPlcDetails(PlcStatusModel plc)
        {

            // 상세 정보를 처리하는 로직 추가
            MessageBox.Show($"PLC 상세 정보: {plc.Name} 상태는 {plc.Status}");
        }

        // PropertyChanged 호출
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void PLCCleanup()
        {
            // PLC 리소스 정리 코드
            foreach (var timer in ConnectionCheckTimers.Values)
            {
                timer.Stop();
            }

            foreach (var kvp in PLCs)
            {
                try
                {
                    kvp.Value.FreeDeviceStatus();
                    kvp.Value.Close();
                }
                catch { }
            }
        }


    }
}