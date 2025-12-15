using Newtonsoft.Json;

namespace CodeMetrics.Models
{
    public class GiteaUser
    {
        [JsonProperty("id")]
        public long Id { get; set; }
        [JsonProperty("username")]
        public string Name { get; set; } = string.Empty;
        [JsonProperty("email")]
        public string Email { get; set; } = string.Empty;
        [JsonProperty("starred_repos_count")]
        public long CreatingRepos { get; set; }
    }
}
