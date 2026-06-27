using System;
using System.Collections.Generic;
using Musoq.Parser;

namespace Musoq.Evaluator.Visitors.Helpers.Subqueries;

internal enum SubqueryCorrelationNullSemantics
{
    NotCorrelated,
    EqualityComparison,
    ResidualOrUnknown
}
