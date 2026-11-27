using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class InterpretationWindowElementTests : BasicEntityTestBase
{
    private const string BinaryDefinition = "binary A { V: byte }; binary R { D: A[1] };";
    private const string BinaryCall = "Interpret<R>(FromHexToBytes('2a'))";
    private const string TextDefinition = "text A { V: chars[1] }; text R { D: repeat A until end };";
    private const string TextCall = "Parse<R>('a')";
    private const string Switched =
        "text A { V: chars[1] }; text B { V: chars[1] }; text R { D: repeat switch { pattern 'a' => A, _ => B } until end };";

    [TestMethod]
    [DataRow(BinaryDefinition, BinaryCall, (byte)42)]
    [DataRow(TextDefinition, TextCall, "a")]
    public void WindowProjectionOfArrayElement_ShouldKeepItsDecodedMember(
        string definition,
        string call,
        object expected
    )
    {
        var sql = $$"""
            {{definition}}
            with P as (select RowNumber() over (order by s.K) as N, r.D[0] as X
                from values { (K:1), (K:2) } s cross apply {{call}} r)
            select p.N, p.X.V as V from P p
            """;

        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(table, [1L, expected], [2L, expected]);
    }

    [TestMethod]
    [DataRow(BinaryDefinition, BinaryCall, (byte)42)]
    [DataRow(TextDefinition, TextCall, "a")]
    [DataRow(Switched, TextCall, "a")]
    public void ImmediateMemberIndexedByWindowRank_ShouldReadTheElementOrNull(
        string definition,
        string call,
        object expected
    )
    {
        var sql = $$"""
            {{definition}}
            select r.D[ToInt32(RowNumber() over (order by s.K) - 1) ?? 0].V as V
            from values { (K:1), (K:2) } s cross apply {{call}} r
            """;

        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(table, [expected], [null]);
    }
}
