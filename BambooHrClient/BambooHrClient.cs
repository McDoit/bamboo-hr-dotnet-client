using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using BambooHrClient.Models;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Serializers.Xml;
using System.Security.Cryptography;
using RestSharp.Serializers.NewtonsoftJson;
using Newtonsoft.Json.Linq;

namespace BambooHrClient
{
    public interface IBambooHrClient
    {
        Task<List<BambooHrEmployee>> GetEmployees(bool onlyCurrent = true);

        Task<List<Dictionary<string, string>>> GetTabularData(string employeeId, BambooHrTableType tableType);

        Task<List<BambooHrTimeOffRequest>> GetTimeOffRequests(int employeeId);
        Task<BambooHrTimeOffRequest> GetTimeOffRequest(int timeOffRequestId);
        Task<int> CreateTimeOffRequest(int employeeId, int timeOffTypeId, DateTime startDate, DateTime endDate, bool startHalfDay = false, bool endHalfDay = false, string comment = null, List<DateTime> holidays = null, int? previousTimeOffRequestId = null);
        Task<bool> CancelTimeOffRequest(int timeOffRequestId, string reason = null);
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

        Task<BambooHrEmployeeChangedInfo[]> GetLastChangedInfo(DateTime lastChanged, string type = "");
        Task<BambooHrReport<T>> GetReport<T>(int reportId);
    }

    public class BambooHrClient : IBambooHrClient
    {
        private readonly string _createRequestFormat = @"<request>
    <timeOffTypeId>{0}</timeOffTypeId>
    <start>{1}</start>
    <end>{2}</end>
    <dates>
        {3}
    </dates>
    <status>approved</status>
    <notes>
        <note from=""employee"">{4}</note>
    </notes>
</request>";

        private readonly string _cancelTimeOffRequestXml = @"<request>
    <status>cancelled</status>
    <note>Request cancelled via API.</note>
</request>";

        private readonly string _replaceRequestFormat = @"<request>
    <timeOffTypeId>{0}</timeOffTypeId>
    <start>{1}</start>
    <end>{2}</end>
    <dates>
        {3}
    </dates>
    <status>approved</status>
    <notes>
        <note from=""employee"">{4}</note>
    </notes>
    <previousRequest>{5}</previousRequest>
</request>";

        private readonly string _historyEntryRequestFormat = @"<history>
    <date>{0}</date>
    <eventType>used</eventType>
    <timeOffRequestId>{1}</timeOffRequestId>  
    <note>{2}</note>
</history>";

        private readonly IRestClient _iRestClient;

        public BambooHrClient()
        {
            var options = new RestClientOptions(Config.BambooApiUrl)
            {
                Authenticator = new HttpBasicAuthenticator(Config.BambooApiKey, "x")
            };

            _iRestClient = new RestClient(options, configureSerialization: s => s.UseXmlSerializer().UseNewtonsoftJson());
        }

        public BambooHrClient(IRestClient iRestClient)
        {
            _iRestClient = iRestClient;
        }

        public Task<List<Dictionary<string, string>>> GetTabularData(string employeeId, BambooHrTableType tableType)
        {
            var url = string.Format("/employees/{0}/tables/{1}/", employeeId, tableType.ToString().LowerCaseFirstLetter());

            var request = GetNewRestRequest(url, Method.Get, true);

            return GetDataResponse<List<Dictionary<string, string>>>(request);
        }

        #region Employees

        public Task<List<BambooHrEmployee>> GetEmployees(bool onlyCurrent = true)
        {
            var onlyCurrentUrlParam = onlyCurrent ? "" : "&onlyCurrent=false"; // Ignores EffectiveDate for changes, which is needed to get Dept/Div for new employees
            var url = "/reports/custom?format=json" + onlyCurrentUrlParam;
            var xml = GenerateUserReportRequestXml();

            var request = GetNewRestRequest(url, Method.Post, true);

            request.AddParameter("text/xml", xml, ParameterType.RequestBody);

            return GetDataResponse<List<BambooHrEmployee>>(request);
        }

