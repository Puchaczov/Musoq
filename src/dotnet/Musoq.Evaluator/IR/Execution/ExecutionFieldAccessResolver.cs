using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.IR.Execution;

internal static class ExecutionFieldAccessResolver
{
    internal static ResolvedExecutionField? ResolveNestedField(
        ColumnRef column,
        RowShape sourceShape,
        string alias,
        string sourceRelativeColumnName)
    {
        if (!sourceRelativeColumnName.Contains('.', StringComparison.Ordinal) &&
            !sourceRelativeColumnName.Contains('[', StringComparison.Ordinal))
            return null;

        var nestedRoot = FindNestedRootField(column, sourceShape, alias, sourceRelativeColumnName);
        if (nestedRoot == null)
            return null;

        if (sourceShape is TableRowShape && IsSelfNestedTransitionAlias(nestedRoot))
            return new ResolvedExecutionField(alias, nestedRoot.Field);

        var fieldName = sourceShape is TableRowShape
            ? RemoveSourceAlias(nestedRoot.FieldName, alias)
            : sourceRelativeColumnName;
        FieldAccessStrategy? accessStrategy = sourceShape switch
        {
            TableRowShape when nestedRoot.Field.AccessStrategy is GeneratedRowTypeAccess generatedRow =>
                new GeneratedRowNestedAccess(
                    generatedRow.TypeName,
                    generatedRow.FieldName,
                    nestedRoot.PropertyPath,
                    IsRowCarrier: true),
            TableRowShape table when nestedRoot.Field.AccessStrategy is GeneratedFieldAccess generatedField &&
                                    table.Contexts.FirstOrDefault(context => context.AccessStrategy is GeneratedRowContextAccess) is
                                    { AccessStrategy: GeneratedRowContextAccess generatedContext } =>
                new GeneratedRowNestedAccess(
                    generatedContext.TypeName,
                    generatedField.FieldName,
                    nestedRoot.PropertyPath,
                    IsRowCarrier: true),
            TableRowShape table when nestedRoot.Field.AccessStrategy is GeneratedRowContextAccess generatedContext &&
                                    ResolveGeneratedRowFieldName(table, nestedRoot.Field.OutputIndex) is { } generatedContextFieldName =>
                new GeneratedRowNestedAccess(
                    generatedContext.TypeName,
                    generatedContextFieldName,
                    nestedRoot.PropertyPath,
                    IsRowCarrier: true),
            TableRowShape table when table.GeneratedRowTypeName != null &&
                                    nestedRoot.Field.AccessStrategy is PositionalAccess positional =>
                new GeneratedRowNestedAccess(
                    table.GeneratedRowTypeName,
                    ResolveGeneratedRowFieldName(table, positional.Index) ?? string.Empty,
                    nestedRoot.PropertyPath,
                    IsRowCarrier: true),
            TableRowShape table when nestedRoot.Field.GeneratedTypeName is { } generatedTypeName &&
                                    ResolveGeneratedRowFieldName(table, nestedRoot.Field.OutputIndex) is { } generatedFieldName =>
                new GeneratedRowNestedAccess(
                    string.Empty,
                    generatedFieldName,
                    nestedRoot.PropertyPath,
                    IsRowCarrier: true),
            TableRowShape table when TryCreateGeneratedTableNestedAccess(table, nestedRoot) is { } generatedTable =>
                generatedTable,
            TableRowShape table when TryCreateGeneratedDictionaryNestedAccess(table, nestedRoot) is { } generatedDictionary =>
                generatedDictionary,
            TableRowShape table when TryCreateGeneratedContextNestedAccess(table, nestedRoot) is { } generatedContext =>
                generatedContext,
            TableRowShape table => ThrowMissingGeneratedRowType(column, alias, table),
            SourceEntityShape source when nestedRoot.Field.Type.ResolveClrType() is { } nestedType &&
                                    (nestedRoot.Field.AccessStrategy is RuntimeDynamicMemberAccess ||
                                     ExecutionSourceCodeGenerationPolicy.IsSupportedDynamicObject(nestedType)) =>
                new RuntimeDynamicMemberPathAccess(
                    nestedRoot.Field.Name,
                    nestedRoot.Field.Type,
                    CreateRuntimeDynamicPathSegments(
                        nestedRoot.Field.Type.ResolveClrType(),
                        nestedRoot.PropertyPath,
                    column.ReturnType,
                    sourceShape.Fields,
                    nestedRoot.Field.Name),
                    rootIsDynamic: nestedRoot.Field.AccessStrategy is RuntimeDynamicMemberAccess),
            SourceEntityShape when nestedRoot.Field.AccessStrategy is ReflectedMemberAccess =>
                new ReflectedMemberAccess(sourceRelativeColumnName),
            SourceEntityShape when nestedRoot.Field.AccessStrategy is PositionalAccess positional =>
                new NestedPositionalAccess(positional.Index, nestedRoot.PropertyPath),
            SourceEntityShape source when nestedRoot.Field.GeneratedTypeName is { } generatedTypeName =>
                new GeneratedRowNestedAccess(
                    source.GeneratedTypeName ?? string.Empty,
                    nestedRoot.Field.Name,
                    nestedRoot.PropertyPath,
                    generatedTypeName,
                    IsRowCarrier: true),
            SourceEntityShape when IsGeneratedDictionaryValue(nestedRoot.Field) =>
                new GeneratedDictionaryNestedAccess(nestedRoot.Field.Name, nestedRoot.PropertyPath),
            SourceEntityShape source when IsDirectScalarSource(source) =>
                new NestedClrPropertyAccess(nestedRoot.PropertyPath),
            SourceEntityShape => new NestedClrPropertyAccess(sourceRelativeColumnName),
            _ => null
        };

        if (accessStrategy == null)
            return null;

        var field = new FieldBinding(
            fieldName,
            sourceShape is TableRowShape
                ? $"{alias}.{fieldName}"
                : string.IsNullOrWhiteSpace(column.Alias)
                    ? sourceRelativeColumnName
                    : $"{column.Alias}.{sourceRelativeColumnName}",
            nestedRoot.Field.OutputIndex,
            column.ReturnType,
            nestedRoot.Field.Nullability,
            accessStrategy);

        if (accessStrategy is NestedPositionalAccess)
            field = field with
            {
                GeneratedTypeName = nestedRoot.Field.Type.DisplayName.Replace('+', '.')
            };

        return new ResolvedExecutionField(alias, field);
    }

