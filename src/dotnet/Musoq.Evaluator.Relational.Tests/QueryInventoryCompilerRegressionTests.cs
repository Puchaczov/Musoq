using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
[TestCategory("QueryInventoryCompilerReproduction")]
public sealed partial class QueryInventoryCompilerRegressionTests : BasicEntityTestBase
{
    [TestMethod]
    public void DistinctWildcard_InlineValues_ShouldReturnDeclaredColumn()
    {
        const string query = "SELECT DISTINCT v.* FROM values {(Id: 1)} v";

        using var compiled = Compile(query);
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result, ("Id", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, [1]);
    }

    [TestMethod]
    public void DistinctExplicitColumn_InlineValues_ShouldReturnDeclaredColumn()
    {
        const string query = "SELECT DISTINCT v.Id FROM values {(Id: 1)} v";

        using var compiled = Compile(query);
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result, ("Id", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, [1]);
    }

    [TestMethod]
    public void GroupByThirdJoinedSource_ShouldReturnMatchingGroup()
    {
        const string query = """
            SELECT r.Name, Count(*) AS Matches
            FROM values {(Id: 1)} m
            INNER JOIN values {(Id: 1)} p ON m.Id = p.Id
            INNER JOIN values {(Id: 1, Name: 'a')} r ON m.Id = r.Id
            GROUP BY r.Name
            """;

        using var compiled = Compile(query);
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result, ("Name", typeof(string)), ("Matches", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, ["a", 1L]);
    }

    [TestMethod]
    public void GroupByProjectedThirdJoinedSource_ShouldReturnMatchingGroup()
    {
        const string query = """
            WITH joined AS (
                SELECT r.Name
                FROM values {(Id: 1)} m
                INNER JOIN values {(Id: 1)} p ON m.Id = p.Id
                INNER JOIN values {(Id: 1, Name: 'a')} r ON m.Id = r.Id
            )
            SELECT j.Name, Count(*) AS Matches FROM joined j GROUP BY j.Name
            """;

        using var compiled = Compile(query);
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result, ("Name", typeof(string)), ("Matches", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, ["a", 1L]);
    }

    [TestMethod]
    public void CollectionApply_ReusedCteSourceAlias_ShouldReturnChildren()
    {
        const string query = """
            WITH parents AS (
                SELECT p.Children AS Children FROM #A.Entities() p
            )
            SELECT p.Name FROM parents q CROSS APPLY q.Children p
            """;

        using var compiled = Compile(query);
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, ["child1"], ["child2"]);
    }

    [TestMethod]
    public void CollectionApply_DistinctCteSourceAlias_ShouldReturnChildren()
    {
        const string query = """
            WITH parents AS (
                SELECT p.Children AS Children FROM #A.Entities() p
            )
            SELECT child.Name FROM parents q CROSS APPLY q.Children child
            """;

        using var compiled = Compile(query);
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, ["child1"], ["child2"]);
    }

    [TestMethod]
    public void NestedCollectionApply_ReusedCteSourceAlias_ShouldReturnArrayValues()
    {
        const string query = """
            WITH parents AS (
                SELECT p.Array AS Items
                FROM #A.Entities() s CROSS APPLY s.Children p
            )
            SELECT p.Value FROM parents q CROSS APPLY q.Items p
            """;

        using var compiled = Compile(query);
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result, ("Value", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, [0], [1], [2], [0], [1], [2]);
    }

    [TestMethod]
    public void NestedCollectionApply_DistinctCteSourceAlias_ShouldReturnArrayValues()
    {
        const string query = """
            WITH parents AS (
                SELECT p.Array AS Items
                FROM #A.Entities() s CROSS APPLY s.Children p
            )
            SELECT item.Value FROM parents q CROSS APPLY q.Items item
            """;

        using var compiled = Compile(query);
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result, ("Value", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, [0], [1], [2], [0], [1], [2]);
    }

    private CompiledQuery Compile(string query, CompilationOptions? options = null, BasicEntity[]? parents = null) =>
        InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(
                new Dictionary<string, IEnumerable<BasicEntity>> { ["#A"] = parents ?? [new("parent")] }
            ),
            LoggerResolver,
            options ?? TestCompilationOptions
        );
}
