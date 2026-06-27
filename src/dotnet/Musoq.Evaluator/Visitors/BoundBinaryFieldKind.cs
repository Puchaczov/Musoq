using System.Collections.Generic;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

/// <summary>
///     Distinguishes parsed fields (which consume bytes) from computed fields
///     (which derive their value from other fields without consuming input).
/// </summary>
public enum BoundBinaryFieldKind
{
    Parsed,
    Computed
}
