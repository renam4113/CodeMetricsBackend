using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace CodeMetrics.Models
{
    public class GiteaUser
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }
        [JsonPropertyName("username")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;
        [JsonPropertyName("starred_repos_count")]
        public long CreatingRepos { get; set; }
    }
}
