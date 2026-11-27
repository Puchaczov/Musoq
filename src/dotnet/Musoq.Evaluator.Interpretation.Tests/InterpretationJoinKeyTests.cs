using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class InterpretationJoinKeyTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("binary A { V: byte }; binary R { D: A[1] };", "Interpret<R>(FromHexToBytes('2a'))", "[0].V")]
    [DataRow("text A { V: chars[1] }; text R { D: A };", "Parse<R>('a')", ".V")]
    [DataRow(
        "text A { V: chars[1] }; text B { V: chars[1] }; text R { D: switch { pattern 'a' => A, _ => B } };",
        "Parse<R>('a')",
        ".V"
    )]
    public void JoiningOnMemberOfReusedDecodedCte_ShouldPreserveAllMatchingPairs(
        string declarations,
        string call,
        string path
    )
    {
        var sql = $$"""
            {{declarations}}
            with P as (select s.K, r.D as X from values { (K:1), (K:2) } s cross apply {{call}} r)
            select p.K, q.K from P p inner join P q on p.X{{path}} = q.X{{path}}
            """;

        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(table, [1, 1], [1, 2], [2, 1], [2, 2]);
    }
}
