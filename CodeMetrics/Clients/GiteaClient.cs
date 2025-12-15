using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text.Json;
using CodeMetrics.Entity;
using CodeMetrics.Models;
using CodeMetrics.Options;
using CodeMetricsApi.Models;
using Microsoft.Extensions.Options;

namespace CodeMetrics.Clients;
public class GiteaClient
{
    private readonly HttpClient _httpClient;
    private readonly GiteaOptions _options;

    public GiteaClient(HttpClient httpClient, IOptions<GiteaOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<List<GiteaUser>> GetUsersAsync()
    {
        var result = await _httpClient.GetAsync($"/user?access_token={_options.Token}");
        return JsonSerializer.Deserialize<List<GiteaUser>>(await result.Content.ReadAsStringAsync());
    }

    public async Task<GiteaUser> GetUserByUserNameAsync(string username)
    {
        var result = await _httpClient.GetAsync($"/users/{username}?access_token={_options.Token}");
        return JsonSerializer.Deserialize<GiteaUser>(await result.Content.ReadAsStringAsync());
    }

    public async Task<List<RepositoryInfo>> GetReposAsync()
    {
        var result = await _httpClient.GetAsync($"/repos/search?access_token={_options.Token}");
        return JsonSerializer.Deserialize<List<RepositoryInfo>>(await result.Content.ReadAsStringAsync());
    }

    public async Task<List<BranchesResponse>> GetBranchesAsync(string repoName)
    {
        var result = await _httpClient.GetAsync($"/repos/Test/{repoName}/branches?access_token={_options.Token}");
        return JsonSerializer.Deserialize<List<BranchesResponse>>(await result.Content.ReadAsStringAsync());
    }

    public async Task<List<CommitResponse>> GetCommitsAsync(string repoName)
    {
        var result = await _httpClient.GetAsync($"/repos/Test/{repoName}/commits?access_token={_options.Token}");
        return JsonSerializer.Deserialize<List<CommitResponse>>(await result.Content.ReadAsStringAsync());
    }

    public async Task<CommitResponse> GetCommitByHashAsync(string repoName, string commitHash)
    {
        var result = await _httpClient.GetAsync($"/repos/Test/{repoName}/git/commits/{commitHash}?access_token={_options.Token}");
        return JsonSerializer.Deserialize<CommitResponse>(await result.Content.ReadAsStringAsync());
    }
}