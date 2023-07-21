using System;

namespace BambooHrClient.Models
{
    public class BambooHrWebhook
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime Created { get; set; }
        public DateTime? LastSent { get; set; }
        public string[] MonitorFields { get; set; }
        public BambooHrField[] PostFields { get; set; }
        public Uri Url { get; set; }
        public string Format { get; set; }
        public BambooHrFrequency Frequency { get; set; }
        public BambooHrLimit Limit { get; set; }
        public string PrivateKey { get; set; }
        public bool IncludeCompanyDomain { get; set; }
    }
}
