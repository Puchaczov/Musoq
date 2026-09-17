using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class LikeAsciiProbeTests
{
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(8)]
    [DataRow(9)]
    [DataRow(64)]
    [DataRow(4096)]
    public void IsAsciiLikeSpan_WhenSpanIsAscii_ShouldReturnTrue(int length)
    {
        var value = new string('a', length);

        Assert.IsTrue(Operators.IsAsciiLikeSpan(value, 0, value.Length));
    }

    [TestMethod]
    [DataRow(1, 0)]
    [DataRow(8, 0)]
    [DataRow(8, 7)]
    [DataRow(9, 0)]
    [DataRow(9, 8)]
    [DataRow(64, 32)]
    [DataRow(4096, 4095)]
    public void IsAsciiLikeSpan_WhenSpanContainsNonAscii_ShouldReturnFalse(int length, int nonAsciiIndex)
    {
        var characters = new string('a', length).ToCharArray();
        characters[nonAsciiIndex] = 'Ż';

        Assert.IsFalse(Operators.IsAsciiLikeSpan(new string(characters), 0, length));
    }

    [TestMethod]
    public void IsAsciiLikeSpan_WhenNonAsciiIsOutsideSelectedSpan_ShouldInspectOnlySelectedSpan()
    {
        Assert.IsTrue(Operators.IsAsciiLikeSpan("ŻasciiŻ", 1, 5));
    }
}
