using Newtonsoft.Json;
using RestSharp.Serializers;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using System.Xml.Serialization;
using static System.Net.Mime.MediaTypeNames;

namespace BambooHrClient.Models
{
    [XmlType(TypeName = "employee")]
    public class BambooHrEmployee
    {
        public int Id { get; set; }

        public DateTime? LastChanged { get; set; }

        public string Status { get; set; }

        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string Nickname { get; set; }
        public string PreferredName { get; set; }
        public string DisplayName { get; set; }
        public string Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int Age { get; set; }

        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string ZipCode { get; set; }

        public string HomeEmail { get; set; }
        public string HomePhone { get; set; }
        public string MobilePhone { get; set; }

        public string WorkEmail { get; set; }
        public string WorkPhone { get; set; }
        public string WorkPhoneExtension { get; set; }
        public string WorkPhonePlusExtension { get; set; }

        public string JobTitle { get; set; }
        public string Department { get; set; }
        public string Location { get; set; }
        public string Division { get; set; }

        public DateTime HireDate { get; set; }
        public DateTime? TerminationDate { get; set; }

        public string Supervisor { get; set; }
        public string SupervisorId { get; set; }
        public int? SupervisorEid { get; set; }
        public string SupervisorEmail { get; set; }


        public string PaidPer { get; set; }
        public string PayChangeReason { get; set; }
        public string PayFrequency { get; set; }
        public int? PayGroupId { get; set; }
        public string PayRate { get; set; }
        public DateTime? PayRateEffectiveDate { get; set; }
        public string PaySchedule { get; set; }
        public int? PayScheduleId { get; set; }
        public string PayType { get; set; }

        public string CommissionAmount { get; set; }
        public string CommissionComment { get; set; }
        public DateTime? CommissionDate { get; set; }

        public string BonusAmount { get; set; }
        public string BonusComment { get; set; }
        public string BonusReason { get; set; }
        public DateTime? BonusDate { get; set; }


        public string LastFirst
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Nickname))
                    return LastName + ", " + Nickname;

                return LastName + ", " + FirstName;
            }
        }

        public string FirstLast
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Nickname))
                    return Nickname + " " + LastName;

                return FirstName + " " + LastName;
            }
        }

        public string ToXml()
        {
            var xElement = new XElement("employee");

            xElement.AddFieldValueIfNotNull("status", Status);

            xElement.AddFieldValueIfNotNull("firstName", FirstName);
            xElement.AddFieldValueIfNotNull("middleName", MiddleName);
            xElement.AddFieldValueIfNotNull("lastName", LastName);
            xElement.AddFieldValueIfNotNull("nickname", Nickname); //?
            xElement.AddFieldValueIfNotNull("displayName", DisplayName);
            xElement.AddFieldValueIfNotNull("gender", Gender);
            xElement.AddFieldValueIfNotNull("dateOfBirth", DateOfBirth);

            xElement.AddFieldValueIfNotNull("address1", Address1);
            xElement.AddFieldValueIfNotNull("address2", Address2);
            xElement.AddFieldValueIfNotNull("city", City);
            xElement.AddFieldValueIfNotNull("state", State);
            xElement.AddFieldValueIfNotNull("country", Country);
            xElement.AddFieldValueIfNotNull("zipCode", ZipCode);

            xElement.AddFieldValueIfNotNull("homeEmail", HomeEmail);
            xElement.AddFieldValueIfNotNull("homePhone", HomePhone);
            xElement.AddFieldValueIfNotNull("mobilePhone", MobilePhone);

            xElement.AddFieldValueIfNotNull("workEmail", WorkEmail);
            xElement.AddFieldValueIfNotNull("workPhone", WorkPhone);
            xElement.AddFieldValueIfNotNull("workPhoneExtension", WorkPhoneExtension);

            xElement.AddFieldValueIfNotNull("jobTitle", JobTitle);
            xElement.AddFieldValueIfNotNull("department", Department);
            xElement.AddFieldValueIfNotNull("location", Location);
            xElement.AddFieldValueIfNotNull("division", Division);

            xElement.AddFieldValueIfNotNull("terminationDate", TerminationDate);

            xElement.AddFieldValueIfNotNull("supervisor", Supervisor);
            xElement.AddFieldValueIfNotNull("supervisorId", SupervisorId);
            xElement.AddFieldValueIfNotNull("supervisorEid", SupervisorEid);
            xElement.AddFieldValueIfNotNull("supervisorEmail", SupervisorEmail);

            return xElement.ToString();
        }

        public static string[] FieldNames = new[] 
        {
            "acaStatusCategory",
            "address1",
            "address2",
            "age",
            "bestEmail",
            "birthday",
            "bonusAmount",
            "bonusComment",
            "bonusDate",
            "bonusReason",
            "city",
            "commissionAmount",
            "commissionComment",
            "commissionDate",
            "commisionDate",
            "country",
            "createdByUserId",
            "dateOfBirth",
            "department",
            "division",
            "eeo",
            "employeeNumber",
            "employmentHistoryStatus",
            "ethnicity",
            "exempt",
            "firstName",
            "fullName1",
            "fullName2",
            "fullName3",
            "fullName4",
            "fullName5",
            "displayName",
            "gender",
            "hireDate",
            "originalHireDate",
            "homeEmail",
            "homePhone",
            "id",
            "includeInPayroll",
            "isPhotoUploaded",
            "jobTitle",
            "lastChanged",
            "lastName",
            "location",
            "maritalStatus",
            "middleName",
            "mobilePhone",
            "nationalId",
            "nationality",
            "nin",
            "paidPer",
            "payChangeReason",
            "payGroup",
            "payGroupId",
            "payRate",
            "payRateEffectiveDate",
            "payType",
            "paidPer",
            "paySchedule",
            "payScheduleId",
            "payFrequency",
            "preferredName",
            "ssn",
            "sin",
            "standardHoursPerWeek",
            "state",
            "stateCode",
            "status",
            "supervisor",
            "supervisorId",
            "supervisorEId",
            "supervisorEmail",
            "terminationDate",
            "timeTrackingEnabled",
            "workEmail",
            "workPhone",
            "workPhonePlusExtension",
            "workPhoneExtension",
            "zipcode"
        };
}
}
