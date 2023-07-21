using System.Configuration;

namespace BambooHrClient
{
    public class BambooHrClientConfigurationManagerConfig : IBambooHrClientConfig
    {
        public static readonly BambooHrClientConfigurationManagerConfig Instance = new();

        public string BambooApiUser { get { return ConfigurationManager.AppSettings["BambooApiUser"]; } }
        public string BambooApiKey { get { return ConfigurationManager.AppSettings["BambooApiKey"]; } }
        public string BambooApiUrl { get { return ConfigurationManager.AppSettings["BambooApiUrl"]; } }
        public string BambooCompanyUrl { get { return ConfigurationManager.AppSettings["BambooCompanyUrl"]; } }
    }
}
