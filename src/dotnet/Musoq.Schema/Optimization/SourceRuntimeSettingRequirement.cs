namespace Musoq.Schema.Optimization;

public sealed record SourceRuntimeSettingRequirement(
    string Name,
    bool Required,
    bool Secret,
    SourceRuntimeSettingPhase Phases,
    string Description);
