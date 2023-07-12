using RestSharp.Serializers;
using System;
using System.Collections.Generic;

namespace BambooHrClient.Models
{
    public class BambooHrDirectory
    {
        public List<BambooHrField> Fields { get; set; }
        public List<BambooHrUser> Employees { get; set; }
    }

    [DeserializeAs(Name = "User")]
    public class BambooHrUser
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string Email { get; set; }
        
        public DateTime LastLogin { get; set; }

        public string Status { get; set; }

        public string LastFirst
        {
            get
            {
                return LastName + ", " + FirstName;
            }
        }

        public string FirstLast
        {
            get
            {
                return FirstName + " " + LastName;
            }
        }

        /// <summary>
        /// Parameterless constructor for XML deserialization.
        /// </summary>
        public BambooHrUser()
        {

        }
    }
}
