using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class PatternPredicateNullLiteralTests : BasicEntityTestBase
{
    private const string Source =
        "from values {(Id: 1, Txt: 'x'), (Id: 2, Txt: null), (Id: 3, Txt: 'X'), (Id: 4, Txt: 'other')} s";

    [TestMethod]
    [DataRow("like", false, new[] { true, false, true, false }, new[] { 1, 3 })]
    [DataRow("not like", true, new[] { false, true, false, true }, new[] { 2, 4 })]
    [DataRow("rlike", false, new[] { true, false, false, false }, new[] { 1 })]
    [DataRow("not rlike", true, new[] { false, true, true, true }, new[] { 2, 3, 4 })]
    public void LiteralAndColumnOperands_ShouldProduceExpectedBooleansAndFilteredRows(
        string operation,
        bool expectedForNull,
        bool[] columnResults,
        int[] filteredIds
    )
    {
        var sql =
            $"select Id, null {operation} 'x' as NullValue, Txt {operation} null as NullPattern, "
            + $"null {operation} null as BothNull, Txt {operation} 'x' as ColumnValue {Source} order by Id";
        var schemas = new Dictionary<string, IEnumerable<BasicEntity>>();
        using var projection = CreateAndRunVirtualMachine(sql, schemas);
        using var values = projection.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(
            values,
            ("Id", typeof(int)),
            ("NullValue", typeof(bool)),
            ("NullPattern", typeof(bool)),
            ("BothNull", typeof(bool)),
            ("ColumnValue", typeof(bool))
        );
        var expectedRows = columnResults
            .Select(
                (value, index) => new object?[] { index + 1, expectedForNull, expectedForNull, expectedForNull, value }
            )
            .ToArray();
        TableMaterializationTestHelper.AssertRowsInOrder(values, expectedRows);

        var filter =
            $"select Id {Source} where case when Id = 2 then null {operation} 'x' "
            + $"else Txt {operation} 'x' end order by Id";
        using var query = CreateAndRunVirtualMachine(filter, schemas);
        using var filtered = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsInOrder(
            filtered,
            filteredIds.Select(id => new object?[] { id }).ToArray()
        );
    }
}
