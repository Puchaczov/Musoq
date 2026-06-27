using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Optimization;

internal enum LogicalSubqueryFormKind
{
    Predicate,
    Scalar,
    ScalarMaterialization,
    DerivedTable,
    Unknown
}
