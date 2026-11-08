using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class BinarySpecificationRegressionTests
{
    [TestMethod]
    public void SmallIntegralComputedArithmetic_ShouldPromoteWithoutNarrowing()
    {
        var table = RunBinary("binary Rec { Width: byte, Height: byte, Area: Width * Height };",
            "r.Area", [255, 2]);

        TableMaterializationTestHelper.AssertColumns(table, ("Area", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [510]);
    }

    [TestMethod]
    [DataRow("CASE WHEN Flag = 1 THEN 7 ELSE 9 END")]
    [DataRow("= CASE WHEN Flag = 1 THEN 7 ELSE 9 END")]
    [DataRow("(CASE WHEN Flag = 1 THEN 7 ELSE 9 END)")]
    [DataRow("CASE Flag WHEN 1 THEN 7 ELSE 9 END")]
    public void SchemaCaseExpression_ShouldChooseExpectedArm(string expression)
    {
        var table = RunBinary($"binary Rec {{ Flag: byte, Derived: {expression} }};", "r.Derived", [1], [0]);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [7], [9]);
    }

    [TestMethod]
    public void SchemaNotExpression_ShouldPreserveUnknownAndPrecedence()
    {
        var table = RunBinary("binary Rec { Flag: byte, Optional: int le when Flag = 1, Enabled: NOT Optional = 0, Value: byte when NOT (Optional = 0) AND Flag = 1, Tail: byte };",
            "r.Enabled, r.Value, r.Tail", [1, 2, 0, 0, 0, 7, 99], [0, 99]);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [true, (byte)7, (byte)99], [null, null, (byte)99]);
    }

    [TestMethod]
    public void SchemaCaseExpression_ShouldNotEvaluateUnselectedArm()
    {
        var table = RunBinary("binary Rec { Flag: byte, Zero: byte, Derived: CASE WHEN Flag = 1 THEN 7 ELSE 1 / Zero END };",
            "r.Derived", [1, 0]);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [7]);
    }

    [TestMethod]
    public void SchemaCaseSizeAndPositionAndCheck_ShouldUseExpressions()
    {
        var table = RunBinary("binary Rec { Flag: byte, Values: byte[CASE WHEN Flag = 1 THEN 2 ELSE 1 END], Tail: byte at CASE WHEN Flag = 1 THEN 3 ELSE 2 END check CASE WHEN Tail = 99 THEN true ELSE false END };",
            "r.Values[0], r.Tail", [1, 7, 8, 99], [0, 9, 99]);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)7, (byte)99], [(byte)9, (byte)99]);
    }
}
