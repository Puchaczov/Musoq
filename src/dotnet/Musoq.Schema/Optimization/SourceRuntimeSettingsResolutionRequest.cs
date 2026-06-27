using System.Collections.Generic;

namespace Musoq.Schema.Optimization;

public sealed record SourceRuntimeSettingsResolutionRequest(
    SourceIdentity Identity,
    string? ProfileName,
    IReadOnlyList<SourceRuntimeSettingRequirement> Requirements,
    object?[] Parameters);
