using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Musoq.Plugins.Tests;

[TestClass]
public class BytesTests : LibraryBaseBaseTests
{
    [TestMethod]
    public void GetBytesForString()
    {
        AssertLoop("abc"u8.ToArray(), Library.GetBytes("abc")!);
        AssertLoop(BitConverter.GetBytes('a'), Library.GetBytes('a')!);
        AssertLoop(BitConverter.GetBytes(5L), Library.GetBytes(5L)!);
        AssertLoop(BitConverter.GetBytes(true), Library.GetBytes(true)!);

        AssertLoop(decimal.GetBits(5m).SelectMany(f => BitConverter.GetBytes(f)).ToArray(), Library.GetBytes(5m)!);
    }

    private void AssertLoop(byte[] byte1, byte[] byte2)
    {
        Assert.HasCount(byte1.Length, byte2);

        for(var i = 0; i < byte1.Length; ++i)
        {
            Assert.AreEqual(byte1[i], byte2[i]);
        }
    }
}