using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.Helpers.Subqueries;

internal sealed record CorrelationProjection(
    string Alias,
    string ColumnName,
    string CteColumnName,
    Type ReturnType,
    TextSpan Span,
    string? IntendedTypeName);
