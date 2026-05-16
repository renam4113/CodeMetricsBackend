namespace CodeMetrics.Options
{
    public class GiteaOptions
    {
        public required string BaseUrl { get; set; }
        [ConfigurationKeyName("AccessToken")]
        public required string Token { get; set; }
        [ConfigurationKeyName("Owner")]
        public required string DefaultOwner { get; set; }
    }
}

