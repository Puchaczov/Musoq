using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

internal static class RowShapeLookup
{
    public static IReadOnlyDictionary<string, RowShape> EmptySourceShapeLookup()
    {
        return new Dictionary<string, RowShape>(StringComparer.OrdinalIgnoreCase);
    }

    public static IReadOnlyDictionary<string, RowShape> CreateSourceShapeLookupOrEmpty(RowShape? source)
    {
        return source == null
            ? EmptySourceShapeLookup()
            : CreateSourceShapeLookup(source);
    }

    public static IReadOnlyDictionary<string, RowShape> CreateSourceShapeLookup(
        RowShape left,
        RowShape right)
    {
        return CreateSourceShapeLookup(
            new Dictionary<string, RowShape>(StringComparer.OrdinalIgnoreCase),
            left,
            right);
    }

    public static IReadOnlyDictionary<string, RowShape> CreateSourceShapeLookup(RowShape source)
    {
        return CreateSourceShapeLookup(
            new Dictionary<string, RowShape>(StringComparer.OrdinalIgnoreCase),
            source);
    }

    public static IReadOnlyDictionary<string, RowShape> CreateSourceShapeLookup(
        IReadOnlyDictionary<string, RowShape> inherited,
        params RowShape[] sources)
    {
        var lookup = new Dictionary<string, RowShape>(inherited, StringComparer.OrdinalIgnoreCase);

        foreach (var source in sources)
        {
            var alias = ResolveSourceAlias(source);
            if (!string.IsNullOrWhiteSpace(alias))
                lookup[alias] = source;
        }

        return lookup;
    }

    public static IReadOnlyDictionary<string, RowShape> CreateTransitionAliasLookup(
        IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        var lookup = new Dictionary<string, RowShape>(sourceLookup, StringComparer.OrdinalIgnoreCase);

        foreach (var source in sourceLookup.Values.OfType<TableRowShape>())
        {
            foreach (var field in source.Fields)
            {
                AddQualifiedAlias(lookup, source, field.Name);
                AddQualifiedAlias(lookup, source, field.QualifiedName);
            }

            foreach (var context in source.Contexts)
            {
                AddAlias(lookup, source, context.Name);
                AddAlias(lookup, source, context.QualifiedName);
            }
        }

        return lookup;
    }

    private static void AddAlias(Dictionary<string, RowShape> lookup, RowShape source, string alias)
    {
        if (!string.IsNullOrWhiteSpace(alias))
            lookup.TryAdd(alias, source);
    }

    private static void AddQualifiedAlias(Dictionary<string, RowShape> lookup, RowShape source, string fieldName)
    {
        var separatorIndex = fieldName.IndexOf('.', StringComparison.Ordinal);
        if (separatorIndex <= 0)
            return;

        lookup.TryAdd(fieldName[..separatorIndex], source);
    }

    public static string ResolveSourceAlias(RowShape shape)
    {
        if (TryResolveSourceAlias(shape, out var alias))
            return alias;

        throw new NotSupportedException($"Row shape '{shape.GetType().Name}' does not expose a source alias.");
    }

    public static bool TryResolveSourceAlias(RowShape shape, out string alias)
    {
        alias = shape switch
        {
            SourceEntityShape source => source.Alias,
            TableRowShape tableRow => tableRow.Alias,
            ValuesRowShape values => values.Alias,
            ExpandoAdapterShape expando => expando.Alias,
            _ => string.Empty
        };

        return !string.IsNullOrWhiteSpace(alias);
    }

    public static Type ResolveSourceRuntimeType(RowShape sourceShape)
    {
        return sourceShape switch
        {
            SourceEntityShape source when UsesReflectedMemberAccess(source) || !CanReferenceType(source.EntityType.ResolveClrType()) => typeof(object),
            SourceEntityShape source => source.EntityType.ResolveClrType(),
            ExpandoAdapterShape expando => expando.RuntimeType.ResolveClrType(),
            TableRowShape => typeof(Row),
            ValuesRowShape => typeof(object),
            GeneratedRowShape => typeof(object),
            _ => typeof(object)
        };
    }

    public static Type ResolveSourceRequestType(RowShape sourceShape)
    {
        return sourceShape switch
        {
            SourceEntityShape source => source.EntityType.ResolveClrType(),
            ExpandoAdapterShape expando => expando.RuntimeType.ResolveClrType(),
            TableRowShape => typeof(Row),
            ValuesRowShape => typeof(object),
            GeneratedRowShape => typeof(object),
            _ => typeof(object)
        };
    }

    public static bool UsesReflectedMemberAccess(SourceEntityShape source)
    {
        return source.Fields.Any(static field => field.AccessStrategy is ReflectedMemberAccess);
    }

    public static bool CanReferenceType(Type type)
    {
        return ExecutionSourceCodeGenerationPolicy.CanReferenceType(type);
    }

    public static FieldBinding? ResolveProjectedField(
        GeneratedRowShape rowShape,
        OrderField key,
        IReadOnlyList<ProjectedField> projectedFields)
    {
        if (key.Expression is ColumnRef columnRef)
        {
            var field = ResolveProjectedField(rowShape, columnRef);
            if (field != null)
                return field;
        }

        var outputName = SortKeyProjectionResolver.TryResolveOutputName(key.Expression, projectedFields);
        return outputName == null
            ? null
            : ResolveProjectedField(rowShape, outputName);
    }

    public static FieldBinding? ResolveProjectedField(GeneratedRowShape rowShape, ColumnRef columnRef)
    {
        var qualifiedName = string.IsNullOrWhiteSpace(columnRef.Alias)
            ? columnRef.ColumnName
            : $"{columnRef.Alias}.{columnRef.ColumnName}";

        return ResolveProjectedField(rowShape, columnRef.ColumnName)
               ?? ResolveProjectedField(rowShape, qualifiedName);
    }

    public static FieldBinding? ResolveProjectedField(GeneratedRowShape rowShape, string fieldName)
    {
        return rowShape.Fields.FirstOrDefault(field =>
            string.Equals(field.Name, fieldName, StringComparison.Ordinal) ||
            string.Equals(field.QualifiedName, fieldName, StringComparison.Ordinal));
    }
}
