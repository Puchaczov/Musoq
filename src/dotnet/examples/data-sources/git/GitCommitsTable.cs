using Musoq.Schema;

namespace Musoq.Examples.DataSources.Git;

public sealed class GitCommitsTable : ISchemaTable
{
    public GitCommitsTable()
    {
    }

    public GitCommitsTable(string repository)
    {
        _ = repository;
    }

    public ISchemaColumn[] Columns => GitCommitColumnCatalog.SchemaColumns;

    public SchemaTableMetadata Metadata { get; } = new(typeof(GitCommitRow));

    public ISchemaColumn? GetColumnByName(string name)
    {
        return GitCommitColumnCatalog.TryGetColumn(name, out var column)
            ? column.SchemaColumn
            : null;
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return GitCommitColumnCatalog.TryGetColumn(name, out var column)
            ? [column.SchemaColumn]
            : [];
    }
}