        public async Task<string> AddEmployee(BambooHrEmployee bambooHrEmployee)
        {
            if (string.IsNullOrWhiteSpace(bambooHrEmployee.FirstName))
            {
                throw new Exception("First name is required.");
            }

            if (string.IsNullOrWhiteSpace(bambooHrEmployee.LastName))
            {
                throw new Exception("Lastname is required.");
            }

            var url = "/employees/";

            var request = GetNewRestRequest(url, Method.Post, false);

            var xml = bambooHrEmployee.ToXml();

            request.AddParameter("text/xml", xml, ParameterType.RequestBody);

            RestResponse response;

            try
            {
                response = await _iRestClient.ExecuteAsync(request);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error executing Bamboo request to {url} to add employee '{bambooHrEmployee.FirstName} {bambooHrEmployee.LastName}'", ex);
            }

            if (response.ErrorException != null)
                throw new Exception($"Error executing Bamboo request to {url} to add employee '{bambooHrEmployee.FirstName} {bambooHrEmployee.LastName}'", response.ErrorException);

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                throw new Exception($"There is already an employee with the email address {bambooHrEmployee.WorkEmail}.");
            }

            if (response.StatusCode == HttpStatusCode.Created)
            {
                var location = response.Headers.Single(h => h.Name == "Location").Value.ToString();
                var id = Regex.Match(location, @"\d+$").Value;
                bambooHrEmployee.Id = int.Parse(id);

                if (!string.IsNullOrWhiteSpace(location))
                {
                    return location;
                }

                throw new Exception("Bamboo Response does not contain Employee");
            }

            throw new Exception($"Bamboo Response threw error code {response.StatusCode} ({response.StatusDescription}) {response.GetBambooHrErrorMessage()} in {nameof(AddEmployee)}");
        }

        public async Task<BambooHrEmployee> GetEmployeeOld(int employeeId, params string[] fieldNames)
        {
            var url = "/employees/" + employeeId;

            var request = GetNewRestRequest(url, Method.Get, true);

            if(fieldNames?.Any() == true)
            {
                request.AddQueryParameter("fields", String.Join(",", fieldNames));
            }

            RestResponse<BambooHrEmployee> response;

            try
            {
                response = await _iRestClient.ExecuteAsync<BambooHrEmployee>(request);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0} for employee ID {1}", url, employeeId), ex);
            }

            if (response.ErrorException != null)
                throw new Exception(string.Format("Error executing Bamboo request to {0} for employee ID {1}", url, employeeId), response.ErrorException);

            if (string.IsNullOrWhiteSpace(response.Content))
                throw new Exception(string.Format("Empty Response to Request from BambooHR, Code: {0} and employee id {1}", response.StatusCode, employeeId));

            if (response.StatusCode == HttpStatusCode.OK)
            {
                //var raw = response.Content.Replace("Date\":\"0000-00-00\"", "Date\":null").RemoveTroublesomeCharacters();
                //var package = raw.FromJson<BambooHrEmployee>();

                if (response.Data != null)
                {
                    return response.Data;
                }

                throw new Exception("Bamboo Response does not contain Employee");
            }

