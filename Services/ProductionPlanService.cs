using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using Dapper;
using MES.Solution.Models;
using MySql.Data.MySqlClient;

namespace MES.Solution.Services
{
    public class ProductionPlanService
    {
        private readonly string _connectionString;

        public ProductionPlanService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["MESDatabase"].ConnectionString;
        }

        // 생산계획 목록 가져오기
        public async Task<IEnumerable<ProductionPlanModel>> GetProductionPlans(DateTime startDate, DateTime endDate, string productionLine, string productName, string status)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                var query = @"
                        SELECT 
                            pp.work_order_code AS PlanNumber,
                            pp.production_date AS PlanDate,
                            pp.production_line AS ProductionLine,
                            pp.product_code AS ProductCode,
                            p.product_name AS ProductName,
                            pp.order_quantity AS PlannedQuantity,
                            pp.production_quantity AS ProductionQuantity,
                            pp.work_shift AS WorkShift,
                            IF(pp.order_quantity > 0, 
                               (pp.production_quantity * 100 / pp.order_quantity), 
                               0) AS AchievementRate,
                            pp.process_status AS Status,
                            pp.remarks AS Remarks
                        FROM dt_production_plan pp
                        JOIN dt_product p ON pp.product_code = p.product_code
                        WHERE pp.production_date BETWEEN @StartDate AND @EndDate";

                var parameters = new DynamicParameters();
                parameters.Add("@StartDate", startDate.Date); // 시작일의 00:00:00
                parameters.Add("@EndDate", endDate.Date.AddDays(1).AddSeconds(-1)); // 종료일의 23:59:59


                if (!string.IsNullOrEmpty(productionLine) && productionLine != "전체")
                {
                    query += " AND pp.production_line = @ProductionLine";
                    parameters.Add("@ProductionLine", productionLine);
                }

                if (!string.IsNullOrEmpty(productName) && productName != "전체")
                {
                    query += " AND p.product_name = @ProductName";
                    parameters.Add("@ProductName", productName);
                }

                if (!string.IsNullOrEmpty(status) && status != "전체")
                {
                    query += " AND pp.process_status = @Status";
                    parameters.Add("@Status", status);
                }

                query += " ORDER BY pp.production_date DESC, pp.work_order_sequence";

                try
                {
                    return await conn.QueryAsync<ProductionPlanModel>(query, parameters);
                }
                catch (Exception ex)
                {
                    throw new Exception($"생산계획 조회 중 오류가 발생했습니다: {ex.Message}", ex);
                }
            }
        }

        // ProductionPlanService.cs에 추가
        public async Task UpdateProductionPlan(ProductionPlanModel plan)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                var sql = @"
            UPDATE dt_production_plan 
            SET production_date = @ProductionDate,
                production_line = @ProductionLine,
                product_code = @ProductCode,
                order_quantity = @OrderQuantity,
                work_shift = @WorkShift,
                remarks = @Remarks,
                employee_name = @EmployeeName
            WHERE work_order_code = @WorkOrderCode";

                await conn.ExecuteAsync(sql, new
                {
                    ProductionDate = plan.PlanDate,
                    ProductionLine = plan.ProductionLine,
                    ProductCode = plan.ProductCode,
                    OrderQuantity = plan.PlannedQuantity,
                    WorkShift = plan.WorkShift,
                    Remarks = plan.Remarks,
                    EmployeeName = App.CurrentUser.Username,
                    WorkOrderCode = plan.PlanNumber
                });
            }
        }

        // 생산계획 삭제
        public async Task DeleteProductionPlan(string planNumber)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                string query = "DELETE FROM dt_production_plan WHERE work_order_code = @PlanNumber";
                await conn.ExecuteAsync(query, new { PlanNumber = planNumber });
            }
        }

        // 생산라인 목록 가져오기
        public async Task<IEnumerable<string>> GetProductionLines()
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                string query = "SELECT DISTINCT production_line FROM dt_production_plan ORDER BY production_line";
                return await conn.QueryAsync<string>(query);
            }
        }

        // 제품 목록 가져오기
        public async Task<IEnumerable<string>> GetProducts()
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                string query = "SELECT DISTINCT product_name FROM dt_product ORDER BY product_name";
                return await conn.QueryAsync<string>(query);
            }
        }

        // 상태 목록 가져오기
        public async Task<IEnumerable<string>> GetStatuses()
        {
            return await Task.FromResult(new List<string> { "전체", "대기", "진행중", "완료", "지연" });
        }
    }
}
