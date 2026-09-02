using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Musoq.Schema.Optimization;

namespace Musoq.Converter;

internal static class ArtifactMetadataSnapshot
{
    public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> CopySourceRuntimeSettings(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> source)
    {
        return CopyStringKeyedDictionary(source, CopyStringDictionary);
    }

    public static IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> CopySourceRuntimeSettingDescriptions(
        IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> source)
    {
        return CopyStringKeyedDictionary(source, CopyList);
    }

    public static IReadOnlyDictionary<string, SourceExecutionPlan> CopySourceExecutionPlans(
        IReadOnlyDictionary<string, SourceExecutionPlan> source)
    {
        return CopyStringKeyedDictionary(source, CopySourceExecutionPlan);
    }

    private static SourceExecutionPlan CopySourceExecutionPlan(SourceExecutionPlan plan)
    {
        return new SourceExecutionPlan
        {
            Identity = plan.Identity,
            AcceptedColumns = CopyList(plan.AcceptedColumns.Select(CopyColumn)),
            AcceptedPredicate = CopyPredicate(plan.AcceptedPredicate),
            AcceptedOrderBy = CopyList(plan.AcceptedOrderBy.Select(CopyOrderBy)),
            AcceptedSkip = plan.AcceptedSkip,
            AcceptedTake = plan.AcceptedTake,
            Properties = CopyProperties(plan.Properties)
        };
    }

    private static SourceColumnRef CopyColumn(SourceColumnRef column)
    {
        return new SourceColumnRef(column.Name, column.ReadModifiers);
    }

    private static OrderByExpression CopyOrderBy(OrderByExpression orderBy)
    {
        return new OrderByExpression(CopyColumn(orderBy.Column), orderBy.Direction);
    }

    private static SourcePredicateExpression? CopyPredicate(SourcePredicateExpression? predicate)
    {
        return predicate switch
        {
            null => null,
            SourcePredicateColumn column => new SourcePredicateColumn(CopyColumn(column.Column)),
            SourcePredicateLiteral literal => new SourcePredicateLiteral(CopyKnownMetadataValue(literal.Value)),
            SourcePredicateEnumLiteral literal => new SourcePredicateEnumLiteral(
                literal.Value,
                literal.EnumFingerprint),
            SourcePredicateComparison comparison => new SourcePredicateComparison(
                comparison.Operator,
                CopyPredicate(comparison.Left)!,
                CopyPredicate(comparison.Right)!),
            SourcePredicateLogical logical => new SourcePredicateLogical(
                logical.Operator,
                CopyPredicate(logical.Left)!,
                CopyPredicate(logical.Right)!),
            SourcePredicateIn sourceIn => new SourcePredicateIn(
                CopyPredicate(sourceIn.Expression)!,
                CopyList(sourceIn.Values.Select(static value => CopyPredicate(value)!)),
                sourceIn.IsNegated),
            SourcePredicateNullCheck nullCheck => new SourcePredicateNullCheck(
                CopyPredicate(nullCheck.Expression)!,
                nullCheck.IsNegated),
            SourcePredicateFlags flags => new SourcePredicateFlags(
                CopyPredicate(flags.Expression)!,
                new SourcePredicateEnumLiteral(flags.Mask.Value, flags.Mask.EnumFingerprint),
                flags.MatchMode),
            _ => throw new InvalidOperationException($"Unsupported source predicate metadata type {predicate.GetType().FullName}.")
        };
    }

    private static IReadOnlyDictionary<string, object?> CopyProperties(
        IReadOnlyDictionary<string, object?> source)
    {
        return CopyStringKeyedDictionary(source, CopyKnownMetadataValue);
    }

    private static object? CopyKnownMetadataValue(object? value)
    {
        return value switch
        {
            null => null,
            string => value,
            Type => value,
            SourceColumnRef column => CopyColumn(column),
            OrderByExpression orderBy => CopyOrderBy(orderBy),
            SourcePredicateExpression predicate => CopyPredicate(predicate),
            IReadOnlyDictionary<string, string> stringDictionary => CopyStringDictionary(stringDictionary),
            IReadOnlyDictionary<string, object?> objectDictionary => CopyObjectDictionary(objectDictionary),
            IEnumerable<string> strings => CopyList(strings),
            IEnumerable<SourceColumnRef> columns => CopyList(columns.Select(CopyColumn)),
            IEnumerable<OrderByExpression> orderBys => CopyList(orderBys.Select(CopyOrderBy)),
            IEnumerable<SourcePredicateExpression> predicates => CopyList(predicates.Select(static predicate => CopyPredicate(predicate)!)),
            _ when IsScalar(value) => value,
            _ => value
        };
    }

    private static IReadOnlyDictionary<string, object?> CopyObjectDictionary(
        IReadOnlyDictionary<string, object?> source)
    {
        return CopyStringKeyedDictionary(source, CopyKnownMetadataValue);
    }

    private static IReadOnlyDictionary<string, string> CopyStringDictionary(
        IReadOnlyDictionary<string, string> source)
    {
        return CopyStringKeyedDictionary(source, static value => value);
    }

    private static IReadOnlyList<T> CopyList<T>(IEnumerable<T> source)
    {
        return Array.AsReadOnly(source.ToArray());
    }

    private static IReadOnlyDictionary<string, TValue> CopyStringKeyedDictionary<TSourceValue, TValue>(
        IReadOnlyDictionary<string, TSourceValue> source,
        Func<TSourceValue, TValue> copyValue)
    {
        var copy = new Dictionary<string, TValue>(source.Count, StringComparer.Ordinal);
        foreach (var entry in source)
            copy[entry.Key] = copyValue(entry.Value);

        return new ReadOnlyDictionary<string, TValue>(copy);
    }

    private static bool IsScalar(object value)
    {
        var type = value.GetType();
        return type.IsPrimitive ||
               type.IsEnum ||
               type == typeof(decimal) ||
               type == typeof(DateTime) ||
               type == typeof(DateTimeOffset) ||
               type == typeof(TimeSpan) ||
               type == typeof(Guid);
    }
}
