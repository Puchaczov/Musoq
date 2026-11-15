using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class CaseCteNullabilityTests : BasicEntityTestBase
{
    private const string Inputs = """
        with Data as (
            select v.Name, v.Ddl
            from values {
                (Name: 'literal', Ddl: 'ABC'),
                (Name: 'missing', Ddl: null),
                (Name: 'parsed', Ddl: 'ABC')
            } v
        )
        """;

    [TestMethod]
    [DataRow("case when v.Value is null then 0 else v.Value end")]
    [DataRow("case when v.Value is not null then v.Value else 0 end")]
    public void CaseNullGuardOnDeclaredNullableColumn_ShouldPreserveNullableMetadata(string expression)
    {
        var sql = $"select {expression} as Amount from values {{ (Value: null), (Value: 7) }} v order by Amount";

        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Amount", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [0], [7]);
    }

    [TestMethod]
    public void RecursiveCaseWithPresentBranches_ShouldRetainNonNullableAnchorType()
    {
        const string sql = """
            with recursive Cycle (Id) as (
                select v.Id from values { (Id: 1) } v
                union select case when c.Id = 1 then 2 else 1 end from Cycle c
            )
            select c.Id from Cycle c order by c.Id
            """;

        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Id", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1], [2]);
    }

    [TestMethod]
    [DataRow(false, false, false)]
    [DataRow(false, true, false)]
    [DataRow(false, true, true)]
    [DataRow(true, false, false)]
    [DataRow(true, true, false)]
    [DataRow(true, true, true)]
    public void JoinedCteCaseWithNullableScalar_ShouldPreserveNullableResult(
        bool nullableFirst,
        bool sidecarIndexes,
        bool parallelCtes
    )
    {
        var expression = nullableFirst
            ? "case when d.Name <> 'literal' then Length(RegexMatches('[A-Z]+', d.Ddl)) else 3 end"
            : "case when d.Name = 'literal' then 3 else Length(RegexMatches('[A-Z]+', d.Ddl)) end";
        var sql =
            Inputs
            + $$"""
                , Counts as (select d.Name, Count(1) as Rows from Data d group by d.Name),
                ColumnValues as (select d.Name, {{expression}} as Amount from Data d)
                select c.Name, c.Rows, n.Amount
                from Counts c inner join ColumnValues n on c.Name = n.Name
                order by c.Name
                """;
        var options = new CompilationOptions(
            parallelizationMode: parallelCtes ? ParallelizationMode.Full : ParallelizationMode.None,
            useHashJoin: true,
            useSortMergeJoin: false,
            usePrimitiveTypeValidation: false,
            useCteParallelization: parallelCtes,
            useCteSidecarIndexes: sidecarIndexes
        );

        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>(), options);
        using var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Rows", typeof(long)),
            ("Amount", typeof(int?))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["literal", 1L, 3],
            ["missing", 1L, null],
            ["parsed", 1L, 1]
        );
    }

    [TestMethod]
    [DataRow("case when d.Name = 'literal' then 3 else Length(RegexMatches('[A-Z]+', d.Ddl)) end")]
    [DataRow("case when d.Name = 'literal' then 3 else null end")]
    [DataRow("case when d.Name = 'literal' then 3 end")]
    public void CaseNullableResultThroughRepeatedCte_ShouldPreserveValuesAndMetadata(string expression)
    {
        var sql =
            Inputs
            + $$"""
                , Results as (select d.Name, {{expression}} as Amount from Data d)
                select a.Name, a.Amount
                from Results a inner join Results b on a.Name = b.Name
                where a.Name <> 'parsed'
                order by a.Name
                """;

        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Amount", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["literal", 3], ["missing", null]);
    }

    [TestMethod]
    public void CaseNullableNumericPromotion_ShouldRetainWidenedNullableType()
    {
        var sql =
            Inputs
            + """
                , Results as (
                    select d.Name,
                        case when d.Name = 'literal' then 3l else Length(RegexMatches('[A-Z]+', d.Ddl)) end as Amount
                    from Data d
                )
                select a.Name, a.Amount from Results a inner join Results b on a.Name = b.Name order by a.Name
                """;

        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Amount", typeof(long?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["literal", 3L], ["missing", null], ["parsed", 1L]);
    }
}
