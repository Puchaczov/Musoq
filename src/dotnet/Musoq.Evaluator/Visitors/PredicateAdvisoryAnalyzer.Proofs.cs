using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal static partial class PredicateAdvisoryAnalyzer
{
    private static void ReportProvenAnd(
        SemanticAdvisoryContext context,
        PredicateConstantResolver resolver,
        AndNode node)
    {
        if (!TryCreateFact(resolver, node.Left, out var left) ||
            !TryCreateFact(resolver, node.Right, out var right) ||
            !string.Equals(left.Column, right.Column, StringComparison.Ordinal))
            return;

        if (!FactsContradict(left, right))
            return;

        context.Report(
            DiagnosticCode.MQ5011_ContradictoryCondition,
            "Predicate conditions cannot be true at the same time.",
            node.Span);
    }

    private static void ReportProvenOr(
        SemanticAdvisoryContext context,
        PredicateConstantResolver resolver,
        OrNode node)
    {
        if (!TryCreateFact(resolver, node.Left, out var left) ||
            !TryCreateFact(resolver, node.Right, out var right) ||
            !string.Equals(left.Column, right.Column, StringComparison.Ordinal) ||
            !((left.Kind == FactKind.IsNull && right.Kind == FactKind.IsNotNull) ||
              (left.Kind == FactKind.IsNotNull && right.Kind == FactKind.IsNull)))
            return;

        context.Report(
            DiagnosticCode.MQ5010_TautologicalCondition,
            "Predicate always evaluates to true because every value is either NULL or not NULL.",
            node.Span);
    }

    private static bool TryCreateFact(
        PredicateConstantResolver resolver,
        Node node,
        out PredicateFact fact)
    {
        if (node is IsNullNode isNull && isNull.Expression is AccessColumnNode column)
        {
            fact = new PredicateFact(ColumnKey(column), isNull.IsNegated ? FactKind.IsNotNull : FactKind.IsNull, null, false);
            return true;
        }

        if (node is not BinaryNode comparison || !IsComparison(node))
        {
            fact = default;
            return false;
        }

        if (comparison.Left is AccessColumnNode leftColumn && TryGetValue(resolver, comparison.Right, out var rightValue))
        {
            fact = CreateFact(ColumnKey(leftColumn), node, rightValue, false);
            return true;
        }

        if (comparison.Right is AccessColumnNode rightColumn && TryGetValue(resolver, comparison.Left, out var leftValue))
        {
            fact = CreateFact(ColumnKey(rightColumn), node, leftValue, true);
            return true;
        }

        fact = default;
        return false;
    }

    private static PredicateFact CreateFact(string column, Node comparison, object? value, bool reversed)
    {
        var kind = comparison switch
        {
            EqualityNode => FactKind.Equal,
            GreaterNode => reversed ? FactKind.Upper : FactKind.Lower,
            GreaterOrEqualNode => reversed ? FactKind.UpperInclusive : FactKind.LowerInclusive,
            LessNode => reversed ? FactKind.Lower : FactKind.Upper,
            LessOrEqualNode => reversed ? FactKind.LowerInclusive : FactKind.UpperInclusive,
            _ => FactKind.Equal
        };
        return new PredicateFact(column, kind, value, true);
    }

    private static bool FactsContradict(PredicateFact left, PredicateFact right)
    {
        if (left.Kind == FactKind.IsNull && right.Kind == FactKind.IsNotNull ||
            left.Kind == FactKind.IsNotNull && right.Kind == FactKind.IsNull)
            return true;

        if (!left.HasValue || !right.HasValue)
            return false;

        if (left.Kind == FactKind.Equal && right.Kind == FactKind.Equal)
            return !ValuesEqual(left.Value, right.Value);

        if (left.Kind == FactKind.Equal)
            return !Satisfies(left.Value, right);

        if (right.Kind == FactKind.Equal)
            return !Satisfies(right.Value, left);

        return IsLower(left) && IsUpper(right)
            ? BoundsContradict(left, right)
            : IsLower(right) && IsUpper(left) && BoundsContradict(right, left);
    }

    private static bool Satisfies(object? value, PredicateFact fact)
    {
        if (!TryDecimal(value, out var actual) || !TryDecimal(fact.Value, out var bound))
            return true;

        return fact.Kind switch
        {
            FactKind.Lower => actual > bound,
            FactKind.LowerInclusive => actual >= bound,
            FactKind.Upper => actual < bound,
            FactKind.UpperInclusive => actual <= bound,
            _ => true
        };
    }

    private static bool BoundsContradict(PredicateFact lower, PredicateFact upper)
    {
        if (!TryDecimal(lower.Value, out var low) || !TryDecimal(upper.Value, out var high))
            return false;

        return low > high || low == high && (lower.Kind == FactKind.Lower || upper.Kind == FactKind.Upper);
    }

    private static bool TryGetValue(PredicateConstantResolver resolver, Node node, out object? value)
    {
        var resolved = resolver.Resolve(node);
        if (resolved is not ConstantValueNode constant || resolved is NullNode)
        {
            value = null;
            return false;
        }

        value = constant.ObjValue;
        return value is not float and not double;
    }

    private static bool TryFindNullOperand(
        PredicateConstantResolver resolver,
        Node node,
        out Node nullOperand)
    {
        if (node is not BinaryNode comparison)
        {
            nullOperand = null!;
            return false;
        }

        if (resolver.Resolve(comparison.Left) is NullNode leftNull)
        {
            nullOperand = leftNull;
            return true;
        }

        if (resolver.Resolve(comparison.Right) is NullNode rightNull)
        {
            nullOperand = rightNull;
            return true;
        }

        nullOperand = null!;
        return false;
    }

    private static bool HasNullConstant(Node node, IReadOnlyDictionary<string, ScriptVariableDefinition> variables)
    {
        if (node is not BinaryNode comparison)
            return false;

        return IsNullValue(comparison.Left, variables) || IsNullValue(comparison.Right, variables);
    }

    private static bool IsNullValue(Node node, IReadOnlyDictionary<string, ScriptVariableDefinition> variables)
    {
        if (node is NullNode)
            return true;

        if (node is ParameterReferenceNode parameter && variables.TryGetValue(parameter.Name, out var definition))
            return definition.Value is null;

        if (node is ScriptVariableReferenceNode variable && variables.TryGetValue(variable.Name, out definition))
            return definition.Value is null;

        return false;
    }

    private static string ColumnKey(AccessColumnNode column) => $"{column.Alias}.{column.Name}";

    private static bool IsLower(PredicateFact fact) => fact.Kind is FactKind.Lower or FactKind.LowerInclusive;

    private static bool IsUpper(PredicateFact fact) => fact.Kind is FactKind.Upper or FactKind.UpperInclusive;

    private static bool ValuesEqual(object? left, object? right)
    {
        if (TryDecimal(left, out var leftNumber) && TryDecimal(right, out var rightNumber))
            return leftNumber == rightNumber;

        return Equals(left, right);
    }

    private static bool TryDecimal(object? value, out decimal result)
    {
        switch (value)
        {
            case byte number: result = number; return true;
            case sbyte number: result = number; return true;
            case short number: result = number; return true;
            case ushort number: result = number; return true;
            case int number: result = number; return true;
            case uint number: result = number; return true;
            case long number: result = number; return true;
            case ulong number: result = number; return true;
            case decimal number: result = number; return true;
            default: result = default; return false;
        }
    }

    private enum FactKind
    {
        Equal,
        Lower,
        LowerInclusive,
        Upper,
        UpperInclusive,
        IsNull,
        IsNotNull
    }

    private readonly record struct PredicateFact(string Column, FactKind Kind, object? Value, bool HasValue);
}
