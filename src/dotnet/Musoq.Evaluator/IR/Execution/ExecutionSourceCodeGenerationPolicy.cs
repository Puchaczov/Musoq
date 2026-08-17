using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser.Nodes.From;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

/// <summary>
/// Defines the source contracts that can cross the generated-execution boundary.
/// Compile-time reflection is deliberately isolated here; generated code never
/// uses the fallback that this policy rejects.
/// </summary>
internal static class ExecutionSourceCodeGenerationPolicy
{
    public static IReadOnlyList<Violation> FindViolations(
        Scope scope,
        IEnumerable<SchemaFromNode> sourceNodes,
        IReadOnlyDictionary<SchemaFromNode, ISchemaColumn[]> usedColumns)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(sourceNodes);
        ArgumentNullException.ThrowIfNull(usedColumns);

        var violations = new List<Violation>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var source in sourceNodes)
        {
            // Interpret/Parse sources are compiler-created in-memory row carriers. They
            // are not CLR-backed datasource contracts and must not be mistaken for a
            // source that would require reflected row loading.
            if (IsGeneratedInterpretationSource(source))
                continue;

            if (!TryResolveEntityType(scope, source.Alias, out var entityType))
                continue;

            var used = usedColumns.TryGetValue(source, out var columns)
                ? columns
                : Array.Empty<ISchemaColumn>();

            var reason = GetEntityContractViolation(entityType);
            if (reason != null)
            {
                AddViolation(source, entityType, string.Empty, reason);
                continue;
            }

            if (IsSupportedDictionary(entityType))
                continue;

            if (IsSupportedDynamicObject(entityType))
            {
                foreach (var column in used)
                {
                    if (CanReferenceType(column.ColumnType))
                        continue;

                    AddViolation(
                        source,
                        entityType,
                        column.ColumnName,
                        $"column '{column.ColumnName}' reaches non-referenceable type '{column.ColumnType.FullName ?? column.ColumnType.Name}'");
                }

                continue;
            }

            // A coupled/table source supplies a generated column contract rather than
            // CLR member paths. The public source row remains part of the typed source
            // boundary, but its columns are intentionally mapped by the table shape.
            if (IsExternallyProvidedType(source) || IsSupportedScalarEntity(entityType))
                continue;

            if (SchemaIndexedRowContract.IsSupported(entityType))
            {
                foreach (var column in used)
                {
                    if (SchemaIndexedRowContract.TryValidateColumn(column, out var positionalReason))
                        continue;

                    AddViolation(source, entityType, column.ColumnName, positionalReason!);
                }

                continue;
            }

            foreach (var column in used)
            {
                if (TryValidateMemberPath(entityType, column.ColumnName, out var memberReason))
                    continue;

                AddViolation(source, entityType, column.ColumnName, memberReason!);
            }
        }

        return violations;

        void AddViolation(SchemaFromNode source, Type entityType, string memberPath, string reason)
        {
            var key = $"{source.Id}|{memberPath}|{reason}";
            if (seen.Add(key))
                violations.Add(new Violation(source, entityType, memberPath, reason));
        }
    }

    public static bool CanReferenceType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (type.IsByRef || type.IsPointer)
            return false;

        if (type.IsArray)
            return type.GetElementType() is { } elementType && CanReferenceType(elementType);

        var nullableType = Nullable.GetUnderlyingType(type);
        if (nullableType != null)
            return CanReferenceType(nullableType);

        if (type.IsGenericType)
        {
            return CanReferencePublicType(type.GetGenericTypeDefinition()) &&
                   type.GetGenericArguments().All(CanReferenceType);
        }

        return CanReferencePublicType(type);
    }

    public static bool IsSupportedDictionary(Type type)
    {
        return type == DynamicEntityBoundary.ExpandoType ||
               DynamicEntityBoundary.IsAssignableToStringObjectDictionary(type);
    }

    public static bool IsSupportedDynamicObject(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return DynamicEntityBoundary.IsDynamicObject(type) &&
               !IsSupportedDictionary(type);
    }

    private static string? GetEntityContractViolation(Type entityType)
    {
        if (entityType == typeof(object))
            return "the source metadata is object-typed";

        if (DynamicEntityBoundary.IsDynamicMetaObjectProvider(entityType) &&
            !IsSupportedDynamicObject(entityType))
        {
            return "the source entity is a custom runtime-dynamic type";
        }

        if (!CanReferenceType(entityType))
            return "the source entity or one of its declaring/generic types is not publicly referenceable";

        return null;
    }

    private static bool IsExternallyProvidedType(SchemaFromNode source)
    {
        return source is Musoq.Evaluator.Parser.SchemaFromNode { HasExternallyProvidedTypes: true };
    }

    private static bool IsSupportedScalarEntity(Type entityType)
    {
        var scalarType = Nullable.GetUnderlyingType(entityType) ?? entityType;
        return scalarType.IsPrimitive ||
               scalarType.IsEnum ||
               scalarType == typeof(string) ||
               scalarType == typeof(decimal) ||
               scalarType == typeof(DateTime) ||
               scalarType == typeof(DateTimeOffset) ||
               scalarType == typeof(TimeSpan) ||
               scalarType == typeof(Guid);
    }

    private static bool TryValidateMemberPath(Type rootType, string path, out string? reason)
    {
        reason = null;
        var currentType = rootType;
        var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0)
        {
            reason = "the projected member path is empty";
            return false;
        }

        foreach (var rawSegment in segments)
        {
            var segment = rawSegment;
            var indexStart = segment.IndexOf('[', StringComparison.Ordinal);
            if (indexStart >= 0)
                segment = segment[..indexStart];

            if (segment.Length != 0)
            {
                var member = currentType.GetMember(
                        segment,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase)
                    .FirstOrDefault(candidate => candidate switch
                    {
                        PropertyInfo property => property.GetMethod is { IsPublic: true },
                        FieldInfo field => field.IsPublic,
                        _ => false
                    });

                currentType = member switch
                {
                    PropertyInfo property => property.PropertyType,
                    FieldInfo field => field.FieldType,
                    _ => null!
                };

                if (currentType == null)
                {
                    reason = $"member '{segment}' is not a public instance property or field";
                    return false;
                }
            }

            if (indexStart >= 0)
            {
                currentType = ResolveIndexedType(currentType);
                if (currentType == null)
                {
                    reason = $"member '{rawSegment}' uses an indexer that cannot be emitted as typed access";
                    return false;
                }
            }

            if (!CanReferenceType(currentType))
            {
                reason = $"member path '{path}' reaches non-referenceable type '{currentType.FullName ?? currentType.Name}'";
                return false;
            }
        }

        return true;
    }

    private static Type? ResolveIndexedType(Type type)
    {
        if (type.IsArray)
            return type.GetElementType();

        return type.GetDefaultMembers()
            .OfType<PropertyInfo>()
            .FirstOrDefault(property => property.GetIndexParameters().Length > 0)
            ?.PropertyType;
    }

    private static bool TryResolveEntityType(Scope scope, string alias, out Type entityType)
    {
        var tableSymbol = FindTableSymbol(scope, alias);
        if (tableSymbol == null || !tableSymbol.ContainsAlias(alias))
        {
            entityType = typeof(object);
            return false;
        }

        var (_, table, _) = tableSymbol.GetTableByAlias(alias);
        entityType = table.Metadata?.TableEntityType ?? typeof(object);
        return true;
    }

    private static TableSymbol? FindTableSymbol(Scope scope, string alias)
    {
        TableSymbol? firstMatch = null;
        TableSymbol? firstTypedMatch = null;
        FindTableSymbol(scope, alias, ref firstMatch, ref firstTypedMatch);
        return firstTypedMatch ?? firstMatch;
    }

    private static bool IsGeneratedInterpretationSource(SchemaFromNode source)
    {
        return source.Method.Equals("Interpret", StringComparison.OrdinalIgnoreCase) ||
               source.Method.Equals("Parse", StringComparison.OrdinalIgnoreCase) ||
               source.Method.Equals("InterpretAt", StringComparison.OrdinalIgnoreCase) ||
               source.Method.Equals("TryInterpret", StringComparison.OrdinalIgnoreCase) ||
               source.Method.Equals("TryParse", StringComparison.OrdinalIgnoreCase) ||
               source.Method.Equals("PartialInterpret", StringComparison.OrdinalIgnoreCase) ||
               source.Method.Equals("PartialParse", StringComparison.OrdinalIgnoreCase);
    }

    private static void FindTableSymbol(
        Scope scope,
        string alias,
        ref TableSymbol? firstMatch,
        ref TableSymbol? firstTypedMatch)
    {
        if (scope.ScopeSymbolTable.TryGetSymbol(alias, out TableSymbol? tableSymbol) &&
            tableSymbol != null &&
            tableSymbol.ContainsAlias(alias))
        {
            firstMatch ??= tableSymbol;
            var (_, table, _) = tableSymbol.GetTableByAlias(alias);
            if ((table.Metadata?.TableEntityType ?? typeof(object)) != typeof(object))
                firstTypedMatch ??= tableSymbol;
        }

        foreach (var child in scope.Child)
            FindTableSymbol(child, alias, ref firstMatch, ref firstTypedMatch);
    }

    private static bool CanReferencePublicType(Type type)
    {
        if (!type.IsNested)
            return type.IsPublic;

        return type is { IsNestedPublic: true, DeclaringType: not null } &&
               CanReferencePublicType(type.DeclaringType);
    }

    internal sealed record Violation(
        SchemaFromNode Source,
        Type EntityType,
        string MemberPath,
        string Reason);
}
