using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class BinarySpecificationRegressionTests
{
    [TestMethod]
    public void RecordCase_ShouldSelectBetweenDistinctValuesOfTheSameSchemaType()
    {
        var table = RunBinary("binary Child { Value: byte }; binary Rec { Flag: byte, Left: Child, Right: Child, Selected: = CASE WHEN Flag = 1 THEN Left ELSE Right END };",
            "r.Selected.Value", [0, 10, 20], [1, 30, 40]);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)20], [(byte)30]);
    }

    [TestMethod]
    public void ComputedRecordAlias_ShouldPreserveItsShapeAndConditionalNull()
    {
        var table = RunBinary("binary Child { Value: byte }; binary Rec { Flag: byte, Child: Child, Copy: = Child, Maybe: = CASE WHEN Flag = 1 THEN Copy ELSE NULL END };",
            "r.Copy.Value, r.Maybe.Value", [0, 10], [1, 20]);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)10, null], [(byte)20, (byte)20]);
    }

    [TestMethod]
    public void ComputedArrayElement_ShouldKeepItsPrimitiveTypeAndSizeSemantics()
    {
        var table = RunBinary("binary Rec { Header: byte[1], Count: = Header[0], Values: byte[Count] };",
            "r.Count, r.Values.Length", [2, 10, 20]);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)2, 2]);
    }
}
