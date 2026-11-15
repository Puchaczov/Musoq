using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class QueryInventoryCompilerRegressionTests
{
    [TestMethod]
    [DataRow("*")]
    [DataRow("v.*")]
    public void DistinctWildcard_WithComputedColumn_ShouldDeduplicateNullRows(string projection)
    {
        var query = $$"""
            SELECT DISTINCT {{projection}}, CASE WHEN v.Kind IS NULL THEN 'missing' ELSE v.Kind END AS Label
            FROM values {
                (Id: 1, Kind: 'a'), (Id: 1, Kind: 'a'),
                (Id: 2, Kind: null), (Id: 2, Kind: null)
            } v
            """;

        using var compiled = Compile(query);
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            result,
            ("Id", typeof(int)),
            ("Kind", typeof(string)),
            ("Label", typeof(string))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(result, [1, "a", "a"], [2, null, "missing"]);
    }

    [TestMethod]
    [DataRow("v.* exclude (Ignored)", "Id", 1)]
    [DataRow("v.* exclude (Ignored) replace (v.Id + 1 as Id)", "Id", 2)]
    [DataRow("v.* exclude (Ignored) rename (Id as Key)", "Key", 1)]
    public void DistinctWildcard_WithModifiers_ShouldPreserveExportedColumns(
        string projection,
        string idName,
        int expectedId
    )
    {
        var query = $$"""
            SELECT DISTINCT {{projection}}
            FROM values {(Id: 1, Name: 'a', Ignored: 10), (Id: 1, Name: 'a', Ignored: 20)} v
            """;

        using var compiled = Compile(query);
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result, (idName, typeof(int)), ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, [expectedId, "a"]);
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public void GroupByThirdJoinedSource_DuplicateColumnNames_ShouldKeepRightValuesAndMultiplicity(
        bool hashJoin,
        bool parallel
    )
    {
        const string query = """
            SELECT r.Name, Count(*) AS Matches
            FROM values {(Id: 1, Name: 'left'), (Id: 2, Name: 'left-other')} m
            INNER JOIN values {(Id: 1, Name: 'middle'), (Id: 2, Name: 'middle-other')} p ON m.Id = p.Id
            INNER JOIN values {
                (Id: 1, Name: 'right'), (Id: 1, Name: 'right'), (Id: 2, Name: 'right-other')
            } r ON m.Id = r.Id
            GROUP BY r.Name
            """;
        var options = new CompilationOptions(
            parallelizationMode: parallel ? ParallelizationMode.Full : ParallelizationMode.None,
            useHashJoin: hashJoin,
            useSortMergeJoin: false,
            usePrimitiveTypeValidation: false
        );

        using var compiled = Compile(query, options);
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result, ("Name", typeof(string)), ("Matches", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, ["right", 2L], ["right-other", 1L]);
    }

    [TestMethod]
    public void GroupByFourthJoinedSource_MultipleKeys_ShouldPreserveNullKeyAndMultiplicity()
    {
        const string query = """
            SELECT r.Name, r.Kind, Count(*) AS Matches
            FROM values {(Id: 1, Name: 'first')} m
            INNER JOIN values {(Id: 1, Name: 'second')} p ON m.Id = p.Id
            INNER JOIN values {(Id: 1, Name: 'third')} q ON m.Id = q.Id
            INNER JOIN values {
                (Id: 1, Name: 'fourth', Kind: null), (Id: 1, Name: 'fourth', Kind: null),
                (Id: 1, Name: 'fourth', Kind: 'known')
            } r ON m.Id = r.Id
            GROUP BY r.Name, r.Kind
            """;

        using var compiled = Compile(query);
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            result,
            ("Name", typeof(string)),
            ("Kind", typeof(string)),
            ("Matches", typeof(long))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(result, ["fourth", null, 2L], ["fourth", "known", 1L]);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void NestedCollectionApply_ReusedAliasesAcrossCtes_ShouldKeepEachDeclaration(bool parallel)
    {
        const string query = """
            WITH arrays AS (
                SELECT p.Array AS Items FROM #A.Entities() s CROSS APPLY s.Children p
            ), names AS (
                SELECT p.Name AS Name FROM #A.Entities() s CROSS APPLY s.Children p
            )
            SELECT p.Value, n.Name
            FROM arrays a CROSS APPLY a.Items p
            INNER JOIN names n ON n.Name = 'child1'
            """;
        var options = new CompilationOptions(useCteParallelization: parallel, usePrimitiveTypeValidation: false);

        using var compiled = Compile(query, options);
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result, ("Value", typeof(int)), ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            result,
            [0, "child1"],
            [1, "child1"],
            [2, "child1"],
            [0, "child1"],
            [1, "child1"],
            [2, "child1"]
        );
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void NestedCollectionApply_ReusedAliasWithEmptyInput_ShouldRetainColumnContract(bool parallel)
    {
        const string query = """
            WITH parents AS (
                SELECT p.Array AS Items FROM #A.Entities() s CROSS APPLY s.Children p
            )
            SELECT p.Value FROM parents q CROSS APPLY q.Items p
            """;
        var options = new CompilationOptions(useCteParallelization: parallel, usePrimitiveTypeValidation: false);

        using var compiled = Compile(query, options, parents: []);
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result, ("Value", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(result);
    }

    [TestMethod]
    public void NestedOuterApply_ReusedAlias_ShouldRetainNullableColumnContract()
    {
        const string query = """
            WITH parents AS (
                SELECT p.Array AS Items FROM #A.Entities() s CROSS APPLY s.Children p
            )
            SELECT p.Value FROM parents q OUTER APPLY q.Items p
            """;

        using var compiled = Compile(query);
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result, ("Value", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, [0], [1], [2], [0], [1], [2]);
    }

    [TestMethod]
    public void CaseExpression_BranchValues_ShouldPreserveLiteralConstantFallbackAndNull()
    {
        const string query = """
            SELECT t.Id, CASE WHEN t.IsLiteral THEN t.Token
                             WHEN t.ConstantKnown THEN t.ConstantValue ELSE t.Token END AS Sql
            FROM values {
                (Id: 1, IsLiteral: true, ConstantKnown: true, Token: 'literal', ConstantValue: 'ignored'),
                (Id: 2, IsLiteral: false, ConstantKnown: true, Token: 'constant', ConstantValue: 'resolved'),
                (Id: 3, IsLiteral: false, ConstantKnown: false, Token: 'fallback', ConstantValue: 'ignored'),
                (Id: 4, IsLiteral: false, ConstantKnown: true, Token: 'constant', ConstantValue: null)
            } t
            ORDER BY t.Id
            """;

        using var compiled = Compile(query);
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result, ("Id", typeof(int)), ("Sql", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            result,
            [1, "literal"],
            [2, "resolved"],
            [3, "fallback"],
            [4, null]
        );
    }

    [TestMethod]
    public void NestedCollectionApply_ReusedAliasWithOrdinality_ShouldRetainDeclaredOrdinalColumn()
    {
        const string query = """
            WITH parents AS (
                SELECT p.Array AS Items FROM #A.Entities() s CROSS APPLY s.Children p
            )
            SELECT p.Value, p.Ordinal FROM parents q CROSS APPLY q.Items p WITH ORDINALITY
            """;

        using var compiled = Compile(query);
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result, ("Value", typeof(int)), ("Ordinal", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, [0, 0], [1, 1], [2, 2], [0, 0], [1, 1], [2, 2]);
    }

    [TestMethod]
    public void ChainedCollectionApply_CompoundInputScope_ShouldRetainNestedPropertyBinding()
    {
        const string query = """
            WITH numbers AS (
                SELECT a.Value
                FROM #A.Entities() s CROSS APPLY s.Children p CROSS APPLY p.Array a
            )
            SELECT p.Value FROM numbers p
            """;

        using var compiled = Compile(query);
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result, ("Value", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, [0], [1], [2], [0], [1], [2]);
    }
}
