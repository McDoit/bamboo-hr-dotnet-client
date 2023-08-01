#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BambooHrClient.Models;

namespace BambooHrClient
{
    public interface IBambooHrClient
    {
        Task<List<BambooHrEmployee>> GetEmployees(bool onlyCurrent = true);

        Task<List<Dictionary<string, string>>> GetTabularData(string employeeId, BambooHrTableType tableType);

        Task<List<BambooHrTimeOffRequest>> GetTimeOffRequests(int employeeId);
        Task<BambooHrTimeOffRequest> GetTimeOffRequest(int timeOffRequestId);
        Task<int> CreateTimeOffRequest(int employeeId, int timeOffTypeId, DateTime startDate, DateTime endDate, bool startHalfDay = false, bool endHalfDay = false, string? comment = null, List<DateTime> holidays = null, int? previousTimeOffRequestId = null);
        Task<bool> CancelTimeOffRequest(int timeOffRequestId, string? reason = null);
        Task<List<BambooHrAssignedTimeOffPolicy>> GetAssignedTimeOffPolicies(int employeeId);
        Task<List<BambooHrEstimate>> GetFutureTimeOffBalanceEstimates(int employeeId, DateTime? endDate = null);
        Task<List<BambooHrWhosOutInfo>> GetWhosOut(DateTime? startDate = null, DateTime? endDate = null);
        Task<List<BambooHrHoliday>> GetHolidays(DateTime startDate, DateTime endDate);

        Task<string> AddEmployee(BambooHrEmployee bambooHrEmployee);
        Task<BambooHrEmployee> GetEmployee(int employeeId, params string[] fieldNames);
        Task<bool> UpdateEmployee(BambooHrEmployee bambooHrEmployee);

        Task<Byte[]> GetEmployeePhoto(int employeeId, string size = "small");
        string GetEmployeePhotoUrl(string employeeEmail);
        Task<bool> UploadEmployeePhoto(int employeeId, byte[] binaryData, string fileName);

        Task<BambooHrField[]> GetFields();
        Task<BambooHrTable[]> GetTabularFields();
        Task<List<BambooHrListField>> GetListFieldDetails();
        Task<BambooHrListField> AddOrUpdateListValues(int listId, List<BambooHrListFieldOption> values);
        Task<BambooHrTimeOffTypeInfo> GetTimeOffTypes(string mode = "");
        Task<BambooHrTimeOffPolicy[]> GetTimeOffPolicies();
        Task<BambooHrUser[]> GetUsers();

        Task<BambooHrEmployeeChangedInfos> GetLastChangedInfos(DateTime lastChanged, string type = "");

        Task<BambooHrEmployeeChangedInfo[]> GetLastChangedInfo(DateTime lastChanged, string type = "");
        Task<BambooHrReport<T>> GetReport<T>(int reportId);

        Task<BambooHrUpdatedWebhook> GetWebhook(int id);
        Task<BambooHrWebhookList> GetWebhooks();
        Task<BambooHrNewWebhook> AddWebhook(BambooHrWebhook webhook);
        Task<BambooHrUpdatedWebhook> UpdateWebhook(BambooHrCreatedWebhook webhook);
        Task<bool> DeleteWebhook(int id);
        Task<BambooHrWebhookMonitorFieldList> GetWebhookMonitorFields();
    }
}
