using System;
using System.Collections.Generic;
using Musoq.Parser;

namespace Musoq.Evaluator.Visitors.Helpers.Subqueries;

internal sealed record SubqueryCorrelationKeyFact(
    string LocalAlias,
    string LocalColumn,
    Type LocalType,
    TextSpan LocalSpan,
    string? LocalIntendedTypeName,
    string OuterAlias,
    string OuterColumn,
    Type OuterType,
    TextSpan OuterSpan,
    string? OuterIntendedTypeName);
