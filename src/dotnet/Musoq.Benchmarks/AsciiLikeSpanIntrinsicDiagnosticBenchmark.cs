using System.Text;
using BenchmarkDotNet.Attributes;

namespace Musoq.Benchmarks;

/// <summary>
/// Produces compact enabled/disabled hardware-intrinsic disassembly for the two BCL ASCII kernels.
/// </summary>
[DisassemblyDiagnoser(printSource: true, exportCombinedDisassemblyReport: true)]
[ShortRunJob]
public class AsciiLikeSpanIntrinsicDiagnosticBenchmark
{
    private readonly string _value = new('a', 4096);

    [Benchmark(Baseline = true)]
    public bool ContainsAnyExceptInRange() =>
        !_value.AsSpan().ContainsAnyExceptInRange('\0', '\u007F');

    [Benchmark]
    public bool AsciiIsValid() => Ascii.IsValid(_value.AsSpan());
}
