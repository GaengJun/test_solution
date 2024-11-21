using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using Dapper;
using MES.Solution.Models;
using MES.Solution.Models.Equipment;
using MySql.Data.MySqlClient;

namespace MES.Solution.Services
{
    public class EquipmentMaintenanceScheduleService
    {
        private readonly string _connectionString;

        public EquipmentMaintenanceScheduleService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["MESDatabase"].ConnectionString;
        }

        public async Task<IEnumerable<MaintenanceScheduleModel>> GetSchedules()
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                var sql = @"
                    SELECT 
                        equipment_code as EquipmentCode,
                        production_line as ProductionLine,
                        equipment_company_name as EquipmentCompanyName,
                        equipment_contact_number as EquipmentContactNumber,
                        equipment_contact_person as EquipmentContactPerson,
                        inspection_date as InspectionDate,
                        inspection_frequency as InspectionFrequency,
                        temperature as Temperature,
                        humidity as Humidity,
                        employee_name as EmployeeName
                    FROM dt_facility_management
                    ORDER BY inspection_date DESC";

                return await conn.QueryAsync<MaintenanceScheduleModel>(sql);
            }
        }

        public async Task AddSchedule(MaintenanceScheduleModel schedule)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                var sql = @"
                    INSERT INTO dt_facility_management (
                        equipment_code,
                        production_line,
                        equipment_company_name,
                        equipment_contact_number,
                        equipment_contact_person,
                        inspection_date,
                        inspection_frequency,
                        temperature,
                        humidity,
                        employee_name
                    ) VALUES (
                        @EquipmentCode,
                        @ProductionLine,
                        @EquipmentCompanyName,
                        @EquipmentContactNumber,
                        @EquipmentContactPerson,
                        @InspectionDate,
                        @InspectionFrequency,
                        @Temperature,
                        @Humidity,
                        @EmployeeName
                    )";

                await conn.ExecuteAsync(sql, schedule);
            }
        }

        public async Task UpdateSchedule(MaintenanceScheduleModel schedule, string originalEquipmentCode)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                var sql = @"
                    UPDATE dt_facility_management 
                    SET 
                        equipment_code = @EquipmentCode,
                        production_line = @ProductionLine,
                        equipment_company_name = @EquipmentCompanyName,
                        equipment_contact_number = @EquipmentContactNumber,
                        equipment_contact_person = @EquipmentContactPerson,
                        inspection_date = @InspectionDate,
                        inspection_frequency = @InspectionFrequency,
                        temperature = @Temperature,
                        humidity = @Humidity,
                        employee_name = @EmployeeName
                    WHERE equipment_code = @OriginalEquipmentCode";

                var parameters = new DynamicParameters(schedule);
                parameters.Add("@OriginalEquipmentCode", originalEquipmentCode);

                await conn.ExecuteAsync(sql, parameters);
            }
        }

        public async Task DeleteSchedule(string equipmentCode)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                var sql = "DELETE FROM dt_facility_management WHERE equipment_code = @EquipmentCode";
                await conn.ExecuteAsync(sql, new { EquipmentCode = equipmentCode });
            }
        }

        public async Task DeleteSchedules(IEnumerable<string> equipmentCodes)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                var sql = "DELETE FROM dt_facility_management WHERE equipment_code IN @EquipmentCodes";
                await conn.ExecuteAsync(sql, new { EquipmentCodes = equipmentCodes });
            }
        }

        public async Task<bool> CheckEquipmentCodeExists(string equipmentCode, string currentEquipmentCode = null)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                var sql = "SELECT COUNT(*) FROM dt_facility_management WHERE equipment_code = @EquipmentCode";
                if (!string.IsNullOrEmpty(currentEquipmentCode))
                {
                    sql += " AND equipment_code != @CurrentEquipmentCode";
                }

                var parameters = new DynamicParameters();
                parameters.Add("@EquipmentCode", equipmentCode);
                if (!string.IsNullOrEmpty(currentEquipmentCode))
                {
                    parameters.Add("@CurrentEquipmentCode", currentEquipmentCode);
                }

                var count = await conn.ExecuteScalarAsync<int>(sql, parameters);
                return count > 0;
            }
        }

        public async Task<MaintenanceScheduleModel> GetScheduleByCode(string equipmentCode)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                var sql = @"
                    SELECT 
                        equipment_code as EquipmentCode,
                        production_line as ProductionLine,
                        equipment_company_name as EquipmentCompanyName,
                        equipment_contact_number as EquipmentContactNumber,
                        equipment_contact_person as EquipmentContactPerson,
                        inspection_date as InspectionDate,
                        inspection_frequency as InspectionFrequency,
                        temperature as Temperature,
                        humidity as Humidity,
                        employee_name as EmployeeName
                    FROM dt_facility_management
                    WHERE equipment_code = @EquipmentCode";

                return await conn.QueryFirstOrDefaultAsync<MaintenanceScheduleModel>(sql, new { EquipmentCode = equipmentCode });
            }
        }
    }
}