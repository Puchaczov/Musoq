using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class BinarySpecificationRegressionTests
{
    [TestMethod]
    [DataRow("CASE WHEN Flag = 1 THEN 7 END")]
    [DataRow("CASE Flag WHEN 1 THEN 7 END")]
    [DataRow("CASE WHEN Flag = 1 THEN CASE WHEN Flag = 1 THEN 7 END END")]
    public void EmbeddedImplicitElse_ShouldReturnNullableComputedField(string expression)
    {
        var table = RunBinary($"binary Rec {{ Flag: byte, Derived: {expression} }};", "r.Derived", [1], [0]);

        TableMaterializationTestHelper.AssertColumns(table, ("Derived", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [7], [null]);
    }

    [TestMethod]
    public void EmbeddedUnmatchedImplicitElse_ShouldPreserveLazyThenEvaluation()
    {
        var table = RunBinary("binary Rec { Flag: byte, Zero: byte, Derived: CASE WHEN Flag = 1 THEN 1 / Zero END };",
            "r.Derived", [0, 0]);

        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { null });
    }
}
