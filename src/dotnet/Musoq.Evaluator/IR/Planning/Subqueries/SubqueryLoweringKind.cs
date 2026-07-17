using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.Visitors.Helpers.Subqueries;

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
