using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class BinarySpecificationRegressionTests
{
    [TestMethod]
    public void SchemaIntegerSuffixes_ShouldPreserveUnsignedMaximumAndResultTypes()
    {
        var table = RunBinary("binary Rec { A: 4294967295ui, B: 18446744073709551615ul };", "r.A, r.B", System.Array.Empty<byte>());
        TableMaterializationTestHelper.AssertRowsInOrder(table, [uint.MaxValue, ulong.MaxValue]);
    }

    [TestMethod]
    public void ExplicitFloatingCastsWithDecimalLiterals_ShouldPreserveDecimalArithmetic()
    {
        var table = RunBinary("binary Rec { F: float le, D: double le, Sum: F::Decimal + 0.5, Product: D::Decimal * 0.5, Difference: D::Decimal - 0.05, Quotient: F::Decimal / 0.25, Remainder: F::Decimal % 0.3 };",
            "r.Sum, r.Product, r.Difference, r.Quotient, r.Remainder",
            [0, 0, 0, 0x3F, 0, 0, 0, 0, 0, 0, 0xD0, 0x3F]);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1m, 0.125m, 0.2m, 2m, 0.2m]);
    }

    [TestMethod]
    public void ConditionalFloatingArithmetic_ShouldPreserveNullAfterExplicitDecimalCast()
    {
        var table = RunBinary("binary Rec { Flag: byte, F: float le when Flag = 1, Sum: F::Decimal + 0.5 };",
            "r.Sum", [0], [1, 0, 0, 0, 0x3F]);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [null], [1m]);
    }
}
