using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class NullableBytesConversionTests : PluginsTestBase
{
    [TestMethod]
    public void ToBytes_WhenPresent_ShouldUseTheUnderlyingTypeWidth()
    {
        CollectionAssert.AreEqual(BitConverter.GetBytes(true), Library.ToBytes((bool?)true));
        CollectionAssert.AreEqual(BitConverter.GetBytes(short.MinValue), Library.ToBytes((short?)short.MinValue));
        CollectionAssert.AreEqual(BitConverter.GetBytes(ushort.MaxValue), Library.ToBytes((ushort?)ushort.MaxValue));
        CollectionAssert.AreEqual(BitConverter.GetBytes(int.MinValue), Library.ToBytes((int?)int.MinValue));
        CollectionAssert.AreEqual(BitConverter.GetBytes(uint.MaxValue), Library.ToBytes((uint?)uint.MaxValue));
        CollectionAssert.AreEqual(BitConverter.GetBytes(long.MinValue), Library.ToBytes((long?)long.MinValue));
        CollectionAssert.AreEqual(BitConverter.GetBytes(ulong.MaxValue), Library.ToBytes((ulong?)ulong.MaxValue));
    }

    [TestMethod]
    public void ToBytes_WhenMissing_ShouldReturnNullForEachWidth()
    {
        Assert.IsNull(Library.ToBytes((bool?)null));
        Assert.IsNull(Library.ToBytes((short?)null));
        Assert.IsNull(Library.ToBytes((ushort?)null));
        Assert.IsNull(Library.ToBytes((int?)null));
        Assert.IsNull(Library.ToBytes((uint?)null));
        Assert.IsNull(Library.ToBytes((long?)null));
        Assert.IsNull(Library.ToBytes((ulong?)null));
    }

    [TestMethod]
    public void GetBytes_WithNullableBounds_ShouldKeepUtf8EncodingAndRangeValidation()
    {
        CollectionAssert.AreEqual("é"u8.ToArray(), Library.GetBytes("ABéZ", (int?)1, (int?)2));
        Assert.Throws<ArgumentOutOfRangeException>(() => Library.GetBytes("abc", (int?)4, (int?)0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Library.GetBytes("abc", (int?)1, (int?)-1));
    }
}
