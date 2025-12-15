using Newtonsoft.Json;

namespace CodeMetrics.Models
{
    public class RepositoryData
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
        [JsonProperty("owner_name")]
        public string OwnerName{ get; set; } = string.Empty;
        [JsonProperty("default_branch")]
        public string DefaultBranch { get; set; } = string.Empty;
        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }
        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get; set; }
        [JsonProperty("is_fork")]
        public bool IsFork { get; set; }
    }

    public class RepositoryApiResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; } = "";

        [JsonProperty("request_id")]
        public string RequestId { get; set; } = "";

        [JsonProperty("data")]
        public RepositoryData[] Data { get; set; } = [];
    }
}
