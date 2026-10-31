using System.Text.Json;
using Musoq.Evaluator.KaitaiStruct.Tests.Infrastructure;

namespace Musoq.Evaluator.KaitaiStruct.Tests.Tests;

[TestClass]
public sealed class AssertionComparerTests
{
    [TestMethod]
    [DataRow(0.5, 0)]
    [DataRow(0.004999999999999999, 2)]
    [DataRow(5.0, -1)]
    [DataRow(double.Epsilon, 323)]
    public void Python_rounding_accepts_exact_ties_and_values_below_the_decimal_midpoint(double value, int places)
    {
        using var assertion = JsonDocument.Parse($"{{\"expected\":{{\"kind\":\"float\",\"value\":\"0.0\"}},\"places\":{places}}}");
        AssertionComparer.Compare(assertion.RootElement, value, "Python rounding");
    }

    [TestMethod]
    [DataRow(0.005, 2)]
    [DataRow(0.5000000000000001, 0)]
    [DataRow(5.000000000000001, -1)]
    [DataRow(double.Epsilon, 324)]
    public void Python_rounding_rejects_values_above_the_exact_decimal_midpoint(double value, int places)
    {
        using var assertion = JsonDocument.Parse($"{{\"expected\":{{\"kind\":\"float\",\"value\":\"0.0\"}},\"places\":{places}}}");
        Assert.Throws<AssertFailedException>(() => AssertionComparer.Compare(assertion.RootElement, value, "wrong rounded value"));
    }

    [TestMethod]
    public void Delta_comparison_rejects_an_outside_value()
    {
        using var assertion = JsonDocument.Parse("{\"expected\":{\"kind\":\"float\",\"value\":\"1.0\"},\"delta\":\"0.000001\"}");
        AssertionComparer.Compare(assertion.RootElement, 1.0000005d, "inside delta");
        Assert.Throws<AssertFailedException>(() => AssertionComparer.Compare(assertion.RootElement, 1.000002d, "outside delta"));
    }

    [TestMethod]
    public void Unsigned_maximum_is_preserved_and_a_nearby_wrong_value_is_rejected()
    {
        using var assertion = JsonDocument.Parse("{\"expected\":{\"kind\":\"integer\",\"value\":\"18446744073709551615\"}}");
        AssertionComparer.Compare(assertion.RootElement, ulong.MaxValue, "maximum");
        Assert.Throws<AssertFailedException>(() => AssertionComparer.Compare(assertion.RootElement, ulong.MaxValue - 1, "wrong"));
        Assert.Throws<AssertFailedException>(() => AssertionComparer.Compare(assertion.RootElement, (double)ulong.MaxValue, "lossy"));
    }

    [TestMethod]
    public void Bytes_are_compared_by_content_and_order()
    {
        using var assertion = JsonDocument.Parse("{\"expected\":{\"kind\":\"bytes\",\"value\":\"0102\"}}");
        AssertionComparer.Compare(assertion.RootElement, new byte[] { 1, 2 }, "bytes");
        Assert.Throws<AssertFailedException>(() => AssertionComparer.Compare(assertion.RootElement, new byte[] { 2, 1 }, "order"));
    }
}
