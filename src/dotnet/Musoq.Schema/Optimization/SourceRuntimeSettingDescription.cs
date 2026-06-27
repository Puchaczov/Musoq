namespace Musoq.Schema.Optimization;

public sealed record SourceRuntimeSettingDescription(
    string Name,
    bool Required,
    bool Secret,
    SourceRuntimeSettingPhase Phases,
    SourceRuntimeSettingResolutionStatus Status,
    string Description);
