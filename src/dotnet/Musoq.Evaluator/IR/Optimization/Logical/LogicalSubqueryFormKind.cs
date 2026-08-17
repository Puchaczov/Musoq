namespace Musoq.Evaluator.IR.Optimization.Logical;

internal enum LogicalSubqueryFormKind
{
    Predicate,
    Scalar,
    ScalarMaterialization,
    DerivedTable,
    Unknown
}

