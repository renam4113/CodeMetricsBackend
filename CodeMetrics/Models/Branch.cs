using CodeMetricsApi.Models;
using Newtonsoft.Json;

namespace CodeMetrics.Models
{
    public class BranchData
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
        [JsonProperty("last_commit")]
        public CommitData LastCommit { get; set; } = new CommitData();
    }

    public class BranchApiResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; } = "";

        [JsonProperty("request_id")]
        public string RequestId { get; set; } = "";

        [JsonProperty("data")]
        public BranchData[] Data { get; set; } = [];
    }
}
