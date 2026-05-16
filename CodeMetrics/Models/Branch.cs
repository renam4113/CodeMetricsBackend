using CodeMetricsApi.Models;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace CodeMetrics.Models
{
    public class BranchesResponse
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("commit")]
        public CommitInfo LastCommit { get; set; } = null!;
    }
}
