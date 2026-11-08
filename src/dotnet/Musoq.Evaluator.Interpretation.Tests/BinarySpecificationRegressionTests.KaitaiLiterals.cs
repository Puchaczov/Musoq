using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class BinarySpecificationRegressionTests
{
    [TestMethod]
    public void ComputedStringLiteralFields_ShouldAcceptOrdinaryAndRawForms()
    {
        var table = RunBinary("binary Rec { Ordinary: 'hello', Raw: r'${value}\\path' };",
            "r.Ordinary, r.Raw", System.Array.Empty<byte>());
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["hello", "${value}\\path"]);
    }

    [TestMethod]
    public void ComputedPrefixedIntegerLiteralFields_ShouldAcceptAllCoreBases()
    {
        var table = RunBinary("binary Rec { Hex: 0x2A, Bin: 0b101010, Oct: 0o52 };",
            "r.Hex, r.Bin, r.Oct", System.Array.Empty<byte>());
        TableMaterializationTestHelper.AssertRowsInOrder(table, [42, 42, 42]);
    }
}
