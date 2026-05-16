namespace CodeMetrics.Models
{
    public class CommitStatsDto
    {
        public int AddedLines { get; set; }
        public int DeletedLines { get; set; }
        public int ChangedFiles { get; set; }
        public int TotalChanges => AddedLines + DeletedLines;
    }

    public class CommitDto
    {
        public string Hash { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string authorEmail { get; set; } = string.Empty;
        public string authorName { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public string repoName { get; set; } = string.Empty;
        public string committerEmail { get; set; } = string.Empty;
        public string committerName { get; set; } = string.Empty;

        public CommitStatsDto CommitStats { get; set; } = new CommitStatsDto();
    }

    public class CommitWithStatsResponse
    {
        public CommitDto Commit { get; set; } = new CommitDto();
    }
}
