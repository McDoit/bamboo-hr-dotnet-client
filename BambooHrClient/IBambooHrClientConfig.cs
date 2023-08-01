namespace BambooHrClient
{
    public interface IBambooHrClientConfig
    {
        string BambooApiUser { get; }
        string BambooApiKey { get; }
        string BambooApiUrl { get; }
        string BambooCompanyUrl { get; }
    }
}