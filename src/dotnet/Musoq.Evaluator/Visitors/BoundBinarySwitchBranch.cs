using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

/// <summary>
///     Resolved binding for a single binary switch branch, describing the match label,
///     the emitted branch alias property, and the branch payload type to parse.
/// </summary>
public sealed record BoundBinarySwitchBranch
{
    /// <summary>Gets the constant case label, or null when this is the default branch.</summary>
    public Musoq.Parser.Nodes.Node? CaseValue { get; init; }

    /// <summary>Gets the branch alias emitted as a nullable property of the union result.</summary>
    public required string BranchAlias { get; init; }

    /// <summary>Gets the branch payload type annotation to parse when this branch matches.</summary>
    public required TypeAnnotationNode BranchType { get; init; }

    /// <summary>Gets whether this branch is the default ("_") branch.</summary>
    public bool IsDefault => CaseValue is null;
}
