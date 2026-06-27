namespace Musoq.Examples.DataSources.Git;

public sealed class GitCommitRow
{
    private readonly Lazy<GitCommitStats> _stats;

    public GitCommitRow(GitCommitRecord record, Func<string, GitCommitStats> loadStats)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(loadStats);

        Repository = record.Repository;
        Branch = record.Branch;
        Sha = record.Sha;
        ShortSha = CreateShortSha(record.Sha);
        AuthorName = record.AuthorName;
        AuthorEmail = record.AuthorEmail;
        AuthoredAt = record.AuthoredAt;
        Subject = record.Subject;
        Message = record.Message;
        IsMerge = record.IsMerge;
        _stats = new Lazy<GitCommitStats>(() => loadStats(record.Sha));
    }

    public string Repository { get; }

    public string Branch { get; }

    public string Sha { get; }

    public string ShortSha { get; }

    public string AuthorName { get; }

    public string AuthorEmail { get; }

    public DateTime AuthoredAt { get; }

    public string Subject { get; }

    public string Message { get; }

    public int ChangedFiles => _stats.Value.ChangedFiles;

    public int Additions => _stats.Value.Additions;

    public int Deletions => _stats.Value.Deletions;

    public int Churn => _stats.Value.Churn;

    public bool IsMerge { get; }

    private static string CreateShortSha(string sha)
    {
        if (string.IsNullOrWhiteSpace(sha))
            return string.Empty;

        return sha.Length <= 7 ? sha : sha[..7];
    }
}
