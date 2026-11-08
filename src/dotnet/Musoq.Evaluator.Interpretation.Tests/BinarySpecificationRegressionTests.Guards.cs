using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class BinarySpecificationRegressionTests
{
    [TestMethod]
    public void ComputedConditionalBooleanGuard_ShouldPreserveUnknownAndCursor()
    {
        var table = RunBinary("binary Rec { Flag: byte, Optional: int le when Flag = 1, Enabled: Optional > 0, Value: byte when Enabled, Tail: byte };",
            "r.Enabled, r.Value, r.Tail", [1, 2, 0, 0, 0, 7, 99], [1, 0, 0, 0, 0, 99], [0, 99]);

        TableMaterializationTestHelper.AssertColumns(table, ("Enabled", typeof(bool?)), ("Value", typeof(byte?)), ("Tail", typeof(byte)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [true, (byte)7, (byte)99], [false, null, (byte)99], [null, null, (byte)99]);
    }

    [TestMethod]
    public void TransitiveConditionalBooleanGuard_ShouldPreserveUnknown()
    {
        var table = RunBinary("binary Rec { Flag: byte, Optional: int le when Flag = 1, Derived: Optional + 1, Enabled: Derived > 1, Value: byte when Enabled, Tail: byte };",
            "r.Derived, r.Enabled, r.Value, r.Tail", [1, 2, 0, 0, 0, 7, 99], [0, 99]);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [3, true, (byte)7, (byte)99], [null, null, null, (byte)99]);
    }

    [TestMethod]
    [DataRow("Optional > 0")]
    [DataRow("Enabled = true")]
    [DataRow("Enabled")]
    public void EquivalentConditionalGuards_ShouldSkipUnknown(string guard)
    {
        var table = RunBinary($"binary Rec {{ Flag: byte, Optional: int le when Flag = 1, Enabled: Optional > 0, Value: byte when {guard}, Tail: byte }};",
            "r.Value, r.Tail", [0, 99]);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [null, (byte)99]);
    }

    [TestMethod]
    [DataRow("NOT Flag")]
    [DataRow("Flag AND true")]
    [DataRow("CASE WHEN Flag THEN 1 ELSE 0 END = 1")]
    public void NonBooleanLogicalOperand_ShouldReportConstraintDiagnostic(string guard)
    {
        var exception = Assert.Throws<Musoq.Converter.Exceptions.MusoqQueryException>(() =>
            RunBinary($"binary Rec {{ Flag: byte, Value: byte when {guard} }};", "r.Value", [1, 7]));

        StringAssert.Contains(exception.Message, "MQ4006");
    }

    [TestMethod]
    public void NonBooleanGuard_ShouldReportConstraintDiagnostic()
    {
        var exception = Assert.Throws<Musoq.Converter.Exceptions.MusoqQueryException>(() =>
            RunBinary("binary Rec { Flag: byte, Value: byte when Flag };", "r.Value", [1, 7]));

        StringAssert.Contains(exception.Message, "MQ4006");
    }
}
