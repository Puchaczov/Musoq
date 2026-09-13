using BenchmarkDotNet.Attributes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class AsciiLikeSpanBenchmarkTests
{
    [TestMethod]
    public void BenchmarkMatrix_ShouldCoverScalarBoundaryLongInputsAndNonAsciiPositions()
    {
        CollectionAssert.AreEqual(
            new object[] { 1, 4, 8, 9, 16, 64, 256, 4096 },
            Parameters<AsciiLikeSpanBenchmark>(nameof(AsciiLikeSpanBenchmark.Length)));
        CollectionAssert.AreEqual(
            Enum.GetValues<AsciiProbeNonAsciiPosition>().Cast<object>().ToArray(),
            Parameters<AsciiLikeSpanBenchmark>(nameof(AsciiLikeSpanBenchmark.NonAsciiPosition)));
        Assert.IsFalse(typeof(AsciiLikeSpanBenchmark).IsSealed);
        Assert.IsFalse(typeof(AsciiLikeSpanIntrinsicDiagnosticBenchmark).IsSealed);
    }

    [TestMethod]
    public void BenchmarkKernels_AcrossMatrix_ShouldReturnIdenticalResults()
    {
        foreach (var length in new[] { 1, 4, 8, 9, 16, 64, 256, 4096 })
        {
            foreach (var position in Enum.GetValues<AsciiProbeNonAsciiPosition>())
            {
                var benchmark = new AsciiLikeSpanBenchmark
                {
                    Length = length,
                    NonAsciiPosition = position
                };
                benchmark.Setup();

                var rangeResult = benchmark.ContainsAnyExceptInRange();
                var asciiResult = benchmark.AsciiIsValid();

                Assert.AreEqual(position == AsciiProbeNonAsciiPosition.None, rangeResult);
                Assert.AreEqual(rangeResult, asciiResult, $"Length {length}, position {position}.");
            }
        }
    }

    [TestMethod]
    public void LikeAsciiSpanProbe_AtScalarBoundaryAndEveryNonAsciiPosition_ShouldRemainExact()
    {
        for (var length = 0; length <= 9; length++)
        {
            var ascii = new string('a', length);
            Assert.IsTrue(Musoq.Evaluator.Operators.IsAsciiLikeSpan(ascii, 0, ascii.Length));

            for (var index = 0; index < length; index++)
            {
                var characters = ascii.ToCharArray();
                characters[index] = 'Ż';
                var unicode = new string(characters);
                Assert.IsFalse(
                    Musoq.Evaluator.Operators.IsAsciiLikeSpan(unicode, 0, unicode.Length),
                    $"Length {length}, index {index}.");
            }
        }
    }

    private static object[] Parameters<TBenchmark>(string propertyName)
    {
        var property = typeof(TBenchmark).GetProperty(propertyName)
            ?? throw new AssertFailedException($"Missing property {typeof(TBenchmark).Name}.{propertyName}.");
        var attribute = property.GetCustomAttributes(typeof(ParamsAttribute), inherit: false)
            .Cast<ParamsAttribute>()
            .Single();
        return attribute.Values.Select(static value => value!).ToArray();
    }
}
