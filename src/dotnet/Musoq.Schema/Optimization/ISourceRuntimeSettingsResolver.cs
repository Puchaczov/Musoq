using System.Collections.Generic;

namespace Musoq.Schema.Optimization;

public interface ISourceRuntimeSettingsResolver
{
    IReadOnlyDictionary<string, string> Resolve(SourceRuntimeSettingsResolutionRequest request);
}
