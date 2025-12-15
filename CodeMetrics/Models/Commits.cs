using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.Extensions.ObjectPool;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace CodeMetricsApi.Models
{
    public class CommitApiResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; } = "";

        [JsonProperty("request_id")]
        public string RequestId { get; set; } = "";

        [JsonProperty("data")]
        public List<CommitData> Data { get; set; } = new List<CommitData>();
    }



    public class CommitData
    {
        public string hash { get; set; } = string.Empty;
        public string message { get; set; } = string.Empty;
        public User author { get; set; } = new User();
        public User committer { get; set; } = new User();
        public DateTimeOffset created_at { get; set; }
    }

    public class User
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
        [JsonProperty("email")]
        public string Email { get; set; } = string.Empty;
    }

    public class CommitDiffApiResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; } = "";

        [JsonProperty("request_id")]
        public string RequestId { get; set; } = "";

        [JsonProperty("data")]
        public CommitDiffData Data { get; set; } = new CommitDiffData();
    }

    public class CommitDiffData
    {
        [JsonProperty("source_head_id")]
        public string SourceHeadId { get; set; } = string.Empty;

        [JsonProperty("content")]
        public string Content { get; set; } = string.Empty;

        [JsonProperty("large_files")]
        public List<string> LargeFiles { get; set; } = [];

        [JsonProperty("excluded_files")]
        public List<string> ExcludedFiles { get; set; } = [];
    }
}