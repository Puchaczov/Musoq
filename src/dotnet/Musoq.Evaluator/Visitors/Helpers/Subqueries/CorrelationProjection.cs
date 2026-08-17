using Musoq.Parser;

namespace Musoq.Evaluator.Visitors.Helpers.Subqueries;

internal sealed record CorrelationProjection(
    string Alias,
    string ColumnName,
    string CteColumnName,
    Type ReturnType,
    TextSpan Span,
    string? IntendedTypeName);
