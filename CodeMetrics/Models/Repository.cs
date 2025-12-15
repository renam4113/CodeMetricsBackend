using Newtonsoft.Json;

namespace CodeMetrics.Models
{
    public class RepositoryApiResponse
    {
        [JsonProperty("ok")]
        public bool Ok { get; set; }

        [JsonProperty("data")]
        public List<RepositoryInfo> Data { get; set; }
    }

    public class RepositoryInfo
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("owner")]
        public GiteaUser Owner { get; set; }  // переименовать Creater в Owner

        [JsonProperty("name")]
        public string Name { get; set; } = null!;

        [JsonProperty("full_name")]
        public string FullName { get; set; } = null!;

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonProperty("default_branch")]
        public string DefaultBranch { get; set; } = string.Empty;

        [JsonProperty("size")]
        public long Size { get; set; }

        [JsonProperty("language")]
        public string Language { get; set; } = string.Empty;

        // Можно добавить другие нужные поля
    }

    // Удалить класс RepoResponse или использовать его для чего-то другого
}