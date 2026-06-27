using System.Collections.Generic;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.RuntimeSettings;

internal sealed record ResolvedSourceRuntimeSettings(
    string SourceContextId,
    string? ProfileName,
    IReadOnlyDictionary<string, string> Values,
    IReadOnlyList<SourceRuntimeSettingDescription> Descriptions,
    bool HasDeclaredRequirements,
    bool HasResolvedValues,
    bool HasMissingRequired);
