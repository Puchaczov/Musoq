using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public sealed class ByteBufferSnapshotTests
{
    [TestMethod]
    public void BoundedSnapshot_ShouldOwnItsBytesAndRetainNullAndEmptySemantics()
    {
        byte[] input = [1, 2, 3, 4];
        var copy = ByteBufferSnapshot.Copy(input, 2);
        input[0] = 9;
        CollectionAssert.AreEqual(new byte[] { 1, 2 }, copy);
        Assert.IsNull(ByteBufferSnapshot.Copy(null, 2));
        Assert.HasCount(0, ByteBufferSnapshot.Copy([], 2)!);
        Assert.IsNull(ByteBufferSnapshot.CopyOrNull([], 2));
        Assert.HasCount(0, ByteBufferSnapshot.CopyOrNull(input, 0)!);
    }

    [TestMethod]
    public void NullableRange_ShouldEvaluateToNullWithoutValidatingMissingOrNullInput()
    {
        Assert.IsNull(ByteBufferSnapshot.Copy(null, -1, -1, 1));
        Assert.IsNull(ByteBufferSnapshot.Copy([1], null, -1, 1));
        Assert.IsNull(ByteBufferSnapshot.Copy([1], -1, null, 1));
        CollectionAssert.AreEqual(new byte[] { 2, 3 }, ByteBufferSnapshot.Copy([1, 2, 3, 4], 1, 3, 2));
    }

    [TestMethod]
    [DataRow(-1, 1)]
    [DataRow(0, -1)]
    [DataRow(3, 1)]
    [DataRow(1, int.MaxValue)]
    [DataRow(int.MaxValue, int.MaxValue)]
    public void InvalidRange_ShouldPreservePlatformCodecDiagnosticsBeforeApplyingReadBound(int offset, int length)
    {
        byte[] input = [1, 2];
        var original = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            Convert.ToBase64String(input, offset, length)
        );
        var optimized = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            ByteBufferSnapshot.Copy(input, offset, length, 0)
        );
        Assert.AreEqual(original.ParamName, optimized.ParamName);
        Assert.AreEqual(original.Message, optimized.Message);
    }
}
