namespace Musoq.Examples.DataSources.Git;

public interface IGitHistoryStore
{
    IReadOnlyList<GitCommitRecord> GetCommits(string? repository);

    GitCommitStats GetStats(string sha);
}
