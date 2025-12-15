using CodeMetricsApi.Models;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace CodeMetrics.Models
{
    public class ProjectData
    {
        public int Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;
        [JsonProperty("is_public")]
        public bool IsPublic { get; set; }
        [JsonProperty("created_at")] 
        public DateTime CreatedAt { get; set; }
        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    public class ProjectApiResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; } = "";

        [JsonProperty("request_id")]
        public string RequestId { get; set; } = "";

        [JsonProperty("data")]
        public ProjectData[] Data { get; set; } = [];
    }
}
