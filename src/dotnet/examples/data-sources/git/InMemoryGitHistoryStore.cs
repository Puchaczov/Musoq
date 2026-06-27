namespace Musoq.Examples.DataSources.Git;

public sealed class InMemoryGitHistoryStore : IGitHistoryStore
{
    private readonly IReadOnlyList<GitCommitRecord> _commits;
    private readonly IReadOnlyDictionary<string, GitCommitStats> _statsBySha;

    public InMemoryGitHistoryStore(IEnumerable<GitCommitRecord> commits)
        : this(commits, new Dictionary<string, GitCommitStats>())
    {
    }

    public InMemoryGitHistoryStore(
        IEnumerable<GitCommitRecord> commits,
        IReadOnlyDictionary<string, GitCommitStats> statsBySha)
    {
        ArgumentNullException.ThrowIfNull(commits);
        ArgumentNullException.ThrowIfNull(statsBySha);

        _commits = commits.ToArray();
        _statsBySha = new Dictionary<string, GitCommitStats>(statsBySha, StringComparer.Ordinal);
    }

    public IReadOnlyList<GitCommitRecord> GetCommits(string? repository)
    {
        if (string.IsNullOrWhiteSpace(repository))
            return _commits;

        return _commits
            .Where(commit => string.Equals(commit.Repository, repository, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    public GitCommitStats GetStats(string sha)
    {
        return _statsBySha.TryGetValue(sha, out var stats)
            ? stats
            : GitCommitStats.Empty;
    }

    public static InMemoryGitHistoryStore CreateDefault()
    {
        var commits = new[]
        {
            CreateCommit(
                "musoq",
                "main",
                "a1b2c3d4e5f6012345678901234567890abcdef",
                "Alice Runtime",
                "alice@example.test",
                new DateTime(2026, 1, 8, 10, 15, 0, DateTimeKind.Utc),
                "Add runtime planner",
                "Add the first source planning pass for runtime v2.",
                false,
                new GitCommitStats(5, 240, 31)),
            CreateCommit(
                "musoq",
                "main",
                "b2c3d4e5f6012345678901234567890abcdefa1",
                "Bob Evaluator",
                "bob@example.test",
                new DateTime(2026, 1, 12, 14, 30, 0, DateTimeKind.Utc),
                "Tune evaluator",
                "Tune evaluator code generation for planned sources.",
                false,
                new GitCommitStats(3, 80, 20)),
            CreateCommit(
                "musoq",
                "runtime-v2",
                "c3d4e5f6012345678901234567890abcdefa1b2",
                "Alice Runtime",
                "alice@example.test",
                new DateTime(2026, 2, 1, 9, 0, 0, DateTimeKind.Utc),
                "Merge runtime v2",
                "Merge runtime v2 planning work back into main.",
                true,
                new GitCommitStats(8, 160, 100)),
            CreateCommit(
                "docs",
                "main",
                "d4e5f6012345678901234567890abcdefa1b2c3",
                "Cara Docs",
                "cara@example.test",
                new DateTime(2025, 12, 20, 16, 45, 0, DateTimeKind.Utc),
                "Document query samples",
                "Document basic query samples for new users.",
                false,
                new GitCommitStats(2, 60, 4)),
            CreateCommit(
                "docs",
                "main",
                "e5f6012345678901234567890abcdefa1b2c3d4",
                "Bob Evaluator",
                "bob@example.test",
                new DateTime(2026, 3, 4, 11, 20, 0, DateTimeKind.Utc),
                "Refresh runtime docs",
                "Refresh runtime v2 docs after the planning cleanup.",
                false,
                new GitCommitStats(4, 120, 12))
        };

        return new InMemoryGitHistoryStore(
            commits.Select(static commit => commit.Record),
            commits.ToDictionary(static commit => commit.Record.Sha, static commit => commit.Stats));
    }

    private static (GitCommitRecord Record, GitCommitStats Stats) CreateCommit(
        string repository,
        string branch,
        string sha,
        string authorName,
        string authorEmail,
        DateTime authoredAt,
        string subject,
        string message,
        bool isMerge,
        GitCommitStats stats)
    {
        return (
            new GitCommitRecord(
                repository,
                branch,
                sha,
                authorName,
                authorEmail,
                authoredAt,
                subject,
                message,
                isMerge),
            stats);
    }
}
