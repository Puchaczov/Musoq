using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public class ArithmeticUnformattedTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("s.A-(1)")]
    [DataRow("s.A-ToInt32(1)")]
    public void SubtractionAfterQualifiedColumnAcceptsAdjacentRightOperand(string expression)
    {
        var sql = $"select {expression} as N from values {{ (A:2) }} s";
        using var query = CreateAndRunVirtualMachine(sql, CreateSingleSource());
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(result, [1]);
    }

    [TestMethod]
    public void UnformattedMinus_ShouldBeProperlyRecognized()
    {
        TestMethodTemplate("1-1", 0);
    }
}
