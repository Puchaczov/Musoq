using Musoq.Schema.Optimization;

namespace Musoq.Schema.Attributes;

[AttributeUsage(AttributeTargets.Constructor, AllowMultiple = true)]
public sealed class SourceRuntimeSettingAttribute(string name) : Attribute
{
    public string Name { get; } = string.IsNullOrWhiteSpace(name)
        ? throw new ArgumentException("Runtime setting name cannot be empty.", nameof(name))
        : name;

    public bool Required { get; init; } = true;

    public bool Secret { get; init; }

    public SourceRuntimeSettingPhase Phases { get; init; } = SourceRuntimeSettingPhase.All;

    public string Description { get; init; } = string.Empty;

    public SourceRuntimeSettingRequirement ToRequirement()
    {
        return new SourceRuntimeSettingRequirement(Name, Required, Secret, Phases, Description);
    }
}
