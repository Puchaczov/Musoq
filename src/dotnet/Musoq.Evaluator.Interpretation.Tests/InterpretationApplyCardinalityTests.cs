using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class InterpretationApplyCardinalityTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("text R { V: chars[1] };", "Parse<R>('a')", "TryParse<R>(s.S)", "a", "cross")]
    [DataRow("text R { V: chars[1] };", "Parse<R>('a')", "TryParse<R>(s.S)", "a", "outer")]
    [DataRow(
        "binary R { V: byte };",
        "Interpret<R>(FromHexToBytes('2a'))",
        "TryInterpret<R>(FromHexToBytes(s.S))",
        "2a",
        "cross"
    )]
    [DataRow(
        "binary R { V: byte };",
        "Interpret<R>(FromHexToBytes('2a'))",
        "TryInterpret<R>(FromHexToBytes(s.S))",
        "2a",
        "outer"
    )]
    public void ChainedInterpretationWithUnusedAlias_ShouldStillFilterOrPreserveRows(
        string declarations,
        string first,
        string second,
        string valid,
        string apply
    )
    {
        var sql = $$"""
            {{declarations}}
            select s.K from values { (K:1,S:'{{valid}}'), (K:0,S:''), (K:0,S:'') } s
            cross apply {{first}} a {{apply}} apply {{second}} r
            """;

        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var table = query.Run(TestContext.CancellationToken);
        object?[][] expected =
            apply == "outer"
                ?
                [
                    [1],
                    [0],
                    [0],
                ]
                :
                [
                    [1],
                ];
        TableMaterializationTestHelper.AssertRowsUnordered(table, expected);
    }
}
