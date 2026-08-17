namespace Musoq.Evaluator.IR.Planning.Subqueries;

internal enum SubqueryLoweringKind
{
    PredicateSemiJoin,
    PredicateAntiSemiJoin,
    PredicateRangeSemiJoin,
    PredicateRangeAntiSemiJoin,
    PredicateHashMark,
    PredicateRangeMark,
    PredicateCte,
    ScalarLeftJoin,
    ScalarHashSingle,
    ScalarRangeSingle,
    DerivedTableJoin,
    DerivedTableScan
}
