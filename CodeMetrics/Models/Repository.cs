using Newtonsoft.Json;

namespace CodeMetrics.Models
{
    public class RepositoryData
    {
        public RepositoryInfo data { get; set; }
    }

    public class RepositoryInfo
    {
        public GiteaUser Creater { get; set; }
        public RepoResponse Repo { get; set; }

    }

    public class RepoResponse
    {
        [JsonProperty("name")]
        public string name { get; set; } = null!;
        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }
        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get; set; }
        [JsonProperty("default_branch")]
        public string DefaultBranch { get; set; } = string.Empty;
    }
}
