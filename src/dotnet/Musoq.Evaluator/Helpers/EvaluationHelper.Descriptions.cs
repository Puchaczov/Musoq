using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Runtime;
using Musoq.Evaluator.Tables;
using Musoq.Schema;

namespace Musoq.Evaluator.Helpers;

public static partial class EvaluationHelper
{
    private static readonly BoundedRuntimeCache<string, XmlDocument> XmlDocCache =
        new(RuntimeCacheOptions.XmlDocumentationCacheSize, StringComparer.Ordinal);
    private static readonly Regex WhitespaceNormalizerRegex = new(@"\s+", RegexOptions.Compiled);

    public static Table GetSpecificTableDescription(ISchemaTable table)
    {
        ArgumentNullException.ThrowIfNull(table);
        var newTable = new Table("desc", [
            new Column("Name", typeof(string), 0),
            new Column("Index", typeof(int), 1),
            new Column("Type", typeof(string), 2)
        ]);

        foreach (var column in table.Columns)
        foreach (var complexField in CreateTypeComplexDescription(column.ColumnName, column.ColumnType))
            newTable.AddUnchecked(new DescriptionColumnRow(
                complexField.FieldName,
                column.ColumnIndex,
                complexField.Type.FullName ?? complexField.Type.Name));

        return newTable;
    }

    public static Table GetQueryDescription(IEnumerable<ISchemaColumn> columns)
    {
        ArgumentNullException.ThrowIfNull(columns);
        var newTable = new Table("desc", [
            new Column("Name", typeof(string), 0),
            new Column("Index", typeof(int), 1),
            new Column("Type", typeof(string), 2)
        ]);

        foreach (var column in columns)
            newTable.AddUnchecked(new DescriptionColumnRow(
                column.ColumnName,
                column.ColumnIndex,
                column.ColumnType.FullName ?? column.ColumnType.Name));

        return newTable;
    }

    public static Table GetSpecificColumnDescription(ISchemaTable table, string columnName)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(columnName);
        var newTable = new Table("desc", [
            new Column("Name", typeof(string), 0),
            new Column("Index", typeof(int), 1),
            new Column("Type", typeof(string), 2)
        ]);


        var pathParts = columnName.Split('.');
        var rootColumnName = pathParts[0];

        var targetColumn = table.Columns.FirstOrDefault(c =>
            string.Equals(c.ColumnName, rootColumnName, StringComparison.OrdinalIgnoreCase));

        if (targetColumn == null)
            throw new UnknownColumnOrAliasException($"Column '{rootColumnName}' does not exist in the table.");

        var canonicalPathParts = new List<string> { targetColumn.ColumnName };
        var currentType = targetColumn.ColumnType;

        for (var i = 1; i < pathParts.Length; i++)
        {
            if (currentType.IsArray)
                currentType = currentType.GetElementType()!;
            else if (IsGenericEnumerable(currentType, out var elementTypeFromEnumerable))
                currentType = elementTypeFromEnumerable;

            var propertyName = pathParts[i];
            var property = currentType.GetProperties()
                .FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase));

            if (property == null)
                throw new UnknownColumnOrAliasException(
                    $"Property '{propertyName}' does not exist on type '{currentType.Name}'.");

            canonicalPathParts.Add(property.Name);
            currentType = property.PropertyType;
        }

        var canonicalPath = string.Join(".", canonicalPathParts);


        Type elementType;

        if (currentType.IsArray)
            elementType = currentType.GetElementType()!;
        else if (IsGenericEnumerable(currentType, out var genericElementType))
            elementType = genericElementType;
        else if (currentType.IsPrimitive || currentType == typeof(string) || currentType == typeof(object))
            throw new ColumnMustBeAnArrayOrImplementIEnumerableException();
        else
            elementType = currentType;


        var prefixLength = canonicalPath.Length;

        foreach (var complexField in CreateTypeComplexDescription(canonicalPath, elementType))
        {
            var relativeFieldName =
                complexField.FieldName.Length > prefixLength && complexField.FieldName[prefixLength] == '.'
                    ? complexField.FieldName.Substring(prefixLength + 1)
                    : complexField.FieldName == canonicalPath
                        ? canonicalPath.Substring(canonicalPath.LastIndexOf('.') + 1)
                        : complexField.FieldName;

            newTable.AddUnchecked(new DescriptionColumnRow(
                relativeFieldName,
                targetColumn.ColumnIndex,
                complexField.Type.FullName ?? complexField.Type.Name));
        }

        return newTable;
    }

    private static bool IsGenericEnumerable(Type type, [NotNullWhen(true)] out Type? elementType)
    {
        elementType = null;

        if (!type.IsGenericType) return false;

        var interfaces = type.GetInterfaces().Concat([type]);

        foreach (var interfaceType in interfaces)
        {
            if (!interfaceType.IsGenericType ||
                interfaceType.GetGenericTypeDefinition() != typeof(IEnumerable<>)) continue;

            elementType = interfaceType.GetGenericArguments()[0];
            return true;
        }

        return false;
    }


}
