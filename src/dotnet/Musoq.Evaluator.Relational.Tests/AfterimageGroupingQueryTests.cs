using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

[TestClass]
[TestCategory("AfterimageRepro")]
public sealed class AfterimageGroupingQueryTests
{
    private const string LeftJoinCase = "case when r.Id is null then 0 else 1 end";

    [TestMethod]
    [DataRow(LeftJoinCase)]
    [DataRow("Status")]
    public void LeftJoin_ShouldGroupByCaseExpressionOrItsAlias(string groupKey)
    {
        var sql = $$"""
            select {{LeftJoinCase}} as Status, Count(*) as N
            from values { (Id: 1), (Id: 2) } l
            left join values { (Id: 2) } r on l.Id = r.Id
            group by {{groupKey}}
            """;

        using var compiled = InstanceCreator.CompileForExecution(
            sql,
            nameof(LeftJoin_ShouldGroupByCaseExpressionOrItsAlias),
            new GenericSchemaProvider(new Dictionary<string, ISchema>()),
            new TestsLoggerResolver()
        );
        using var result = compiled.Run();

        TableMaterializationTestHelper.AssertRowsUnordered(result, [0, 1L], [1, 1L]);
    }

    [TestMethod]
    [DataRow("", "values { (Id: 1), (Id: 2) }", "values { (Id: 2), (Id: 3) }")]
    [DataRow(
        """
            with left_rows as (select v.Id from values { (Id: 1), (Id: 2) } v),
                 right_rows as (select w.Id from values { (Id: 2), (Id: 3) } w)
            """,
        "left_rows",
        "right_rows"
    )]
    [DataRow(
        """
            with left_rows as (select r.Id from values { (Id: 1), (Id: 2) } r),
                 right_rows as (select l.Id from values { (Id: 2), (Id: 3) } l)
            """,
        "left_rows",
        "right_rows"
    )]
    public void FullJoin_ShouldGroupByRowPresenceWithInlineOrCteInputs(
        string declarations,
        string leftSource,
        string rightSource
    )
    {
        var sql = $$"""
            {{declarations}}
            select case
                when l is missing then 'right'
                when r is missing then 'left'
                else 'both'
            end as Side, Count(*) as N
            from {{leftSource}} l full join {{rightSource}} r on l.Id = r.Id
            group by Side
            """;

        using var compiled = InstanceCreator.CompileForExecution(
            sql,
            nameof(FullJoin_ShouldGroupByRowPresenceWithInlineOrCteInputs),
            new GenericSchemaProvider(new Dictionary<string, ISchema>()),
            new TestsLoggerResolver()
        );
        using var result = compiled.Run();

        TableMaterializationTestHelper.AssertRowsUnordered(result, ["left", 1L], ["both", 1L], ["right", 1L]);
    }

    [TestMethod]
    public void FullJoin_WhenCteRowHasOnlyNullColumns_ShouldStillGroupItAsPresent()
    {
        const string sql = """
            with left_rows as (select v.Id from values { (Id: null), (Id: 2) } v),
                 right_rows as (select w.Id from values { (Id: 2) } w)
            select case when l is missing then 'right' when r is missing then 'left' else 'both' end as Side,
                   Count(*) as N
            from left_rows l full join right_rows r on l.Id = r.Id
            group by Side
            """;

        using var compiled = InstanceCreator.CompileForExecution(
            sql,
            nameof(FullJoin_WhenCteRowHasOnlyNullColumns_ShouldStillGroupItAsPresent),
            new GenericSchemaProvider(new Dictionary<string, ISchema>()),
            new TestsLoggerResolver()
        );
        using var result = compiled.Run();

        TableMaterializationTestHelper.AssertRowsUnordered(result, ["left", 1L], ["both", 1L]);
    }
}
