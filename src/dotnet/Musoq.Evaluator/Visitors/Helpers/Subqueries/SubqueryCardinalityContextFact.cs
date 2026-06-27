using System;
using System.Collections.Generic;
using Musoq.Parser;

namespace Musoq.Evaluator.Visitors.Helpers.Subqueries;

internal sealed record SubqueryCardinalityContextFact(
    SubqueryCardinalityContextKind Kind,
    string Reason);