            throw new Exception($"Bamboo Response threw error code {response.StatusCode} ({response.StatusDescription}) {response.GetBambooHrErrorMessage()} in {nameof(GetEmployee)}");
        }


        public async Task<T> GetDataResponse<T>(RestRequest restRequest)
        {
            RestResponse<T> response;

            try
            {
                response = await _iRestClient.ExecuteAsync<T>(restRequest);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0}", restRequest.Resource), ex);
            }

            if (response.ErrorException != null)
                throw new Exception(string.Format("Error executing Bamboo request to {0}", restRequest.Resource), response.ErrorException);

            if (string.IsNullOrWhiteSpace(response.Content))
                throw new Exception(string.Format("Empty Response to Request from BambooHR, Path: '{0}' gave code: '{1}'", restRequest.Resource, response.StatusCode));

            if (response.StatusCode == HttpStatusCode.OK)
            {
                if (response.Data != null)
                {
                    return response.Data;
                }

                throw new Exception("Bamboo Response does not contain " + typeof(T).Name);
            }

            throw new Exception($"Bamboo Response threw error code {response.StatusCode} ({response.StatusDescription}) {response.GetBambooHrErrorMessage()} at {restRequest.Resource}");
        }

        public Task<BambooHrEmployee> GetEmployee(int employeeId, params string[] fieldNames)
        {
            var url = "/employees/" + employeeId;

            var request = GetNewRestRequest(url, Method.Get, true);

            if (fieldNames?.Any() == true)
            {
                request.AddQueryParameter("fields", String.Join(",", fieldNames));
            }

            return GetDataResponse<BambooHrEmployee>(request);
        }

        public Task<BambooHrReport<T>> GetReport<T>(int reportId)
        {
            var url = "/reports/" + reportId;

            var request = GetNewRestRequest(url, Method.Get, true);

            request.AddQueryParameter("format", "JSON");

            return GetDataResponse<BambooHrReport<T>>(request);
        }

        public async Task<bool> UpdateEmployee(BambooHrEmployee bambooHrEmployee)
        {
            if (bambooHrEmployee.Id <= 0)
            {
                throw new Exception("ID is required.");
            }

            var url = $"/employees/{bambooHrEmployee.Id}";

            var request = GetNewRestRequest(url, Method.Post, false);

            var xml = bambooHrEmployee.ToXml();

            request.AddParameter("text/xml", xml, ParameterType.RequestBody);

            RestResponse response;

            try
            {
                response = await _iRestClient.ExecuteAsync(request);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error executing Bamboo request to {url} to update employee '{bambooHrEmployee.FirstName} {bambooHrEmployee.LastName}'.", ex);
            }

            if (response.ErrorException != null)
                throw new Exception($"Error executing Bamboo request to {url} to update employee '{bambooHrEmployee.FirstName} {bambooHrEmployee.LastName}'.", response.ErrorException);

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                throw new Exception($"Bad XML trying to update employee with ID {bambooHrEmployee.Id}.");
            }
            else if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new Exception($"Either you don't have permissions to update the employee, or none of the requested fields can be updated for employee ID {bambooHrEmployee.Id}.");
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new Exception($"Employee not found with the ID {bambooHrEmployee.Id}.");
            }
            else if (response.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }

            throw new Exception($"Bamboo Response threw error code {response.StatusCode} ({response.StatusDescription}) {response.GetBambooHrErrorMessage()} in {nameof(UpdateEmployee)}");
        }

        #endregion

        #region Photos

        public async Task<Byte[]> GetEmployeePhoto(int employeeId, string size = "small")
        {
            var url = string.Format("/employees/{0}/photo/{1}", employeeId, size);

            var request = GetNewRestRequest(url, Method.Get, true);

            RestResponse response;

            try
            {
                response = await _iRestClient.ExecuteAsync(request);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0} for employee ID {1}", url, employeeId), ex);
            }

            if (response.ErrorException != null)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0} for employee ID {1}", url, employeeId), response.ErrorException);
            }

            if (string.IsNullOrWhiteSpace(response.Content))
            {
                throw new Exception(string.Format("Empty Response to Request from BambooHR, Code: {0} and employee ID {1}", response.StatusCode, employeeId));
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var fileData = response.RawBytes;

                if (fileData != null)
                {
                    return fileData;
                }

                throw new Exception("Bamboo Response does not contain file data");
            }

            throw new Exception($"Bamboo Response threw error code {response.StatusCode} ({response.StatusDescription}) {response.GetBambooHrErrorMessage()} in {nameof(GetEmployeePhoto)}");
        }

        public string GetEmployeePhotoUrl(string employeeEmail)
        {
            var hashedEmail = Hash(employeeEmail);
            var url = string.Format(Config.BambooCompanyUrl + "/employees/photos/?h={0}", hashedEmail);

            return url;
        }

        /// <summary>
        /// The width and height of the photo must be the same number of pixels.
        /// The API user must have photo uploading permission.
        /// The source photo must be a jpg, gif, or png.
        /// The photo file may not be larger than 20MB.
        /// </summary>
        /// <param name="employeeId"></param>
        /// <param name="binaryData"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public async Task<bool> UploadEmployeePhoto(int employeeId, byte[] binaryData, string fileName)
        {
            var url = $"/employees/{employeeId}/photo";
            var request = GetNewRestRequest(url, Method.Post, true);

            request.AddFile("file", binaryData, fileName);
            request.AddHeader("Content-Type", "multipart/form-data");

            RestResponse response;

            try
            {
                response = await _iRestClient.ExecuteAsync(request);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0} for employee ID {1}", url, employeeId), ex);
            }

            if (response.ErrorException != null)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0} for employee ID {1}", url, employeeId), response.ErrorException);
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                throw new Exception($"Employee {employeeId} doesn't exist.");
            }
            else if (response.StatusCode == HttpStatusCode.RequestEntityTooLarge)
            {
                throw new Exception($"Employee {employeeId} photo file size too big, max size is 20MB.");
            }
            else if (response.StatusCode == HttpStatusCode.UnsupportedMediaType)
            {
                throw new Exception($"Employee {employeeId} photo file not in a supported file format or width doesn't match the height.");
            }
            else if (response.StatusCode == HttpStatusCode.Created)
            {
                return true;
            }

            throw new Exception($"Bamboo Response threw error code {response.StatusCode} ({response.StatusDescription}) {response.GetBambooHrErrorMessage()} in {nameof(UploadEmployeePhoto)}");
        }

        /// <summary>
        /// Mostly from Stack Overflow post: http://stackoverflow.com/a/24031467/57698
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private string Hash(string input)
        {
            var asciiBytes = Encoding.ASCII.GetBytes(input.Trim().ToLower());
            var hashedBytes = MD5.Create().ComputeHash(asciiBytes);
            var hashedString = BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();

            return hashedString;
        }

        #endregion

        /// <summary>
        /// Creates an approved Time Off Request in BambooHR.  Optionally, you can specify half days which reduces the respective day to 4 hours, comments, a list of holidays to skip, and a previous Time Off Request ID to supersede.
        /// </summary>
        /// <param name="employeeId"></param>
        /// <param name="timeOffTypeId"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="startHalfDay"></param>
        /// <param name="endHalfDay"></param>
        /// <param name="comment"></param>
        /// <param name="holidays">Holidays that apply to the supplied date range.</param>
        /// <param name="previousTimeOffRequestId"></param>
        /// <returns></returns>
        public async Task<int> CreateTimeOffRequest(int employeeId, int timeOffTypeId, DateTime startDate, DateTime endDate, bool startHalfDay = false, bool endHalfDay = false, string comment = null, List<DateTime> holidays = null, int? previousTimeOffRequestId = null)
        {
            var url = string.Format("/employees/{0}/time_off/request", employeeId);

            var request = GetNewRestRequest(url, Method.Put, false);

            var datesXml = GetDatesXml(startDate, endDate, startHalfDay, endHalfDay, holidays);

            string requestBody;

            if (previousTimeOffRequestId.HasValue)
            {
                requestBody = string.Format(_replaceRequestFormat, timeOffTypeId, startDate.ToString(Constants.BambooHrDateFormat), endDate.ToString(Constants.BambooHrDateFormat), datesXml, XmlEscape(comment), previousTimeOffRequestId.Value);
            }
            else
            {
                requestBody = string.Format(_createRequestFormat, timeOffTypeId, startDate.ToString(Constants.BambooHrDateFormat), endDate.ToString(Constants.BambooHrDateFormat), datesXml, XmlEscape(comment));
            }

            request.AddParameter("text/xml", requestBody, ParameterType.RequestBody);

            RestResponse response;

            try
            {
                response = await _iRestClient.ExecuteAsync(request);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0} for employee ID {1}", url, employeeId), ex);
            }

            if (response.ErrorException != null)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0} for employee ID {1}", url, employeeId), response.ErrorException);
            }

            if (response.StatusCode == HttpStatusCode.Created)
            {
                var location = response.Headers.Single(h => h.Name == "Location").Value.ToString();
                var id = Regex.Match(location, @"\d+$").Value;
                var timeOffRequestId = int.Parse(id);

                // If the first requested day is in the past, then we need to add a history entry for it.
                if (startDate < DateTime.Today)
                {
                    await AddTimeOffRequestHistoryEntry(employeeId, timeOffRequestId, startDate);
                }

                return timeOffRequestId;
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new Exception($"Can't create Time Off Request in {nameof(CreateTimeOffRequest)}, Employee ID {employeeId} not found.");
            }

            throw new Exception($"Bamboo Response threw error code {response.StatusCode} ({response.StatusDescription}) {response.GetBambooHrErrorMessage()} in {nameof(CreateTimeOffRequest)}");
        }

        private async Task<bool> AddTimeOffRequestHistoryEntry(int employeeId, int timeOffRequestId, DateTime date)
        {
            var url = string.Format("/employees/{0}/time_off/history/", employeeId);
            var note = "Automatically created by OOO tool because request is in the past.";
            var historyEntryRequestFormat = string.Format(_historyEntryRequestFormat, date.ToString(Constants.BambooHrDateFormat), timeOffRequestId, note);

            var request = GetNewRestRequest(url, Method.Put, false);

            request.AddParameter("text/xml", historyEntryRequestFormat, ParameterType.RequestBody);

            RestResponse response;

            try
            {
                response = await _iRestClient.ExecuteAsync(request);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0} for employee ID {1} and time off request ID {2}", url, employeeId, timeOffRequestId), ex);
            }

            if (response.ErrorException != null)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0} for employee ID {1} and time off request ID {2}", url, employeeId, timeOffRequestId), response.ErrorException);
            }

            if (response.StatusCode == HttpStatusCode.Created)
            {
                return true;
            }

            throw new Exception($"Bamboo Response threw error code {response.StatusCode} ({response.StatusDescription}) {response.GetBambooHrErrorMessage()} in {nameof(AddTimeOffRequestHistoryEntry)}");
        }

        public Task<List<BambooHrTimeOffRequest>> GetTimeOffRequests(int employeeId)
        {
            const string url = "/time_off/requests/";

            var request = GetNewRestRequest(url, Method.Get, true);

            request.AddParameter("employeeId", employeeId, ParameterType.QueryString);

            return GetDataResponse<List<BambooHrTimeOffRequest>>(request);
        }

        public async Task<BambooHrTimeOffRequest> GetTimeOffRequest(int timeOffRequestId)
        {
            const string url = "/time_off/requests/";

            var request = GetNewRestRequest(url, Method.Get, true);

            request.AddParameter("id", timeOffRequestId, ParameterType.QueryString);

            var data = await GetDataResponse<List<BambooHrTimeOffRequest>>(request);

            if (data != null && data.Any())
            {
                return data.SingleOrDefault();
            }

            throw new Exception("Bamboo Response does not contain data.");
        }

        public async Task<bool> CancelTimeOffRequest(int timeOffRequestId, string reason = null)
        {
            var url = string.Format("time_off/requests/{0}/status/", timeOffRequestId);

            var request = GetNewRestRequest(url, Method.Put, true);

            request.AddParameter("text/xml", _cancelTimeOffRequestXml, ParameterType.RequestBody);

            RestResponse response;

            try
            {
                response = await _iRestClient.ExecuteAsync(request);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0} for time off request ID {1}", url, timeOffRequestId), ex);
            }

            if (response.ErrorException != null)
            {
                throw new Exception(string.Format("Error executing Bamboo request to {0} for time off request ID {1}", url, timeOffRequestId), response.ErrorException);
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }

            throw new Exception($"Bamboo Response threw error code {response.StatusCode} ({response.StatusDescription}) {response.GetBambooHrErrorMessage()} in {nameof(CancelTimeOffRequest)}");
        }

        public Task<List<BambooHrAssignedTimeOffPolicy>> GetAssignedTimeOffPolicies(int employeeId)
        {
            var url = string.Format("/employees/{0}/time_off/policies/", employeeId);

            var request = GetNewRestRequest(url, Method.Get, true);

            return GetDataResponse<List<BambooHrAssignedTimeOffPolicy>>(request);
        }

        public Task<List<BambooHrEstimate>> GetFutureTimeOffBalanceEstimates(int employeeId, DateTime? endDate = null)
        {
            var url = string.Format("/employees/{0}/time_off/calculator/", employeeId);

            var request = GetNewRestRequest(url, Method.Get, true);

            if (endDate.HasValue)
                request.AddParameter("end", endDate.Value.ToString(Constants.BambooHrDateFormat), ParameterType.QueryString);

            return GetDataResponse<List<BambooHrEstimate>>(request);
        }

        public Task<List<BambooHrWhosOutInfo>> GetWhosOut(DateTime? startDate = null, DateTime? endDate = null)
        {
            const string url = "/time_off/whos_out/";

            var request = GetNewRestRequest(url, Method.Get, true);

            if (startDate.HasValue)
                request.AddParameter("start", startDate.Value.ToString(Constants.BambooHrDateFormat), ParameterType.QueryString);

            if (endDate.HasValue)
                request.AddParameter("end", endDate.Value.ToString(Constants.BambooHrDateFormat), ParameterType.QueryString);

            return GetDataResponse<List<BambooHrWhosOutInfo>>(request);
        }

        // See inner todo regarding this pragma
