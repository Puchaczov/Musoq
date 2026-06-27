namespace Musoq.Examples.DataSources.Git;

public sealed record GitCommitRecord(
    string Repository,
    string Branch,
    string Sha,
    string AuthorName,
    string AuthorEmail,
    DateTime AuthoredAt,
    string Subject,
    string Message,
    bool IsMerge);
