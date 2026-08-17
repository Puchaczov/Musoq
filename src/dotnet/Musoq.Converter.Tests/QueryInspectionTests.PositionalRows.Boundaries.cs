using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator;
using Musoq.Schema.DataSources;
using Musoq.Tests.Common.Schema;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForExecution_WhenPositionalRowsFeedUnion_ShouldPreserveBothRowFields()
    {
        const string query = "select current.Key, current.Value from #positional.all() current union (Key, Value) select previous.Key, previous.Value from #positional.all() previous";
        var inspection = Inspect(query, CreateKeyValuePositionalRowsSchemaProvider());
        var table = CompileForExecution(query, CreateKeyValuePositionalRowsSchemaProvider()).Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("first", table[0][0]);
        Assert.AreEqual("current-first", table[0][1]);
        AssertGeneratedCSharpContains("[0]", inspection.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("[1]", inspection.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenPositionalRowsFeedUnionAll_ShouldPreserveBothRowFields()
    {
        const string query = "select current.Key, current.Value from #positional.all() current union all (Key, Value) select previous.Key, previous.Value from #positional.all() previous";
        var inspection = Inspect(query, CreateKeyValuePositionalRowsSchemaProvider());
        var table = CompileForExecution(query, CreateKeyValuePositionalRowsSchemaProvider()).Run();

        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("first", table[2][0]);
        Assert.AreEqual("current-first", table[2][1]);
        AssertGeneratedCSharpContains("[0]", inspection.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("[1]", inspection.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenPositionalRowsFeedIntersect_ShouldPreserveBothRowFields()
    {
        const string query = "select current.Key, current.Value from #positional.all() current intersect (Key, Value) select previous.Key, previous.Value from #positional.all() previous";
        var inspection = Inspect(query, CreateKeyValuePositionalRowsSchemaProvider());
        var table = CompileForExecution(query, CreateKeyValuePositionalRowsSchemaProvider()).Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("second", table[1][0]);
        Assert.AreEqual("current-second", table[1][1]);
        AssertGeneratedCSharpContains("[0]", inspection.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("[1]", inspection.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenPositionalRowsFeedExcept_ShouldPreserveBothRowFields()
    {
        const string query = "select current.Key, current.Value from #positional.all() current except (Key, Value) select previous.Key, previous.Value from #positional.all() previous";
        var inspection = Inspect(query, CreateKeyValuePositionalRowsSchemaProvider());
        var table = CompileForExecution(query, CreateKeyValuePositionalRowsSchemaProvider()).Run();

        Assert.AreEqual(0, table.Count);
        AssertGeneratedCSharpContains("[0]", inspection.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("[1]", inspection.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenPositionalCteUsesSidecarIndexes_ShouldPreserveMaterializedRowFields()
    {
        AssertPositionalCteMaterializationPreservesRowFields(true);
    }

    [TestMethod]
    public void CompileForExecution_WhenPositionalCteDoesNotUseSidecarIndexes_ShouldPreserveMaterializedRowFields()
    {
        AssertPositionalCteMaterializationPreservesRowFields(false);
    }

    private void AssertPositionalCteMaterializationPreservesRowFields(bool useSidecarIndexes)
    {
        const string query = "with state as (select * from #positional.all() entity) select current.Key from state current inner join state previous on current.Key = previous.Key where current.Value is not null order by current.Key";
        var provider = CreateKeyValuePositionalRowsSchemaProvider();
        var inspection = Inspect(query, provider, new CompilationOptions(useCteSidecarIndexes: useSidecarIndexes));
        var table = CompileForExecution(query, provider, new CompilationOptions(useCteSidecarIndexes: useSidecarIndexes)).Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("first", table[0][0]);
        Assert.AreEqual("second", table[1][0]);
        AssertGeneratedCSharpContains("[0]", inspection.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("[1]", inspection.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenPositionalCteResultIsDistinctFilteredAndOrdered_ShouldKeepCarrierBinding()
    {
        const string query = "with state as (select * from #positional.all() entity) select distinct state.Key from state where state.Value like 'current-%' order by state.Key";
        var provider = CreateKeyValuePositionalRowsSchemaProvider();
        var inspection = Inspect(query, provider);
        var table = CompileForExecution(query, provider).Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("first", table[0][0]);
        Assert.AreEqual("second", table[1][0]);
        AssertGeneratedCSharpContains("[0]", inspection.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("[1]", inspection.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenPositionalSchemaIndexesArePermutedAndPayloadIsNull_ShouldUseDeclaredIndexes()
    {
        const string query = "with state as (select * from #positional.all() entity) select state.Value, state.Key from state order by state.Key";
        var provider = new PositionalRowsSchemaProvider(
            [
                new SchemaColumn("Value", 0, typeof(string)),
                new SchemaColumn("Key", 1, typeof(string))
            ],
            [
                [null!, "first"],
                ["current-second", "second"]
            ]);
        var inspection = Inspect(query, provider);
        var table = CompileForExecution(query, provider).Run();

        Assert.AreEqual(2, table.Count);
        Assert.IsNull(table[0][0]);
        Assert.AreEqual("first", table[0][1]);
        AssertGeneratedCSharpContains("[0]", inspection.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("[1]", inspection.GeneratedCSharpCode);
    }
}
