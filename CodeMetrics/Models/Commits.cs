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
        [JsonPropertyName("timestamp")]
        public DateTimeOffset created_at { get; set; }
    }

    public class CommitResponse
    {
        [JsonPropertyName("sha")]
        public string hash { get; set; } = null!;

        [JsonPropertyName("created")]
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
        [JsonPropertyName("total")]
        public int TotalChanged { get; set; }
        [JsonPropertyName("additions")]
        public int AddedLines { get; set; }
        [JsonPropertyName("deletions")]
        public int RemovedLines { get; set; }
    }
}