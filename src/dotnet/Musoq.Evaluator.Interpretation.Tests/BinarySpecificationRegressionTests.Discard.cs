using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class BinarySpecificationRegressionTests
{
    [TestMethod]
    public void ConditionalDiscardPreservesCursorAndFailureField()
    {
        const string sql =
            "binary Rec { N: byte, _: byte[N] when N > 0, Tail: byte }; select r.IsSuccess, r.BytesConsumed, r.ErrorField, r.ParsedFields.Tail from #test.files() f cross apply PartialInterpret<Rec>(f.Content) r";
        using var result = RunBinaryQuery(sql, [0, 7], [2, 8, 9, 10], [3, 9]);

        Assert.AreEqual(3, result.Count);
        Assert.AreEqual((byte)7, result[0].GetValue<byte>(3));
        Assert.AreEqual((byte)10, result[1].GetValue<byte>(3));
        Assert.IsFalse(result[2].GetValue<bool>(0));
        Assert.AreEqual(1, result[2].GetValue<int>(1));
        Assert.AreEqual("_", result[2].GetValue<string>(2));
    }

    [TestMethod]
    public void DiscardWithMagicStillChecksThePayload()
    {
        const string sql =
            "binary Rec { _: byte[2] magic [0x41, 0x42], Tail: byte }; select r.IsSuccess, r.ErrorField from #test.files() f cross apply PartialInterpret<Rec>(f.Content) r";
        using var result = RunBinaryQuery(sql, [65, 66, 10], [65, 67, 10]);

        Assert.IsTrue(result[0].GetValue<bool>(0));
        Assert.IsFalse(result[1].GetValue<bool>(0));
        Assert.AreEqual("_", result[1].GetValue<string>(1));
    }
}
