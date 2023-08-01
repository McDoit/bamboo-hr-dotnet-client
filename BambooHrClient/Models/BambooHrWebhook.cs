using System;
using System.Collections.Generic;

namespace BambooHrClient.Models
{
    public class BambooHrNewWebhook : BambooHrUpdatedWebhook
    {
        public string PrivateKey { get; set; }
    }

    public class BambooHrCreatedWebhook : BambooHrWebhook
    {

        public int Id { get; set; }
    }

    public class BambooHrUpdatedWebhook : BambooHrCreatedWebhook
    {
        public DateTime Created { get; set; }
        public DateTime? LastSent { get; set; }
    }
    public class BambooHrWebhook
    {
        public string Name { get; set; }
        public string[] MonitorFields { get; set; }
        public IDictionary<string, string> PostFields { get; set; }
        public Uri Url { get; set; }
        public string Format { get; set; }
        public BambooHrFrequency Frequency { get; set; }
        public BambooHrLimit Limit { get; set; }
        public bool IncludeCompanyDomain { get; set; }
    }

    public class BambooHrWebhookListItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime Created { get; set; }
        public DateTime? LastSent { get; set; }
        public Uri Url { get; set; }
    }

    public class BambooHrWebhookList
    {
        public BambooHrWebhookListItem[] Webhooks { get; set; }
    }

    public class BambooHrWebhookMonitorFieldList
    {
        public BambooHrField[] Fields { get; set; }
    }
}
