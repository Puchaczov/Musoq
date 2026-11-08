using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class BinarySpecificationRegressionTests
{
    [TestMethod]
    [DataRow("byte", false)]
    [DataRow("short le", false)]
    [DataRow("int le", false)]
    [DataRow("float le", false)]
    [DataRow("double le", false)]
    [DataRow("byte", true)]
    [DataRow("short le", true)]
    [DataRow("int le", true)]
    [DataRow("float le", true)]
    [DataRow("double le", true)]
    public void NestedConditionalNumericLeaf_ShouldPreserveValuesNullsAndType(string type, bool coalesce)
    {
        var fixture = NumericFixture(type);
        var expression = coalesce ? "Coalesce(r.Y.Z)" : "r.Y.Z";
        var table = RunBinary($"binary Child {{ Flag: byte, Z: {type} when Flag = 1 }}; binary Rec {{ Y: Child }};",
            $"{expression} as Value", fixture.Present, fixture.Zero, [0]);

        TableMaterializationTestHelper.AssertColumns(table, ("Value", fixture.Type));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [fixture.Value], [fixture.ZeroValue], [null]);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AbsentConditionalRecordPath_ShouldYieldNull(bool coalesce)
    {
        var expression = coalesce ? "Coalesce(r.Y.Deep.Z)" : "r.Y.Deep.Z";
        var table = RunBinary("binary Leaf { Z: int le }; binary Child { Deep: Leaf }; binary Rec { Flag: byte, Y: Child when Flag = 1 };",
            $"{expression} as Value", [1, 42, 0, 0, 0], [0]);

        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [42], [null]);
    }

    private static (byte[] Present, byte[] Zero, object Value, object ZeroValue, Type Type) NumericFixture(string type) => type switch
    {
        "byte" => ([1, 42], [1, 0], (byte)42, (byte)0, typeof(byte?)),
        "short le" => ([1, 42, 0], [1, 0, 0], (short)42, (short)0, typeof(short?)),
        "int le" => ([1, 42, 0, 0, 0], [1, 0, 0, 0, 0], 42, 0, typeof(int?)),
        "float le" => ([1, 0, 0, 192, 63], [1, 0, 0, 0, 0], 1.5f, 0f, typeof(float?)),
        "double le" => ([1, 0, 0, 0, 0, 0, 0, 248, 63], [1, 0, 0, 0, 0, 0, 0, 0, 0], 1.5d, 0d, typeof(double?)),
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
}
