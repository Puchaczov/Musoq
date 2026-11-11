using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Benchmarks.Scenarios;

public sealed class PerformanceLibrary : LibraryBase
{
    [BindableMethod]
    public long StableWork(string value) => Checksum(value);

    // Identical work and deterministic values keep the oracle fixed; the annotation forbids reuse.
    [BindableMethod]
    [NonDeterministic]
    public long VolatileWork(string value) => Checksum(value);

    private static long Checksum(string value)
    {
        var hash = 1469598103934665603L;
        foreach (var character in value)
            hash = unchecked((hash ^ character) * 1099511628211L);
        return hash;
    }
}
