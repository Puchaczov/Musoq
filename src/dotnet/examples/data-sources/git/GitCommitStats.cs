namespace Musoq.Examples.DataSources.Git;

public sealed record GitCommitStats(int ChangedFiles, int Additions, int Deletions)
{
    public static GitCommitStats Empty { get; } = new(0, 0, 0);

    public int Churn => Additions + Deletions;
}
