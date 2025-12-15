using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodeMetricsApi.Models;

namespace CodeMetrics.Clients
{
    public class SferaClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "https://gateway-codemetrics.saas.sferaplatform.ru/app/sourcecode/api/api/v2";

        private string access_token = "";
        private string refresh_token = "";

        public SferaClient() => _httpClient = new HttpClient();

        public async Task<CommitApiResponse> GetCommitsAsync(string projectKey, string repoName, int limit)
        {
            if (string.IsNullOrEmpty(access_token))
            {
                await Auth();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access_token);
            }

            var response = await _httpClient.GetAsync($"{_baseUrl}/projects/{projectKey}/repos/{repoName}/commits?fullHistory=true&limit={limit}");
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Ошибка API: {response.StatusCode}");

            var json = await response.Content.ReadAsStringAsync();
            var commit = JsonSerializer.Deserialize<CommitApiResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return commit;
        }

        public async Task<CommitApiResponse> GetCommitsAsync(string projectKey, string repoName, int limit, string brach)
        {
            if (string.IsNullOrEmpty(access_token))
            {
                await Auth();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access_token);
            }

            var response = await _httpClient.GetAsync(
                $"{_baseUrl}/projects/{projectKey}/repos/{repoName}/commits?fullHistory=true&limit={limit}&rev={brach}");
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Ошибка API: {response.StatusCode}");

            var json = await response.Content.ReadAsStringAsync();
            var commit = JsonSerializer.Deserialize<CommitApiResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return commit;
        }

        public async Task<string> GetProjects()
        {
            string url = $"{_baseUrl}/projects";

            var result = await SendQuerry(url);

            return result ?? "";
        }

        public async Task<string> GetProjectInfo(string projectKey)
        {
            string url = $"{_baseUrl}/projects/{projectKey}";

            var result = await SendQuerry(url);

            return result ?? "";
        }
        public async Task<string> GetRepositories(string projectKey)
        {
            string url = $"{_baseUrl}/projects/{projectKey}/repos";
            return await SendQuerry(url);
        }

        public async Task<string> GetRepositoryInfo(string projectKey, string reposName)
        {
            string url = $"{_baseUrl}/projects/{projectKey}/repos/{reposName}";
            return await SendQuerry(url);
        }

        public async Task<string> GetBranches(string projectKey, string repoName)
        {
            string url = $"{_baseUrl}/projects/{projectKey}/repos/{repoName}/branches";
            return await SendQuerry(url);
        }

        public async Task<string> GetCommitDiff(string projectKey, string repoName, string sha1)
        {
            string url = $"{_baseUrl}/projects/{projectKey}/repos/{repoName}/commits/{sha1}/diff";
            var result = await SendQuerry(url);
            return result;
        }

        private async Task<string> SendQuerry(string url)
        {
            if (string.IsNullOrEmpty(access_token))
            {
                await Auth();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access_token);
            }
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Ошибка API: {response.StatusCode}");

            return await response.Content.ReadAsStringAsync();
        }

        private async Task Auth()
        {
            string url = "https://gateway-codemetrics.saas.sferaplatform.ru/app/ppau/api/auth/login";
            JsonContent content = JsonContent.Create(new { username = "mtaganov468@gmail.com", password = "1Q3sbrV114lS" });

            var response = await _httpClient.PostAsync(url, content);

            var json = await response.Content.ReadAsStringAsync();
            var tokens = JsonSerializer.Deserialize<LoginResponse>(json);

            if (tokens == null)
                return;

            access_token = tokens.Access_token;
            refresh_token = tokens.Refresh_token;
        }

        private class LoginResponse
        {
            [JsonPropertyName("access_token")]
            public string Access_token { get; set; } = string.Empty;
            [JsonPropertyName("refresh_token")]
            public string Refresh_token { get; set; } = string.Empty;
        }
    }
}

