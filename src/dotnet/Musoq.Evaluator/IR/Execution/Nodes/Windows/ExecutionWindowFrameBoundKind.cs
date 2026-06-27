using System.Collections.Generic;
using System.Reflection;

namespace Musoq.Evaluator.IR.Execution;

public enum ExecutionWindowFrameBoundKind
{
    UnboundedPreceding,
    UnboundedFollowing,
    CurrentRow,
    OffsetPreceding,
    OffsetFollowing
}
