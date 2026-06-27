using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema;
using Musoq.Schema.Exceptions;
using Musoq.Schema.Optimization;

namespace Musoq.Examples.DataSources.Git.Tests;

[TestClass]
public sealed class GitProviderAndSchemaTests : GitExampleTestBase
{
    [TestMethod]
    public void Provider_WhenSchemaNameIsGit_ShouldResolveSchema()
    {
        var provider = new GitSchemaProvider();

        Assert.IsInstanceOfType<GitSchema>(provider.GetSchema("git"));
        Assert.IsInstanceOfType<GitSchema>(provider.GetSchema("#git"));
    }

    [TestMethod]
    public void Provider_WhenSchemaNameIsUnknown_ShouldThrow()
    {
        var provider = new GitSchemaProvider();

        Assert.Throws<SourceNotFoundException>(() => provider.GetSchema("unknown"));
    }

    [TestMethod]
    public void Schema_WhenTableNameIsUnknown_ShouldThrow()
    {
        var schema = new GitSchema();
        var context = new SourceMetadataContext(
            "query",
            CancellationToken.None,
            [],
            new Dictionary<string, string>(),
            NullLogger.Instance);

        Assert.Throws<SchemaArgumentException>(() => schema.GetTableByName("unknown", context));
    }

    [TestMethod]
    public void Table_WhenColumnsAreRequested_ShouldUseColumnCatalog()
    {
        var table = new GitCommitsTable();

        var columns = table.Columns;

        Assert.AreEqual(GitCommitColumnCatalog.Columns.Count, columns.Length);
        for (var index = 0; index < columns.Length; index++)
        {
            var catalogColumn = GitCommitColumnCatalog.Columns[index];
            Assert.AreEqual(catalogColumn.Name, columns[index].ColumnName);
            Assert.AreEqual(catalogColumn.Ordinal, columns[index].ColumnIndex);
            Assert.AreEqual(catalogColumn.Type, columns[index].ColumnType);
        }
    }

    [TestMethod]
    public void Table_WhenColumnLookupUsesDifferentCasing_ShouldResolveColumn()
    {
        var table = new GitCommitsTable();

        var column = table.GetColumnByName("authorname");

        Assert.IsNotNull(column);
        Assert.AreEqual(nameof(GitCommitRow.AuthorName), column.ColumnName);
    }
}
