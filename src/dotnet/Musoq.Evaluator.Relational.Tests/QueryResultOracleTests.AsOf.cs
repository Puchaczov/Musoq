using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public sealed partial class QueryResultOracleTests
{
    [TestMethod]
    [DataRow(7, ">=", "asof join", 12)]
    [DataRow(19, ">=", "asof join", 12)]
    [DataRow(43, ">=", "asof join", 12)]
    [DataRow(97, ">=", "asof join", 12)]
    [DataRow(997, ">=", "asof join", 12)]
    [DataRow(2026, ">=", "asof join", 12)]
    [DataRow(7, ">", "asof join", 12)]
    [DataRow(19, ">", "asof join", 12)]
    [DataRow(43, ">", "asof join", 12)]
    [DataRow(97, ">", "asof join", 12)]
    [DataRow(997, ">", "asof join", 12)]
    [DataRow(2026, ">", "asof join", 12)]
    [DataRow(7, "<=", "asof join", 12)]
    [DataRow(19, "<=", "asof join", 12)]
    [DataRow(43, "<=", "asof join", 12)]
    [DataRow(97, "<=", "asof join", 12)]
    [DataRow(997, "<=", "asof join", 12)]
    [DataRow(2026, "<=", "asof join", 12)]
    [DataRow(7, "<", "asof join", 12)]
    [DataRow(19, "<", "asof join", 12)]
    [DataRow(43, "<", "asof join", 12)]
    [DataRow(97, "<", "asof join", 12)]
    [DataRow(997, "<", "asof join", 12)]
    [DataRow(2026, "<", "asof join", 12)]
    [DataRow(7, ">=", "asof left join", 12)]
    [DataRow(19, ">=", "asof left join", 12)]
    [DataRow(43, ">=", "asof left join", 12)]
    [DataRow(97, ">=", "asof left join", 12)]
    [DataRow(997, ">=", "asof left join", 12)]
    [DataRow(2026, ">=", "asof left join", 12)]
    [DataRow(7, ">", "asof left join", 12)]
    [DataRow(19, ">", "asof left join", 12)]
    [DataRow(43, ">", "asof left join", 12)]
    [DataRow(97, ">", "asof left join", 12)]
    [DataRow(997, ">", "asof left join", 12)]
    [DataRow(2026, ">", "asof left join", 12)]
    [DataRow(7, "<=", "asof left join", 12)]
    [DataRow(19, "<=", "asof left join", 12)]
    [DataRow(43, "<=", "asof left join", 12)]
    [DataRow(97, "<=", "asof left join", 12)]
    [DataRow(997, "<=", "asof left join", 12)]
    [DataRow(2026, "<=", "asof left join", 12)]
    [DataRow(7, "<", "asof left join", 12)]
    [DataRow(19, "<", "asof left join", 12)]
    [DataRow(43, "<", "asof left join", 12)]
    [DataRow(97, "<", "asof left join", 12)]
    [DataRow(997, "<", "asof left join", 12)]
    [DataRow(2026, "<", "asof left join", 12)]
    [DataRow(7, ">=", "asof join", 0)]
    [DataRow(7, ">=", "asof left join", 0)]
    [DataRow(7, ">=", "asof join", 1)]
    [DataRow(7, ">=", "asof left join", 1)]
    public void AsOfMatching_ShouldMatchIndependentCandidateSearch(
        int seed,
        string comparison,
        string join,
        int rowCount
    )
    {
        var left = OracleAsOfInputs(seed, rowCount, 0);
        var right = OracleAsOfInputs(seed + 1, rowCount, 100);
        var expected = ExpectedAsOfRows(left, right, comparison, join);
        var sql = $"""
            select l.Id as LeftId, r.Id as RightId
            from #A.Entities() l
            {join} #B.Entities() r on l.City = r.City and l.NullableValue {comparison} r.NullableValue
            tie break by r.Id desc
            order by l.Id
            """;
        using var query = CreateAndRunVirtualMachine(sql, Sources(left, right));
        using var result = query.Run(TestContext.CancellationToken);
        var rightType = join == "asof left join" ? typeof(int?) : typeof(int);

        TableMaterializationTestHelper.AssertColumns(result, ("LeftId", typeof(int)), ("RightId", rightType));
        TableMaterializationTestHelper.AssertRowsInOrder(result, expected);
    }

    private BasicEntity[] OracleAsOfInputs(int seed, int count, int idOffset)
    {
        var rows = OracleFrameInputs(seed, count);
        foreach (var row in rows)
            row.Id += idOffset;
        if (count == 1)
            rows[0].NullableValue = 1;
        if (count > 10)
            rows[10].City = idOffset == 0 ? "left-only" : "right-only";
        TestContext.WriteLine(
            "ASOF seed={0}; offset={1}; input={2}",
            seed,
            idOffset,
            string.Join(
                ";",
                rows.Select(row => $"{row.Id},{row.City ?? "<null>"},{row.NullableValue?.ToString() ?? "<null>"}")
            )
        );
        return rows;
    }

    private static object?[][] ExpectedAsOfRows(BasicEntity[] left, BasicEntity[] right, string comparison, string join)
    {
        var expected = new List<object?[]>();
        foreach (var row in left.OrderBy(row => row.Id))
        {
            var candidates = right.Where(other =>
                row.City != null
                && other.City != null
                && row.City == other.City
                && row.NullableValue.HasValue
                && other.NullableValue.HasValue
                && (
                    comparison switch
                    {
                        ">=" => row.NullableValue.Value >= other.NullableValue.Value,
                        ">" => row.NullableValue.Value > other.NullableValue.Value,
                        "<=" => row.NullableValue.Value <= other.NullableValue.Value,
                        "<" => row.NullableValue.Value < other.NullableValue.Value,
                        _ => throw new System.ArgumentOutOfRangeException(nameof(comparison)),
                    }
                )
            );
            IOrderedEnumerable<BasicEntity> ordered;
            if (comparison is ">=" or ">")
                ordered = candidates
                    .OrderByDescending(other => other.NullableValue)
                    .ThenByDescending(other => other.Id);
            else
                ordered = candidates.OrderBy(other => other.NullableValue).ThenByDescending(other => other.Id);
            var match = ordered.FirstOrDefault();
            if (match != null)
                expected.Add([row.Id, match.Id]);
            else if (join == "asof left join")
                expected.Add([row.Id, null]);
        }
        return expected.ToArray();
    }
}
