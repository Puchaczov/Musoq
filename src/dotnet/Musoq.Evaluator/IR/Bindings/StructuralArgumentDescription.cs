namespace Musoq.Evaluator.IR.Bindings;

/// <summary>
/// Portable metadata for one row returned by <c>DESC ARGUMENTS</c>.
/// The descriptor contains only immutable scalar values so execution plans do
/// not retain reflection objects or provider instances.
/// </summary>
public sealed record StructuralArgumentDescription(
    int Overload,
    string Path,
    string Kind,
    string Type,
    bool? Required,
    bool Nullable,
    bool HasDefault,
    string? Default,
    int? MaxDepth,
    int? MaxNodes,
    long? MaxStringBytes);
