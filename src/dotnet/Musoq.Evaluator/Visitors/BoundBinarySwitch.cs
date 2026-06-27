using System.Collections.Generic;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

/// <summary>
///     Resolved binding for a binary switch (tagged union) field. Captures the
///     discriminator selector and the ordered branches the renderer chooses between.
/// </summary>
public sealed record BoundBinarySwitch
{
    /// <summary>Gets the name of the previously parsed field used as the discriminator.</summary>
    public required string Selector { get; init; }

    /// <summary>Gets the branches in declaration order; the optional default branch is last.</summary>
    public required IReadOnlyList<BoundBinarySwitchBranch> Branches { get; init; }

    /// <summary>Gets the default branch when present, otherwise null.</summary>
    public BoundBinarySwitchBranch? DefaultBranch { get; init; }
}
