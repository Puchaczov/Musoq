using System.Collections.Generic;

namespace Musoq.Schema.Optimization;

public sealed class EmptySourceRuntimeSettingsResolver : ISourceRuntimeSettingsResolver
{
    public static EmptySourceRuntimeSettingsResolver Instance { get; } = new();

    private EmptySourceRuntimeSettingsResolver()
    {
    }

    public IReadOnlyDictionary<string, string> Resolve(SourceRuntimeSettingsResolutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new Dictionary<string, string>();
    }
}
