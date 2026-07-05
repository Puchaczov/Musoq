using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using AggregateRefRewriter = Musoq.Evaluator.IR.Expressions.AggregateRefRewriter;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static ExecutionFieldRead? ResolveWindowAggregateSourceRead(
        IrExpression expression,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        IReadOnlyDictionary<string, string>? aggregateSourceFields = null)
    {
        var sourceField = GetWindowAggregateSourceIdentifiers(expression)
            .SelectMany(identifier => GetWindowAggregateSourceFieldCandidates(identifier, aggregateSourceFields))
            .Select(identifier => ResolveWindowAggregateSourceField(identifier, sourceLookup))
            .FirstOrDefault(field => field != null);

        if (sourceField is null)
            return null;

        return new ExecutionFieldRead(
            sourceField.Alias,
            sourceField.Field.Name,
            expression.ReturnType,
            sourceField.Field.AccessStrategy);
    }

    private static IReadOnlyDictionary<string, string> CreateWindowAggregateSourceFieldLookup(
        PhysicalNode source)
    {
        var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var binding in GetWindowAggregateSourceBindings(source))
        {
            AddWindowAggregateSourceFieldLookup(lookup, binding.Identifier, binding.ColumnName);
            AddWindowAggregateSourceFieldLookup(lookup, AggregateRefRewriter.NormalizeIdentifier(binding.Identifier), binding.ColumnName);
            AddWindowAggregateSourceFieldLookup(lookup, binding.ColumnName, binding.ColumnName);
            AddWindowAggregateSourceFieldLookup(lookup, AggregateRefRewriter.NormalizeIdentifier(binding.ColumnName), binding.ColumnName);
            AddWindowAggregateSourceFieldLookup(lookup, binding.DisplayName, binding.ColumnName);
            AddWindowAggregateSourceFieldLookup(lookup, AggregateRefRewriter.NormalizeIdentifier(binding.DisplayName), binding.ColumnName);
        }

        return lookup;
    }

    private static IEnumerable<AggregateBinding> GetWindowAggregateSourceBindings(PhysicalNode source)
    {
        return source switch
        {
            PhysicalHavingFilterNode having => GetWindowAggregateSourceBindings(having.Input),
            PhysicalAggregateOnlyNode aggregate => aggregate.Bindings,
            PhysicalSingleKeyAggregateNode aggregate => aggregate.Bindings,
            PhysicalValueTupleAggregateNode aggregate => aggregate.Bindings,
            _ => source.Children.SelectMany(GetWindowAggregateSourceBindings)
        };
    }

    private static void AddWindowAggregateSourceFieldLookup(
        IDictionary<string, string> lookup,
        string? key,
        string fieldName)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(fieldName))
            return;

        lookup.TryAdd(key, fieldName);
    }

    private static IEnumerable<string> GetWindowAggregateSourceFieldCandidates(
        string identifier,
        IReadOnlyDictionary<string, string>? aggregateSourceFields)
    {
        if (aggregateSourceFields != null &&
            aggregateSourceFields.TryGetValue(identifier, out var fieldName) &&
            !string.IsNullOrWhiteSpace(fieldName))
        {
            yield return fieldName;
        }

        var normalizedIdentifier = AggregateRefRewriter.NormalizeIdentifier(identifier);
        if (aggregateSourceFields != null &&
            !string.IsNullOrWhiteSpace(normalizedIdentifier) &&
            aggregateSourceFields.TryGetValue(normalizedIdentifier, out fieldName) &&
            !string.IsNullOrWhiteSpace(fieldName) &&
            !string.Equals(fieldName, identifier, StringComparison.Ordinal))
        {
            yield return fieldName;
        }

        yield return identifier;
    }

    private static IEnumerable<string> GetWindowAggregateSourceIdentifiers(IrExpression expression)
    {
        switch (expression)
        {
            case AggregateRef aggregateRef:
                yield return aggregateRef.Identifier;
                if (!string.IsNullOrWhiteSpace(aggregateRef.DisplayName) &&
                    !string.Equals(aggregateRef.DisplayName, aggregateRef.Identifier, StringComparison.Ordinal))
                {
                    yield return aggregateRef.DisplayName;
                }
                break;
            case MethodCall methodCall when IsAggregateLikeMethodCall(methodCall):
                var rawIdentifier = GetRawAggregateIdentifier(methodCall);
                if (!string.IsNullOrWhiteSpace(rawIdentifier))
                    yield return rawIdentifier;

                var displayName = AggregateRefRewriter.ExtractDisplayName(methodCall);
                if (!string.IsNullOrWhiteSpace(displayName) &&
                    !string.Equals(displayName, rawIdentifier, StringComparison.Ordinal))
                {
                    yield return displayName;
                }

                var normalizedIdentifier = AggregateRefRewriter.ExtractIdentifier(methodCall);
                if (!string.IsNullOrWhiteSpace(normalizedIdentifier) &&
                    !string.Equals(normalizedIdentifier, rawIdentifier, StringComparison.Ordinal))
                {
                    yield return normalizedIdentifier;
                }

                break;
        }
    }

    private static WindowAggregateSourceField? ResolveWindowAggregateSourceField(
        string identifier,
        IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        var exactMatch = ResolveExactWindowAggregateSourceField(identifier, sourceLookup);
        if (exactMatch != null)
            return exactMatch;

        var normalizedIdentifier = AggregateRefRewriter.NormalizeIdentifier(identifier);
        var matches = sourceLookup.Values
            .OfType<TableRowShape>()
            .SelectMany(tableRow => tableRow.Fields
                .Where(candidate => MatchesWindowAggregateSourceField(normalizedIdentifier, candidate))
                .Select(candidate => new
                {
                    tableRow.Alias,
                    Field = candidate
                }))
            .Take(2)
            .ToArray();

        if (matches.Length == 1)
            return new WindowAggregateSourceField(matches[0].Alias, matches[0].Field);

        return null;
    }

    private static WindowAggregateSourceField? ResolveExactWindowAggregateSourceField(
        string identifier,
        IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        var matches = sourceLookup.Values
            .OfType<TableRowShape>()
            .SelectMany(tableRow => tableRow.Fields
                .Where(candidate => MatchesExactWindowAggregateSourceField(identifier, candidate))
                .Select(candidate => new
                {
                    tableRow.Alias,
                    Field = candidate
                }))
            .Take(2)
            .ToArray();

        return matches.Length == 1
            ? new WindowAggregateSourceField(matches[0].Alias, matches[0].Field)
            : null;
    }

    private static bool MatchesExactWindowAggregateSourceField(
        string identifier,
        FieldBinding field)
    {
        return MatchesExactWindowAggregateSourceFieldName(identifier, field.Name) ||
               MatchesExactWindowAggregateSourceFieldName(identifier, field.QualifiedName);
    }

    private static bool MatchesExactWindowAggregateSourceFieldName(
        string identifier,
        string candidate)
    {
        return string.Equals(candidate, identifier, StringComparison.OrdinalIgnoreCase) ||
               candidate.EndsWith($".{identifier}", StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesWindowAggregateSourceField(
        string? normalizedIdentifier,
        FieldBinding field)
    {
        if (string.IsNullOrWhiteSpace(normalizedIdentifier))
            return false;

        return MatchesWindowAggregateSourceFieldName(normalizedIdentifier, field.Name) ||
               MatchesWindowAggregateSourceFieldName(normalizedIdentifier, field.QualifiedName);
    }

    private static bool MatchesWindowAggregateSourceFieldName(
        string normalizedIdentifier,
        string candidate)
    {
        var normalizedCandidate = AggregateRefRewriter.NormalizeIdentifier(candidate);
        if (string.IsNullOrWhiteSpace(normalizedCandidate))
            return false;

        return string.Equals(normalizedCandidate, normalizedIdentifier, StringComparison.OrdinalIgnoreCase) ||
               normalizedCandidate.EndsWith($".{normalizedIdentifier}", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAggregateLikeMethodCall(MethodCall methodCall)
    {
        return AggregateRefRewriter.IsAggregateMethod(methodCall.Method);
    }
}
