using System;
using Musoq.Plugins;

namespace Musoq.Evaluator.IR.Execution;

/// <summary>
///     Inline rendering shape selected for an aggregate kernel. <see cref="None"/> means the
///     kernel must be rendered through the general (non-inline) aggregate path.
/// </summary>
internal enum AggregateInlineKind
{
    None,
    CountAll,
    CountNullable,
    CountReference,
    Sum,
    Avg,
    Min,
    Max
}
