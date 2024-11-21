using Dapper;
using MySql.Data.MySqlClient;
using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows;

namespace MES.Solution.Services
{
    public class LogService
    {
        private readonly string _connectionString;

        public LogService()
        {
            // 앱 설정에서 연결 문자열을 직접 읽어옵니다.
            _connectionString = ConfigurationManager.ConnectionStrings["MESDatabase"]?.ConnectionString;

            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new InvalidOperationException("Connection string is not properly set in app.config");
            }
        }

        // 로그 저장 메서드
        public async Task SaveLogAsync(int userId, string actionType, string actionDetail)
        {

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    // 연결 문자열에 비밀번호가 포함되어 있는지 확인
                    var sql = @"
                INSERT INTO dt_user_activity_log (user_id, action_type, action_detail, action_date) 
                VALUES (@UserId, @ActionType, @ActionDetail, NOW())";

                    await conn.ExecuteAsync(sql, new
                    {
                        UserId = userId,
                        ActionType = actionType,
                        ActionDetail = actionDetail
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"로그 저장 중 오류가 발생했습니다: {ex.Message}",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



    }
}
