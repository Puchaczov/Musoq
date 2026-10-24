using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public static partial class ExecutionExpressionConverter
{
    private static ExecutionExpression ConvertColumnRef(
        ColumnRef column,
        IReadOnlyDictionary<string, RowShape> sourceShapes)
    {
        var resolvedField = ResolveField(column, sourceShapes);
        if (resolvedField != null)
        {
            var (alias, field) = resolvedField.Value;
            field = field with
            {
                AccessStrategy = field.AccessStrategy switch
                {
                    GeneratedRowNestedAccess generated => generated with { IsNullSafe = column.IsNullSafePath },
                    NestedClrPropertyAccess nestedAccess => nestedAccess with { IsNullSafe = column.IsNullSafePath },
                    _ => field.AccessStrategy
                }
            };

            var interpretation = InterpretationMemberBinding.TryCreate(column, alias, sourceShapes);
            if (interpretation != null)
                return interpretation;

            var nested = NativeNestedFieldBinding.TryCreate(column, alias, field, sourceShapes);
            if (nested is not null)
                return nested;

            if (field.AccessStrategy is RuntimeDynamicMemberPathAccess runtimePath)
            {
                ExecutionExpression current = new ExecutionFieldRead(
                    alias,
                    runtimePath.RootFieldName,
                    runtimePath.RootFieldType,
                    runtimePath.RootIsDynamic
                        ? new RuntimeDynamicMemberAccess(runtimePath.RootFieldName)
                        : new ClrPropertyAccess(runtimePath.RootFieldName));

                foreach (var segment in runtimePath.Segments)
                {
                    current = new ExecutionMemberRead(
                        current,
                        segment.MemberName,
                        segment.ResultType,
                        segment.IsDynamic);
                }

                return current;
            }

            return new ExecutionFieldRead(
                alias,
                field.Name,
                column.ReturnType == typeof(object) && field.Type.ResolveClrType() != typeof(object)
                    ? field.Type
                    : ExecutionClrBindingFactory.FromClr(column.ReturnType),
                field.AccessStrategy,
                field.GeneratedTypeName ??
                InterpretationTypeNames.Executable(column.GeneratedTypeName) ??
                (field.AccessStrategy as GeneratedRowNestedAccess)?.ValueTypeName)
            { Stability = field.Stability, SourceReadType = field.SourceReadType, EnumType = field.EnumType, InterpretationShape = field.InterpretationShape };
        }

        return new ExecutionFieldRead(
            column.Alias,
            column.ColumnName,
            ExecutionClrBindingFactory.FromClr(column.ReturnType),
            GeneratedTypeName: InterpretationTypeNames.Executable(column.GeneratedTypeName))
        {
            Stability = column.Stability
        };
    }

    private static ResolvedExecutionField? ResolveField(
        ColumnRef column,
        IReadOnlyDictionary<string, RowShape> sourceShapes)
    {
        if (!string.IsNullOrWhiteSpace(column.Alias))
        {
            if (sourceShapes.TryGetValue(column.Alias, out var shape))
                return ResolveField(column, shape);

            return ResolveFieldFromTransitionShape(column, sourceShapes.Values);
        }

        ResolvedExecutionField? resolved = null;
        foreach (var shape in sourceShapes.Values)
        {
            var candidate = ResolveField(column, shape);
            if (candidate == null)
                continue;
            if (resolved != null)
                return null;
            resolved = candidate;
        }

        return resolved;
    }

    private static ResolvedExecutionField? ResolveFieldFromTransitionShape(
        ColumnRef column,
        IEnumerable<RowShape> sourceShapes)
    {
        ResolvedExecutionField? resolved = null;
        foreach (var shape in sourceShapes.OfType<TableRowShape>())
        {
            var candidate = ResolveField(column, shape);
            if (candidate == null)
                continue;
            if (resolved != null)
                return null;
            resolved = candidate;
        }

        return resolved;
    }

    private static ResolvedExecutionField? ResolveField(ColumnRef column, RowShape sourceShape)
    {
        if (!RowShapeLookup.TryResolveSourceAlias(sourceShape, out var alias))
            return null;

        if (!string.IsNullOrWhiteSpace(column.Alias) &&
            !string.Equals(alias, column.Alias, StringComparison.OrdinalIgnoreCase) &&
            sourceShape is not TableRowShape)
            return null;

        var columnName = column.ColumnName;
        var qualifiedName = $"{alias}.{columnName}";
        var originalQualifiedName = string.IsNullOrWhiteSpace(column.Alias)
            ? columnName
            : $"{column.Alias}.{columnName}";
        var matchingFields = sourceShape.Fields
            .Where(candidate => (column.SourceColumnIndex == null || candidate.OutputIndex == column.SourceColumnIndex) &&
                !(columnName.Contains('.', StringComparison.Ordinal) &&
                  candidate.Name.Contains('.', StringComparison.Ordinal) &&
                  candidate.AccessStrategy is RuntimeDynamicMemberAccess) &&
                (string.Equals(candidate.Name, columnName, StringComparison.Ordinal) ||
                 string.Equals(candidate.QualifiedName, columnName, StringComparison.Ordinal) ||
                 string.Equals(candidate.Name, originalQualifiedName, StringComparison.Ordinal) ||
                 columnName.Contains('.', StringComparison.Ordinal) &&
                 HasQualifiedSuffix(candidate.Name, columnName) ||
                 string.Equals(candidate.QualifiedName, originalQualifiedName, StringComparison.Ordinal) ||
                 string.Equals(candidate.QualifiedName, qualifiedName, StringComparison.Ordinal) ||
                 string.Equals(candidate.QualifiedName, $"{alias}.{originalQualifiedName}", StringComparison.Ordinal)))
            .ToArray();

        var field = matchingFields.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, columnName, StringComparison.Ordinal) ||
            string.Equals(candidate.Name, originalQualifiedName, StringComparison.Ordinal) ||
            string.Equals(candidate.QualifiedName, columnName, StringComparison.Ordinal) ||
            string.Equals(candidate.QualifiedName, originalQualifiedName, StringComparison.Ordinal) ||
            string.Equals(candidate.QualifiedName, qualifiedName, StringComparison.Ordinal) ||
            string.Equals(candidate.QualifiedName, $"{alias}.{originalQualifiedName}", StringComparison.Ordinal)) ??
            matchingFields.FirstOrDefault();

        if (field != null)
            return new ResolvedExecutionField(alias, field);

        var nestedField = ExecutionFieldAccessResolver.ResolveNestedField(
            column,
            sourceShape,
            alias,
            columnName);
        if (nestedField != null)
            return nestedField;

        var indexedField = ExecutionFieldAccessResolver.ResolveIndexedField(
            column,
            sourceShape,
            alias,
            columnName);
        if (indexedField != null)
            return indexedField;

        var unqualifiedMatches = sourceShape.Fields
            .Where(candidate => string.Equals(GetUnqualifiedFieldName(candidate.Name), columnName, StringComparison.Ordinal))
            .Take(2)
            .ToArray();
        field = unqualifiedMatches.Length == 1 ? unqualifiedMatches[0] : null;
        return field == null ? null : new ResolvedExecutionField(alias, field);
    }

    private static bool HasQualifiedSuffix(string candidate, string relativeName)
    {
        return candidate.EndsWith($".{relativeName}", StringComparison.Ordinal);
    }

    private static string GetUnqualifiedFieldName(string fieldName)
    {
        var separatorIndex = fieldName.LastIndexOf('.');
        return separatorIndex < 0 ? fieldName : fieldName[(separatorIndex + 1)..];
    }
}
