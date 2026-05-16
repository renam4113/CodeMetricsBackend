using CodeMetrics.Application.Contracts;
using CodeMetrics.Application.DTOs.Commits;
using CodeMetrics.Application.DTOs.Database;
using CodeMetrics.Application.Mappers;
using CodeMetrics.Context;
using Microsoft.EntityFrameworkCore;

namespace CodeMetrics.Services;

public sealed class CodeMetricsDatabaseService : ICodeMetricsDatabaseService
{
    private readonly CodeMetricsDbContext _context;

    public CodeMetricsDatabaseService(CodeMetricsDbContext context) => _context = context;

    public async Task<IReadOnlyList<RepositoryDto>> GetRepositoriesAsync()
    {
        var items = await _context.Repositories.AsNoTracking().OrderBy(r => r.RepoName).ToListAsync();
        return items.Select(EntityMapper.ToDto).ToList();
    }

    public async Task<RepositoryDto?> GetRepositoryAsync(string repoName)
    {
        var entity = await _context.Repositories
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RepoName == repoName);
        return entity is null ? null : EntityMapper.ToDto(entity);
    }

    public async Task<IReadOnlyList<BranchDto>> GetBranchesByRepoAsync(string repoName)
    {
        var items = await _context.Branches
            .AsNoTracking()
            .Where(b => b.repoName == repoName)
            .OrderBy(b => b.BranchName)
            .ToListAsync();
        return items.Select(EntityMapper.ToDto).ToList();
    }

    public async Task<IReadOnlyList<CommitListItemDto>> GetCommitsByRepoAsync(string repoName)
    {
        var items = await _context.Commits
            .AsNoTracking()
            .Where(c => c.repoName == repoName)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
        return items.Select(EntityMapper.ToListItem).ToList();
    }

    public async Task<CommitListItemDto?> GetCommitByHashAsync(string hash)
    {
        var entity = await _context.Commits
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Hash == hash);
        return entity is null ? null : EntityMapper.ToListItem(entity);
    }

    public async Task<CommitStatsRecordDto?> GetCommitStatsByHashAsync(string commitHash)
    {
        var entity = await _context.CommitStats
            .AsNoTracking()
            .FirstOrDefaultAsync(cs => cs.CommitHash == commitHash);
        return entity is null ? null : EntityMapper.ToDto(entity);
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        var entity = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserEmail == email);
        return entity is null ? null : EntityMapper.ToDto(entity);
    }
}
