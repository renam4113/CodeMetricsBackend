using CodeMetricsApi.Models;
using Newtonsoft.Json;

namespace CodeMetrics.Models
{
    public class BranchesResponse
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
        [JsonProperty("commit")]
        public CommitInfo LastCommit { get; set; } = null!;
    }
}