    internal static ResolvedExecutionField? ResolveIndexedField(
        ColumnRef column,
        RowShape sourceShape,
        string alias,
        string sourceRelativeColumnName)
    {
        if (!sourceRelativeColumnName.Contains('[', StringComparison.Ordinal))
            return null;

        var rootName = GetRootFieldName(sourceRelativeColumnName);
        var rootField = sourceShape.Fields.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, rootName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(candidate.Name, $"{column.Alias}.{rootName}", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(candidate.QualifiedName, $"{alias}.{rootName}", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(candidate.QualifiedName, $"{column.Alias}.{rootName}", StringComparison.OrdinalIgnoreCase));

        if (rootField == null)
            return null;

        FieldAccessStrategy? accessStrategy = sourceShape switch
        {
            SourceEntityShape when rootField.AccessStrategy is ReflectedMemberAccess =>
                new ReflectedMemberAccess(sourceRelativeColumnName),
            SourceEntityShape => new NestedClrPropertyAccess(sourceRelativeColumnName),
            _ => null
        };

        if (accessStrategy == null)
            return null;

        var field = new FieldBinding(
            sourceRelativeColumnName,
            string.IsNullOrWhiteSpace(column.Alias)
                ? sourceRelativeColumnName
                : $"{column.Alias}.{sourceRelativeColumnName}",
            rootField.OutputIndex,
            column.ReturnType,
            rootField.Nullability,
            accessStrategy);

        return new ResolvedExecutionField(alias, field);
    }

    internal static GeneratedRowNestedAccess? TryCreateGeneratedTableNestedAccess(
        TableRowShape table,
        NestedRootField nestedRoot)
    {
        var pathRoot = nestedRoot.PropertyPath
            .Split(['.', '['], 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
        if (string.IsNullOrWhiteSpace(pathRoot))
            return null;

        var qualifiedRoot = $"{nestedRoot.Field.Name}.{pathRoot}";
        var sourceField = table.Fields.FirstOrDefault(field =>
            string.Equals(field.Name, qualifiedRoot, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(field.QualifiedName, qualifiedRoot, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(field.Name, pathRoot, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(field.QualifiedName, pathRoot, StringComparison.OrdinalIgnoreCase));

        if (sourceField?.GeneratedTypeName is not { Length: > 0 } generatedTypeName ||
            ResolveGeneratedRowFieldName(table, sourceField.OutputIndex) is not { } generatedFieldName)
            return null;

        return new GeneratedRowNestedAccess(
            string.Empty,
            generatedFieldName,
            nestedRoot.PropertyPath,
            IsRowCarrier: true);
    }

    internal static GeneratedDictionaryNestedAccess? TryCreateGeneratedDictionaryNestedAccess(
        TableRowShape table,
        NestedRootField nestedRoot)
    {
        if (!IsGeneratedDictionaryValue(nestedRoot.Field) ||
            ResolveGeneratedRowFieldName(table, nestedRoot.Field.OutputIndex) is not { } fieldName)
            return null;

        return new GeneratedDictionaryNestedAccess(fieldName, nestedRoot.PropertyPath);
    }

    internal static FieldAccessStrategy ThrowMissingGeneratedRowType(
        ColumnRef column,
        string alias,
        TableRowShape table)
    {
        var fields = string.Join(
            ", ",
            table.Fields.Select(field => $"{field.Name}@{field.OutputIndex}:{field.Type.DisplayName}"));
        var contexts = string.Join(
            ", ",
            table.Contexts.Select(context => $"{context.Name}:{context.GeneratedTypeName ?? "<none>"}:{context.AccessStrategy}"));
        throw new InvalidOperationException(
            $"Generated execution cannot lower nested table field '{column.ColumnName}' because table '{alias}' has no generated row type. Fields: {fields}. Contexts: {contexts}.");
    }

    internal static GeneratedRowNestedAccess? TryCreateGeneratedContextNestedAccess(
        TableRowShape table,
        NestedRootField nestedRoot)
    {
        var sourceAlias = GetNestedFieldSourceAlias(nestedRoot.Field.Name);
        var contextIndex = -1;
        FieldBinding? context = null;

        for (var index = 0; index < table.Contexts.Count; index++)
        {
            var candidate = table.Contexts[index];
            if (!string.Equals(candidate.Name, sourceAlias, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(candidate.QualifiedName, sourceAlias, StringComparison.OrdinalIgnoreCase))
                continue;

            context = candidate;
            contextIndex = candidate.AccessStrategy is ContextAccess access ? access.Index : index;
            break;
        }

        if (context?.GeneratedTypeName is not { Length: > 0 } generatedTypeName || contextIndex < 0)
            return null;

        var rootFieldName = nestedRoot.Field.Name;
        var separatorIndex = rootFieldName.LastIndexOf('.');
        if (separatorIndex >= 0)
            rootFieldName = rootFieldName[(separatorIndex + 1)..];

        if (context.GeneratedMemberTypeNames.TryGetValue(rootFieldName, out var generatedMemberTypeName) &&
            ResolveGeneratedRowFieldName(table, nestedRoot.Field.OutputIndex) is { } generatedFieldName)
        {
            return new GeneratedRowNestedAccess(
                string.Empty,
                generatedFieldName,
                nestedRoot.PropertyPath,
                generatedMemberTypeName,
                IsRowCarrier: true);
        }

        throw new InvalidOperationException(
            $"Generated interpretation member '{sourceAlias}.{rootFieldName}' has no generated type metadata. " +
            $"Known members: {string.Join(", ", context.GeneratedMemberTypeNames.Keys)}. " +
            $"Fields: {string.Join(", ", table.Fields.Select(field => $"{field.Name}:{field.Type.DisplayName}:{field.GeneratedTypeName ?? "<none>"}"))}.");
    }

    internal static string? ResolveGeneratedRowFieldName(TableRowShape table, int fieldIndex)
    {
        var field = table.Fields.FirstOrDefault(candidate => candidate.OutputIndex == fieldIndex);
        var directName = field?.AccessStrategy switch
        {
            GeneratedRowTypeAccess generated => generated.FieldName,
            GeneratedFieldAccess generated => generated.FieldName,
            _ => null
        };
        if (directName != null)
            return directName;

        if (field == null)
            return null;

        var usedNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var candidate in table.Fields.OrderBy(candidate => candidate.OutputIndex))
        {
            var generatedName = ExecutionSymbolicNamePolicy.CreateGeneratedFieldName(
                candidate.Name,
                candidate.OutputIndex,
                usedNames);
            if (candidate.OutputIndex == fieldIndex)
                return generatedName;
        }

        return field.Name;
    }

    private static bool IsSelfNestedTransitionAlias(NestedRootField nestedRoot) =>
        !nestedRoot.Field.Name.Contains('.', StringComparison.Ordinal) &&
        string.Equals(nestedRoot.PropertyPath, nestedRoot.Field.Name, StringComparison.OrdinalIgnoreCase);

    private static NestedRootField? FindNestedRootField(
        ColumnRef column,
        RowShape sourceShape,
        string alias,
        string sourceRelativeColumnName)
    {
        var originalQualifiedName = string.IsNullOrWhiteSpace(column.Alias)
            ? column.ColumnName
            : $"{column.Alias}.{column.ColumnName}";

        return sourceShape.Fields
            .SelectMany(candidate => CreateNestedRootMatches(
                candidate,
                alias,
                sourceRelativeColumnName,
                originalQualifiedName))
            .OrderByDescending(candidate => candidate.PrefixLength)
            .FirstOrDefault();
    }

    private static IEnumerable<NestedRootField> CreateNestedRootMatches(
        FieldBinding candidate,
        string alias,
        string sourceRelativeColumnName,
        string originalQualifiedName)
    {
        foreach (var match in CreateNestedRootMatches(candidate, sourceRelativeColumnName, sourceRelativeColumnName))
            yield return match;
        foreach (var match in CreateNestedRootMatches(candidate, originalQualifiedName, originalQualifiedName))
            yield return match;
        foreach (var match in CreateNestedRootMatches(candidate, $"{alias}.{sourceRelativeColumnName}", sourceRelativeColumnName))
            yield return match;
        foreach (var match in CreateNestedRootMatches(candidate, $"{alias}.{originalQualifiedName}", originalQualifiedName))
            yield return match;
    }

    private static IEnumerable<NestedRootField> CreateNestedRootMatches(
        FieldBinding candidate,
        string columnName,
        string fieldName)
    {
        foreach (var prefix in CreateNestedRootPrefixes(candidate))
        {
            if (IsNestedPrefix(columnName, prefix))
            {
                yield return new NestedRootField(
                    candidate,
                    fieldName,
                    CreateNestedPropertyPath(columnName, prefix),
                    prefix.Length);
            }
        }
    }

    private static IEnumerable<string> CreateNestedRootPrefixes(FieldBinding candidate)
    {
        yield return candidate.Name;
        yield return candidate.QualifiedName;

        var aliasSeparatorIndex = candidate.Name.IndexOf('.');
        if (aliasSeparatorIndex >= 0 && aliasSeparatorIndex < candidate.Name.Length - 1)
            yield return candidate.Name[(aliasSeparatorIndex + 1)..];

        aliasSeparatorIndex = candidate.QualifiedName.IndexOf('.');
        if (aliasSeparatorIndex >= 0 && aliasSeparatorIndex < candidate.QualifiedName.Length - 1)
            yield return candidate.QualifiedName[(aliasSeparatorIndex + 1)..];
    }

    private static bool IsNestedPrefix(string columnName, string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix) ||
            !columnName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
            columnName.Length <= prefix.Length)
            return false;

        return columnName[prefix.Length] is '.' or '[';
    }

    private static string CreateNestedPropertyPath(string columnName, string rootName)
    {
        var propertyPath = columnName[rootName.Length..];
        return propertyPath.StartsWith('.') ? propertyPath[1..] : propertyPath;
    }

    private static bool IsGeneratedDictionaryValue(FieldBinding field)
    {
        var type = field.Type.ResolveClrType();
        return type == typeof(object) || DynamicEntityBoundary.IsDynamicEntity(type);
    }

    private static bool IsDirectScalarSource(SourceEntityShape source) =>
        source.Fields is [{ AccessStrategy: DirectScalarValueAccess }];

    private static IReadOnlyList<RuntimeDynamicMemberPathSegment> CreateRuntimeDynamicPathSegments(
        Type rootType,
        string propertyPath,
        Type finalType,
        IReadOnlyList<FieldBinding> schemaFields,
        string rootFieldName)
    {
        var segments = propertyPath
            .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var result = new List<RuntimeDynamicMemberPathSegment>(segments.Length);
        var currentType = rootType;

        for (var index = 0; index < segments.Length; index++)
        {
            var name = segments[index];
            var property = currentType.GetProperty(
                name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            var field = property == null
                ? currentType.GetField(
                    name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase)
                : null;

            if (property != null || field != null)
            {
                var memberType = property?.PropertyType ?? field!.FieldType;
                var memberName = property?.Name ?? field!.Name;
                result.Add(new RuntimeDynamicMemberPathSegment(
                    memberName,
                    ExecutionClrBindingFactory.FromClr(memberType),
                    isDynamic: false));
                currentType = memberType;
                continue;
            }

            var canonicalName = ResolveSchemaMemberName(schemaFields, rootFieldName, segments, index) ?? name;
            var hintedType = ResolveDynamicMemberType(currentType, name) ??
                             (index == segments.Length - 1 ? finalType : typeof(object));
            result.Add(new RuntimeDynamicMemberPathSegment(
                canonicalName,
                ExecutionClrBindingFactory.FromClr(hintedType),
                isDynamic: true));
            currentType = hintedType;
        }

        return result;
    }

    private static string? ResolveSchemaMemberName(
        IReadOnlyList<FieldBinding> schemaFields,
        string rootFieldName,
        IReadOnlyList<string> segments,
        int segmentIndex)
    {
        var expectedPrefix = $"{rootFieldName}.{string.Join('.', segments.Take(segmentIndex + 1))}";
        var schemaField = schemaFields.FirstOrDefault(field =>
            string.Equals(field.Name, expectedPrefix, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(field.QualifiedName, expectedPrefix, StringComparison.OrdinalIgnoreCase));
        if (schemaField == null)
            return null;

        var separator = schemaField.Name.LastIndexOf('.');
        return separator < 0 ? schemaField.Name : schemaField.Name[(separator + 1)..];
    }

    private static Type? ResolveDynamicMemberType(Type parentType, string memberName)
    {
        if (!DynamicEntityBoundary.IsDynamicMetaObjectProvider(parentType))
            return null;

        var hint = parentType
            .GetCustomAttributes<DynamicObjectPropertyTypeHintAttribute>(inherit: true)
            .FirstOrDefault(attribute => string.Equals(attribute.Name, memberName, StringComparison.OrdinalIgnoreCase));
        if (hint != null)
            return hint.Type;

        return parentType.GetCustomAttribute<DynamicObjectPropertyDefaultTypeHintAttribute>(inherit: true)?.Type;
    }

    private static string GetRootFieldName(string columnName)
    {
        var separatorIndex = columnName.IndexOf('.');
        var rootSegment = separatorIndex < 0 ? columnName : columnName[..separatorIndex];
        var indexerIndex = rootSegment.IndexOf('[');
        return indexerIndex < 0 ? rootSegment : rootSegment[..indexerIndex];
    }

    private static string RemoveSourceAlias(string columnName, string sourceAlias)
    {
        var aliasPrefix = $"{sourceAlias}.";
        return columnName.StartsWith(aliasPrefix, StringComparison.OrdinalIgnoreCase)
            ? columnName[aliasPrefix.Length..]
            : columnName;
    }

    private static string GetNestedFieldSourceAlias(string fieldName)
    {
        var separatorIndex = fieldName.IndexOf('.');
        return separatorIndex >= 0 ? fieldName[..separatorIndex] : fieldName;
    }
}

internal readonly record struct ResolvedExecutionField(string Alias, FieldBinding Field);

internal sealed record NestedRootField(
    FieldBinding Field,
    string FieldName,
    string PropertyPath,
    int PrefixLength);
