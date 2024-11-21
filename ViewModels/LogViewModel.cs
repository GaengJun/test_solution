using Dapper;
using MES.Solution.Helpers;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;

namespace MES.Solution.ViewModels
{
    public class LogViewModel : INotifyPropertyChanged
    {
        private readonly string _connectionString;
        private DateTime _startDate = DateTime.Now.AddDays(1 - DateTime.Now.Day);//시작날짜
        private DateTime _endDate = DateTime.Today;

        public LogViewModel()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["MESDatabase"].ConnectionString;

            // 컬렉션 초기화
            ActivityLogs = new ObservableCollection<UserActivityLogModel>();

            // 명령 초기화
            SearchCommand = new RelayCommand(async () => await ExecuteSearch());
            ExportCommand = new RelayCommand(ExecuteExport);

            // 초기 데이터 로드
            _ = ExecuteSearch();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        // 컬렉션
        public ObservableCollection<UserActivityLogModel> ActivityLogs { get; }

        // 속성
        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                if (_startDate != value)
                {
                    _startDate = value;
                    OnPropertyChanged();
                    //날짜 강제선택
                    if (_startDate > EndDate)
                    {
                        EndDate = _startDate;
                    }
                }
            }
        }

        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                if (_endDate != value)
                {
                    _endDate = value;
                    OnPropertyChanged();
                    //날짜 강제선택
                    if (_endDate < StartDate)
                    {
                        StartDate = _endDate;
                    }
                }
            }
        }

        // 명령
        public ICommand SearchCommand { get; }
        public ICommand ExportCommand { get; }

        private async Task ExecuteSearch()
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    var sql = @"
SELECT 
    l.log_id as LogId,
    u.username as Username,
    l.action_type as ActionType,
    l.action_detail as ActionDetail,
    l.action_date as ActionDate
FROM dt_user_activity_log l
JOIN db_user u ON l.user_id = u.user_id
WHERE l.action_date BETWEEN @StartDate AND @EndDate
ORDER BY l.action_date DESC";

                    var parameters = new DynamicParameters();
                    parameters.Add("@StartDate", StartDate.Date);
                    parameters.Add("@EndDate", EndDate.Date.AddDays(1).AddSeconds(-1));

                    var result = await conn.QueryAsync<UserActivityLogModel>(sql, parameters);

                    ActivityLogs.Clear();
                    foreach (var log in result)
                    {
                        ActivityLogs.Add(log);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"검색 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteExport()
        {
            // TODO: 엑셀 내보내기 기능 구현
            MessageBox.Show("엑셀 내보내기 기능은 추후 구현 예정입니다.");
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Cleanup()
        {
            // 리소스 정리 필요시 여기에 구현
        }
    }

    public class UserActivityLogModel
    {
        public int LogId { get; set; }
        public string Username { get; set; }
        public string ActionType { get; set; }
        public string ActionDetail { get; set; }
        public DateTime ActionDate { get; set; }
    }
}

