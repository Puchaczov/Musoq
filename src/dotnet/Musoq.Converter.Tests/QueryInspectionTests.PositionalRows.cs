using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Tests.Common.Schema;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForInspection_WhenSourceRowsAreSchemaIndexedObjectArrays_ShouldUseDirectAccess()
    {
        var result = Inspect(
            "select p.Name, p.Age, p.[Address.City] from #positional.all() p",
            CreatePositionalRowsSchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertExecutionPlanContains("position 0", result.ExecutionPlanText);
        AssertExecutionPlanContains("position 2", result.ExecutionPlanText);
        AssertExecutionPlanContains("position 3", result.ExecutionPlanText);
        AssertGeneratedCSharpContains("[0]", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("[2]", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("[3]", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("ExpandoAdapter", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("GeneratedDictionaryAccess", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("GetColumnValue", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("GetProperty(", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("GetNestedValue", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("System.Reflection", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("NestedPositionalAccess", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenSourceRowsAreSchemaIndexedObjectArrays_ShouldReadSchemaIndexes()
    {
        var compiled = CompileForExecution(
            "select p.Name, p.Age, p.Department, p.[Address.City] from #positional.all() p",
            CreatePositionalRowsSchemaProvider());

        var table = compiled.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("Ada", table[0][0]);
        Assert.AreEqual(37, table[0][1]);
        Assert.AreEqual("Engineering", table[0][2]);
        Assert.AreEqual("London", table[0][3]);
        Assert.IsNull(table[2][1]);
        Assert.AreEqual("Berlin", table[2][3]);
    }

    [TestMethod]
    public void CompileForExecution_WhenIndexedCellIsTraversed_ShouldUseNestedPositionalAccess()
    {
        var provider = new PositionalRowsSchemaProvider(
            [new SchemaColumn("Address", 0, typeof(AddressCell))],
            [[new AddressCell("London")]]);
        var diagnostics = InstanceCreator.CompileWithDiagnostics(
            "select p.Address.City from #positional.all() p",
            Guid.NewGuid().ToString(),
            provider,
            _loggerResolver);
        Assert.IsFalse(diagnostics.HasErrors, string.Join(Environment.NewLine, diagnostics.Errors.Select(error => error.Message)));
        var inspection = Inspect("select p.Address.City from #positional.all() p", provider);

        AssertGeneratedCSharpContains("[0]", inspection.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("City", inspection.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("[1]", inspection.GeneratedCSharpCode);

        var table = CompileForExecution(
            "select p.Address.City from #positional.all() p",
            provider).Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("London", table[0][0]);
    }

    [TestMethod]
    public void CompileForExecution_WhenPositionalColumnNameContainsSymbols_ShouldTreatItAsOpaque()
    {
        var result = Inspect(
            "select p.[%$@%T@sas$@#$] from #positional.all() p",
            CreatePositionalRowsSchemaProvider());

        AssertGeneratedCSharpContains("[4]", result.GeneratedCSharpCode);

        var table = CompileForExecution(
            "select p.[%$@%T@sas$@#$] from #positional.all() p",
            CreatePositionalRowsSchemaProvider()).Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("symbolic-1", table[0][0]);
        Assert.AreEqual("symbolic-3", table[2][0]);
    }

    [TestMethod]
    public void CompileForExecution_WhenPositionalSourceFeedsCte_ShouldPreserveLiteralAndTypedValues()
    {
        var compiled = CompileForExecution(
            """
            with people as (
                select p.Name, p.Age, p.[Address.City]
                from #positional.all() p
                where p.Age >= 30
            )
            select people.Name, people.[Address.City]
            from people
            order by people.Name
            """,
            CreatePositionalRowsSchemaProvider());

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Ada", table[0][0]);
        Assert.AreEqual("London", table[0][1]);
    }

    [TestMethod]
    public void CompileForExecution_WhenSelectingAllPositionalColumns_ShouldReadEveryIndex()
    {
        const string query = "select * from #positional.all() entity";
        var provider = CreateKeyValuePositionalRowsSchemaProvider();
        var inspection = Inspect(query, provider);

        var table = CompileForExecution(query, provider).Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("first", table[0][0]);
        Assert.AreEqual("current-first", table[0][1]);
        AssertGeneratedCSharpContains("[0]", inspection.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("[1]", inspection.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenCteProjectsEveryPositionalColumn_ShouldPreserveEveryIndex()
    {
        const string query = "with state as (select entity.Key, entity.Value from #positional.all() entity) select state.Key, state.Value from state";
        var provider = CreateKeyValuePositionalRowsSchemaProvider();
        var inspection = Inspect(query, provider);

        var table = CompileForExecution(query, provider).Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("first", table[0][0]);
        Assert.AreEqual("current-first", table[0][1]);
        AssertGeneratedCSharpContains("[0]", inspection.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("[1]", inspection.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenPositionalSourcesFeedExcept_ShouldUseIndexedSetKeys()
    {
        const string query = "select current.Key from #positional.all() current except select previous.Key from #positional.all() previous";
        var provider = CreateKeyValuePositionalRowsSchemaProvider();
        var inspection = Inspect(query, provider);

        var table = CompileForExecution(query, provider).Run();

        Assert.AreEqual(0, table.Count);
        AssertGeneratedCSharpContains("[0]", inspection.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenSelectAllPositionalSourceFeedsCteAndExcept_ShouldPreserveIndexedAccess()
    {
        const string query = """
                             with state as (
                                 select *
                                 from #positional.all() entity
                             )
                             select current.Key
                             from state current
                             except
                             select previous.Key
                             from #positional.all() previous
                             """;
        var provider = new PositionalRowsSchemaProvider(
            [
                new SchemaColumn("Key", 0, typeof(string)),
                new SchemaColumn("Value", 1, typeof(string))
            ],
            [
                ["first", "current-first"],
                ["second", "current-second"]
            ]);

        var table = CompileForExecution(query, provider).Run();
        var inspection = Inspect(query, provider);

        Assert.AreEqual(0, table.Count);
        AssertGeneratedCSharpContains("[0]", inspection.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("[1]", inspection.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotUseMemberAccessOnAliases(
            inspection.GeneratedCSharpCode,
            "entity",
            "previous");
    }

    [TestMethod]
    public void CompileForExecution_WhenPositionalSourceIsGrouped_ShouldAggregateDirectReads()
    {
        var compiled = CompileForExecution(
            "select p.Department, Count(p.Name) as People from #positional.all() p group by p.Department order by p.Department",
            CreatePositionalRowsSchemaProvider());

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("Engineering", table[0][0]);
        Assert.AreEqual(1L, table[0][1]);
        Assert.AreEqual("Research", table[1][0]);
        Assert.AreEqual(2L, table[1][1]);
    }

    [TestMethod]
    public void CompileForExecution_WhenTwoPositionalSourcesAreJoined_ShouldUseBothIndexContracts()
    {
        var compiled = CompileForExecution(
            "select p.Name, q.[Address.City] from #positional.all() p inner join #positional.all() q on p.Age = q.Age order by p.Name",
            CreatePositionalRowsSchemaProvider());

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("Ada", table[0][0]);
        Assert.AreEqual("London", table[0][1]);
        Assert.AreEqual("Bea", table[1][0]);
        Assert.AreEqual("Paris", table[1][1]);
    }

    [TestMethod]
    public void CompileForExecution_WhenSamePositionalQueryIsCompiledTwice_ShouldRemainStable()
    {
        const string query = "select p.Name, p.Age from #positional.all() p where p.Name like 'A%'";
        var provider = CreatePositionalRowsSchemaProvider();

        for (var iteration = 0; iteration < 2; iteration++)
        {
            var compiled = CompileForExecution(query, provider);
            var table = compiled.Run();

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual("Ada", table[0][0]);
            Assert.AreEqual(37, table[0][1]);
        }
    }

    private static PositionalRowsSchemaProvider CreatePositionalRowsSchemaProvider()
    {
        return new PositionalRowsSchemaProvider(
            [
                new SchemaColumn("Name", 2, typeof(string)),
                new SchemaColumn("Age", 0, typeof(int?)),
                new SchemaColumn("Department", 1, typeof(string)),
                new SchemaColumn("Address.City", 3, typeof(string)),
                new SchemaColumn("%$@%T@sas$@#$", 4, typeof(string))
            ],
            [
                [37, "Engineering", "Ada", "London", "symbolic-1"],
                [29, "Research", "Bea", "Paris", "symbolic-2"],
                [null!, "Research", "Cid", "Berlin", "symbolic-3"]
            ]);
    }

    private static PositionalRowsSchemaProvider CreateKeyValuePositionalRowsSchemaProvider()
    {
        return new PositionalRowsSchemaProvider(
            [
                new SchemaColumn("Key", 0, typeof(string)),
                new SchemaColumn("Value", 1, typeof(string))
            ],
            [
                ["first", "current-first"],
                ["second", "current-second"]
            ]);
    }

    public sealed class AddressCell(string city)
    {
        public string City { get; } = city;
    }

}
