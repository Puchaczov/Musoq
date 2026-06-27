using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Examples.DataSources.Git;

internal static class GitCommitColumnCatalog
{
    private static readonly GitCommitColumn[] Catalog =
    [
        Create(nameof(GitCommitRow.Repository), 0, typeof(string), static row => row.Repository),
        Create(nameof(GitCommitRow.Branch), 1, typeof(string), static row => row.Branch),
        Create(nameof(GitCommitRow.Sha), 2, typeof(string), static row => row.Sha),
        Create(nameof(GitCommitRow.ShortSha), 3, typeof(string), static row => row.ShortSha),
        Create(nameof(GitCommitRow.AuthorName), 4, typeof(string), static row => row.AuthorName),
        Create(nameof(GitCommitRow.AuthorEmail), 5, typeof(string), static row => row.AuthorEmail),
        Create(nameof(GitCommitRow.AuthoredAt), 6, typeof(DateTime), static row => row.AuthoredAt),
        Create(nameof(GitCommitRow.Subject), 7, typeof(string), static row => row.Subject),
        Create(nameof(GitCommitRow.Message), 8, typeof(string), static row => row.Message),
        Create(nameof(GitCommitRow.ChangedFiles), 9, typeof(int), static row => row.ChangedFiles, isExpensive: true),
        Create(nameof(GitCommitRow.Additions), 10, typeof(int), static row => row.Additions, isExpensive: true),
        Create(nameof(GitCommitRow.Deletions), 11, typeof(int), static row => row.Deletions, isExpensive: true),
        Create(nameof(GitCommitRow.Churn), 12, typeof(int), static row => row.Churn, isExpensive: true),
        Create(nameof(GitCommitRow.IsMerge), 13, typeof(bool), static row => row.IsMerge)
    ];

    private static readonly IReadOnlyDictionary<string, GitCommitColumn> ByName =
        Catalog.ToDictionary(static column => column.Name, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<GitCommitColumn> Columns => Catalog;

    public static ISchemaColumn[] SchemaColumns =>
        Catalog
            .Select(static column => column.SchemaColumn)
            .ToArray();

    public static bool TryGetColumn(string name, out GitCommitColumn column)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            column = GitCommitColumn.Empty;
            return false;
        }

        return ByName.TryGetValue(name, out column!);
    }

    public static GitCommitColumn GetColumn(string name)
    {
        return TryGetColumn(name, out var column)
            ? column
            : throw new InvalidOperationException($"Git commit source has no column '{name}'.");
    }

    private static GitCommitColumn Create(
        string name,
        int ordinal,
        Type type,
        Func<GitCommitRow, object?> valueSelector,
        bool isExpensive = false)
    {
        return new GitCommitColumn(
            name,
            ordinal,
            type,
            isExpensive,
            valueSelector,
            new SchemaColumn(name, ordinal, type));
    }
}

internal sealed record GitCommitColumn(
    string Name,
    int Ordinal,
    Type Type,
    bool IsExpensive,
    Func<GitCommitRow, object?> ValueSelector,
    ISchemaColumn SchemaColumn)
{
    public static GitCommitColumn Empty { get; } = new(
        string.Empty,
        -1,
        typeof(object),
        false,
        static _ => null,
        new SchemaColumn(string.Empty, -1, typeof(object)));
}
