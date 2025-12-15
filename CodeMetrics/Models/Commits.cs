using CodeMetrics.Models;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.Extensions.ObjectPool;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace CodeMetricsApi.Models
{
    public class CommitInfo
    {
        public string id { get; set; } = string.Empty;
        public string message { get; set; } = string.Empty;
        public GiteaUser author { get; set; } = null!;
        public GiteaUser committer { get; set; } = null!;
        [JsonProperty("timestamp")]
        public DateTimeOffset created_at { get; set; }
    }

    public class CommitResponse
    {
        [JsonProperty("sha")]
        public string hash { get; set; } = null!;

        [JsonProperty("created")]
        public DateTimeOffset CreatedAt { get; set; }
        public List<CommitFiles> files { get; set; }
        public CommitInfo commit { get; set; } = null!;

        public CommitDiffData stats { get; set; } = null!;
    }

    public class CommitFiles
    {
        public string filename { get; set; } = string.Empty;
    }
    public class CommitDiffData
    {
        [JsonProperty("total")]
        public int TotalChanged { get; set; }
        [JsonProperty("additions")]
        public int AddedLines { get; set; }
        [JsonProperty("deletions")]
        public int RemovedLines { get; set; }
    }
}