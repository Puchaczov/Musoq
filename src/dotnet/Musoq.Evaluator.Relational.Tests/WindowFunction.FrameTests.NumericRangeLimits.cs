using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class WindowFunctionFrameTests
{
    [TestMethod]
    [DataRow("double", "1e100", "1 preceding and current row", 2)]
    [DataRow("double", "1e100", "1 following and unbounded following", null)]
    [DataRow("decimal", "79228162514264337593543950335", "1 following and unbounded following", null)]
    [DataRow("decimal", "79228162514264337593543950335", "current row and 1 following", 2)]
    public void BoundedRangeNearNumericLimits_ShouldRespectInclusiveBounds(
        string type,
        string key,
        string bounds,
        int? expected
    )
    {
        var sql =
            $"select Sum(N) over (order by K::{type} range between {bounds}) as S from values {{ (K: '{key}', N: 2) }} v";
        using var compiled = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(result, [expected]);
    }

    [TestMethod]
    public void ZeroWidthNumericRange_ShouldPreserveDistinctDoubleKeys()
    {
        const string sql = """
            select N, Sum(N) over (order by K::double range between 0 preceding and 0 following) as S
            from values { (K: '1e-100', N: 1), (K: '2e-100', N: 2) } v order by N
            """;
        using var compiled = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(result, [1, 1], [2, 2]);
    }

    [TestMethod]
    [DataRow("double", "-1e-100", "1")]
    [DataRow("decimal", "-0.0000000000000000000000000001", "2147483647")]
    public void BoundedRange_ShouldExcludeKeysJustPastTheExactBoundary(string type, string first, string offset)
    {
        var sql = $$"""
            select N, Sum(N) over (order by K::{{type}} range between current row and {{offset}} following) as S
            from values { (K: '{{first}}', N: 1), (K: '{{offset}}', N: 2) } v order by N
            """;
        using var compiled = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(result, [1, 1], [2, 2]);
    }
}
