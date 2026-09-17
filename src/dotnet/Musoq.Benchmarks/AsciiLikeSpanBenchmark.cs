using System.Text;
using BenchmarkDotNet.Attributes;

namespace Musoq.Benchmarks;

/// <summary>
/// Compares the BCL ASCII-validation kernels used by LIKE fallback guards.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class AsciiLikeSpanBenchmark
{
    private string _value = string.Empty;

    [Params(1, 4, 8, 9, 16, 64, 256, 4096)]
    public int Length { get; set; }

    [Params(
        AsciiProbeNonAsciiPosition.None,
        AsciiProbeNonAsciiPosition.First,
        AsciiProbeNonAsciiPosition.Middle,
        AsciiProbeNonAsciiPosition.Last)]
    public AsciiProbeNonAsciiPosition NonAsciiPosition { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var characters = new string('a', Length).ToCharArray();
        if (NonAsciiPosition != AsciiProbeNonAsciiPosition.None)
            characters[ResolveNonAsciiIndex(Length, NonAsciiPosition)] = 'Ż';
        _value = new string(characters);
    }

    [Benchmark(Baseline = true)]
    public bool ContainsAnyExceptInRange() =>
        !_value.AsSpan().ContainsAnyExceptInRange('\0', '\u007F');

    [Benchmark]
    public bool AsciiIsValid() => Ascii.IsValid(_value.AsSpan());

    internal static int ResolveNonAsciiIndex(int length, AsciiProbeNonAsciiPosition position) => position switch
    {
        AsciiProbeNonAsciiPosition.First => 0,
        AsciiProbeNonAsciiPosition.Middle => length / 2,
        AsciiProbeNonAsciiPosition.Last => length - 1,
        _ => throw new ArgumentOutOfRangeException(nameof(position), position, null)
    };
}

public enum AsciiProbeNonAsciiPosition
{
    None,
    First,
    Middle,
    Last
}
