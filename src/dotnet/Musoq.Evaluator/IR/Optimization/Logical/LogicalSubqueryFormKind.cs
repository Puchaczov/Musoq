using System.Collections.Generic;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Evaluator.IR.Optimization.Logical;

internal enum LogicalSubqueryFormKind
{
    Predicate,
    Scalar,
    ScalarMaterialization,
    DerivedTable,
    Unknown
}