#pragma warning disable 1998
        public Task<List<BambooHrHoliday>> GetHolidays(DateTime startDate, DateTime endDate)
        {
            const string url = "/time_off/holidays/";

            var request = GetNewRestRequest(url, Method.Get, true);

            request.AddParameter("start", startDate.ToString(Constants.BambooHrDateFormat), ParameterType.QueryString);
            request.AddParameter("end", endDate.ToString(Constants.BambooHrDateFormat), ParameterType.QueryString);
            
            return GetDataResponse<List<BambooHrHoliday>>(request);
        }
#pragma warning restore 1998

        public string GetDatesXml(DateTime startDate, DateTime endDate, bool startHalfDay, bool endHalfDay, List<DateTime> holidays)
        {
            var dates = new StringBuilder();
            var dateFormat = @"<date ymd=""{0}"" amount=""{1}"" />";
            var dateHours = GetDateHours(startDate, endDate, startHalfDay, endHalfDay, holidays);

            foreach (var kvp in dateHours)
            {
                dates.AppendFormat(dateFormat, kvp.Key.ToString(Constants.BambooHrDateFormat), kvp.Value);
            }

            return dates.ToString();
        }

        public Task<BambooHrField[]> GetFields()
        {
            const string url = "/meta/fields/";

            var request = GetNewRestRequest(url, Method.Get, true);

            return GetDataResponse<BambooHrField[]>(request);
        }

        public Task<BambooHrTable[]> GetTabularFields()
        {
            const string url = "/meta/tables/";

            var request = GetNewRestRequest(url, Method.Get, true);

            return GetDataResponse<BambooHrTable[]>(request);
        }

        public Task<List<BambooHrListField>> GetListFieldDetails()
        {
            const string url = "/meta/lists/";

            var request = GetNewRestRequest(url, Method.Get, true);

            return GetDataResponse<List<BambooHrListField>>(request);
        }

        public async Task<BambooHrListField> AddOrUpdateListValues(int listId, List<BambooHrListFieldOption> values)
        {
            var url = $"/meta/lists/{listId}";
            var request = GetNewRestRequest(url, Method.Put, false);

            //request.XmlSerializer = new BambooHrListFieldOptionSerializer();
            request.AddXmlBody(values);

            RestResponse<BambooHrListField> response;

            try
            {
                response = await _iRestClient.ExecuteAsync<BambooHrListField>(request);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error executing Bamboo request to {url}", ex);
            }

            if (response.ErrorException != null)
                throw new Exception($"Error executing Bamboo request to {url}", response.ErrorException);

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                throw new Exception($"Bad XML trying to add or update list value in list with ID {listId}.");
            }
            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new Exception($"The list with ID {listId} is not editable.");
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new Exception($"List or option not found when trying to add or update list value for list with ID {listId}.");
            }
            else if (response.StatusCode == HttpStatusCode.Conflict)
            {
                throw new Exception($"Can't create duplicate list value in list with ID {listId}.");
            }
            else if (response.StatusCode == HttpStatusCode.OK)
            {
                if (response.Data != null)
                    return response.Data;

                throw new Exception("Bamboo Response does not contain data");
            }

            throw new Exception($"Bamboo Response threw error code {response.StatusCode} ({response.StatusDescription}) {response.GetBambooHrErrorMessage()} in {nameof(AddOrUpdateListValues)}");
        }

        public Task<BambooHrTimeOffTypeInfo> GetTimeOffTypes(string mode = "")
        {
            const string url = "/meta/time_off/types/";

            var request = GetNewRestRequest(url, Method.Get, true);

            if (!string.IsNullOrWhiteSpace(mode))
                request.AddParameter("mode", mode, ParameterType.GetOrPost);

            return GetDataResponse<BambooHrTimeOffTypeInfo>(request);
        }

        public Task<BambooHrTimeOffPolicy[]> GetTimeOffPolicies()
        {
            const string url = "/meta/time_off/policies/";

            var request = GetNewRestRequest(url, Method.Get, true);

            return GetDataResponse<BambooHrTimeOffPolicy[]>(request);
        }

        public Task<BambooHrUser[]> GetUsers()
        {
            const string url = "/employees/directory";

            var request = GetNewRestRequest(url, Method.Get, true);

            return GetDataResponse<BambooHrUser[]>(request);
        }

        public Task<BambooHrEmployeeChangedInfo[]> GetLastChangedInfo(DateTime lastChanged, string type = "")
        {
            const string url = "/employees/changed/";

            var request = GetNewRestRequest(url, Method.Get, true);

            request.AddParameter("since", lastChanged.ToString("yyyy-MM-ddTHH:mm:sszzz"), ParameterType.GetOrPost);

            if (!string.IsNullOrWhiteSpace(type))
            {
                request.AddParameter("type", type, ParameterType.GetOrPost);
            }

            return GetDataResponse<BambooHrEmployeeChangedInfo[]>(request);
        }

        private Dictionary<DateTime, int> GetDateHours(DateTime startDate, DateTime endDate, bool startHalfDay, bool endHalfDay, List<DateTime> holidays)
        {
            var dateHours = new Dictionary<DateTime, int>();

            var dateIterator = startDate.Date;

            while (dateIterator <= endDate.Date)
            {
                if (holidays != null && holidays.Any(h => h.Date == dateIterator.Date))
                {
                    dateHours.Add(dateIterator.Date, 0);
                }
                else if (dateIterator.DayOfWeek == DayOfWeek.Saturday || dateIterator.DayOfWeek == DayOfWeek.Sunday)
                {
                    dateHours.Add(dateIterator.Date, 0);
                }
                else if (dateIterator == startDate.Date && startHalfDay)
                {
                    dateHours.Add(dateIterator.Date, 4);
                }
                else if (dateIterator == endDate.Date && endHalfDay)
                {
                    dateHours.Add(dateIterator.Date, 4);
                }
                else
                {
                    dateHours.Add(dateIterator.Date, 8);
                }

                dateIterator = dateIterator.AddDays(1);
            }

            return dateHours;
        }

        private RestRequest GetNewRestRequest(string url, Method method, bool sendingJson, bool binary = false)
        {
            var request = new RestRequest(url, method);

            if (!binary)
            {
                request.AddHeader("Accept", "application/json");
                request.AddHeader("Encoding", "utf-8");
            }

            if (sendingJson)
            {
                request.AddHeader("Content-Type", "application/json");
                request.AddParameter("format", "JSON", ParameterType.QueryString);
            }
            else
            {
                request.AddHeader("Content-Type", "text/xml");
            }

            request.OnBeforeDeserialization = bs => bs.Content = bs.Content.Replace("\":\"0000-00-00\"", "\":null").RemoveTroublesomeCharacters();
            request.OnAfterRequest = ar => 
            {
                var contentString = ar?.Content.ReadAsStringAsync().ContinueWith(s => Console.WriteLine(s.Result));
                
                return new ValueTask(contentString);
            };


            return request;
        }

        private string GenerateUserReportRequestXml()
        {
            const string xml = @"<report>
  <title></title>{0}
  <fields>
    <field id=""id"" />

    <field id=""lastChanged"" />

    <field id=""status"" />

    <field id=""firstName"" />
    <field id=""middleName"" />
    <field id=""lastName"" />
    <field id=""nickname"" />
    <field id=""displayName"" />
    <field id=""gender"" />
    <field id=""DateOfBirth"" />
    <field id=""Age"" />

    <field id=""address1"" />
    <field id=""address2"" />
    <field id=""city"" />
    <field id=""state"" />
    <field id=""country"" />
    <field id=""zipCode"" />

    <field id=""homeEmail"" />
    <field id=""homePhone"" />
    <field id=""mobilePhone"" />

    <field id=""workEmail"" />
    <field id=""workPhone"" />
    <field id=""workPhoneExtension"" />
    <field id=""workPhonePlusExtension"" />

    <field id=""jobTitle"" />
    <field id=""department"" />
    <field id=""division"" />
    <field id=""location"" />

    <field id=""terminationDate"" />

    <field id=""supervisorId"" />
    <field id=""supervisorEid"" />
  </fields> 
</report>";

            return xml;
        }

        private static string XmlEscape(string unescaped)
        {
            var doc = new XmlDocument();
            var node = doc.CreateElement("root");

            node.InnerText = unescaped;

            return node.InnerXml;
        }
    }
}
