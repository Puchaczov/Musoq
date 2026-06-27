using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Musoq.Evaluator.TemporarySchemas;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using static Musoq.Evaluator.Visitors.BuildMetadataAndInferTypesVisitorUtilities;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private static ISchemaTable CreatePartialInterpretTable()
    {
        return new DynamicTable(
        [
            new SchemaColumn("ParsedFields", 0, typeof(Dictionary<string, object?>)),
            new SchemaColumn("ErrorField", 1, typeof(string)),
            new SchemaColumn("ErrorMessage", 2, typeof(string)),
            new SchemaColumn("BytesConsumed", 3, typeof(int))
        ]);
    }

    private ISchemaTable CreateInterpretTable(string? schemaName)
    {
        if (schemaName == null || SchemaRegistry == null)
            throw new InvalidOperationException(
                $"Cannot create interpret table: schema name is '{schemaName ?? "null"}' and schema registry is {(SchemaRegistry != null ? "present" : "null")}.");

        var schema = SchemaRegistry.GetSchema(schemaName);
        var columns = new List<ISchemaColumn>();

        switch (schema.Node)
        {
            case BinarySchemaNode binaryNode:
            {
                var allFields = GetAllBinarySchemaFields(binaryNode);
                columns.AddRange(CreateBinarySchemaColumns(
                    allFields,
                    ComputedFieldColumnTypeMode.PreferExpressionReturnType));
                break;
            }
            case TextSchemaNode textSchemaNode:
            {
                var columnIndex = 0;

                foreach (var field in textSchemaNode.Fields)
                {
                    if (field.Name.StartsWith('_'))
                        continue;

                    if (field is { FieldType: TextFieldType.Pattern, CaptureGroups.Length: > 0 })
                    {
                        columns.Add(new SchemaColumn(field.Name, columnIndex++, typeof(object),
                            $"Musoq.Generated.Interpreters.{schema.Name}.CaptureResult_{field.Name}"));
                        continue;
                    }

                    if (field.FieldType == TextFieldType.Repeat)
                    {
                        var elementSchemaName = field.PrimaryValue ?? "object";
                        columns.Add(new SchemaColumn(field.Name, columnIndex++, typeof(object[]),
                            $"Musoq.Generated.Interpreters.{elementSchemaName}[]"));
                        continue;
                    }

                    if (field.FieldType == TextFieldType.Switch)
                    {
                        columns.Add(new SchemaColumn(field.Name, columnIndex++, typeof(ExpandoObject)));
                        continue;
                    }

                    columns.Add(new SchemaColumn(field.Name, columnIndex++, typeof(string)));
                }

                break;
            }
        }

        if (columns.Count == 0)
            return CreateEmptyTable();

        return new DynamicTable(columns.ToArray());
    }

    private List<SchemaFieldNode> GetAllBinarySchemaFields(
        BinarySchemaNode binaryNode)
    {
        if (string.IsNullOrEmpty(binaryNode.Extends))
            return binaryNode.Fields.ToList();

        var allFields = new List<SchemaFieldNode>();

        if (!string.IsNullOrEmpty(binaryNode.Extends))
        {
            var parentSchema = RequireSchemaRegistry().GetSchema(binaryNode.Extends);
            if (parentSchema?.Node is BinarySchemaNode parentBinaryNode)
                allFields.AddRange(GetAllBinarySchemaFields(parentBinaryNode));
        }


        var overriddenParentNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var childField in binaryNode.Fields)
            for (var i = 0; i < allFields.Count; i++)
                if (string.Equals(allFields[i].Name, childField.Name, StringComparison.OrdinalIgnoreCase))
                {
                    allFields[i] = childField;
                    overriddenParentNames.Add(childField.Name);
                    break;
                }


        foreach (var childField in binaryNode.Fields)
            if (!overriddenParentNames.Contains(childField.Name))
                allFields.Add(childField);

        return allFields;
    }

    private List<ISchemaColumn> CreateBinarySchemaColumns(
        IReadOnlyList<SchemaFieldNode> fields,
        ComputedFieldColumnTypeMode computedFieldColumnTypeMode)
    {
        return CreateBinarySchemaColumns(
            fields,
            computedFieldColumnTypeMode,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
    }

    private List<ISchemaColumn> CreateBinarySchemaColumns(
        IReadOnlyList<SchemaFieldNode> fields,
        ComputedFieldColumnTypeMode computedFieldColumnTypeMode,
        IReadOnlyDictionary<string, string> typeParamMap)
    {
        var columns = new List<ISchemaColumn>();
        var columnIndex = 0;

        foreach (var field in fields)
        {
            if (field.Name.StartsWith('_'))
                continue;

            if (field is FieldDefinitionNode { TypeAnnotation: AlignmentNode })
                continue;

            if (field is FieldDefinitionNode fieldDef)
            {
                var (columnType, intendedTypeName) = ResolveBinaryFieldClrTypeWithIntendedName(
                    fieldDef,
                    typeParamMap);

                if (fieldDef.IsConditional &&
                    columnType.IsValueType &&
                    Nullable.GetUnderlyingType(columnType) == null)
                {
                    columnType = typeof(Nullable<>).MakeGenericType(columnType);
                }

                columns.Add(new SchemaColumn(field.Name, columnIndex++, columnType, intendedTypeName));
                continue;
            }

            if (field is ComputedFieldNode computedField)
            {
                var columnType = ResolveComputedBinaryColumnType(
                    computedField,
                    columns,
                    computedFieldColumnTypeMode);

                if (ReferencesConditionalField(computedField.Expression, fields) &&
                    columnType.IsValueType &&
                    Nullable.GetUnderlyingType(columnType) == null)
                {
                    columnType = typeof(Nullable<>).MakeGenericType(columnType);
                }

                columns.Add(new SchemaColumn(field.Name, columnIndex++, columnType));
                continue;
            }

            columns.Add(new SchemaColumn(field.Name, columnIndex++, typeof(object)));
        }

        return columns;
    }

    private static Type ResolveComputedBinaryColumnType(
        ComputedFieldNode computedField,
        List<ISchemaColumn> columns,
        ComputedFieldColumnTypeMode computedFieldColumnTypeMode)
    {
        if (computedFieldColumnTypeMode == ComputedFieldColumnTypeMode.InferFromColumns)
            return InferComputedFieldType(computedField.Expression, columns);

        var expressionType = computedField.Expression.ReturnType;
        return expressionType == null || expressionType == typeof(void)
            ? InferComputedFieldType(computedField.Expression, columns)
            : expressionType;
    }

    private List<TextFieldDefinitionNode> GetAllTextSchemaFields(TextSchemaNode textNode)
    {
        if (string.IsNullOrEmpty(textNode.Extends))
            return textNode.Fields.ToList();

        var allFields = new List<TextFieldDefinitionNode>();

        if (!string.IsNullOrEmpty(textNode.Extends))
        {
            var parentSchema = RequireSchemaRegistry().GetSchema(textNode.Extends);
            if (parentSchema?.Node is TextSchemaNode parentTextNode)
                allFields.AddRange(GetAllTextSchemaFields(parentTextNode));
        }

        var overriddenParentNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var childField in textNode.Fields)
        {
            for (var i = 0; i < allFields.Count; i++)
            {
                if (!string.Equals(allFields[i].Name, childField.Name, StringComparison.OrdinalIgnoreCase))
                    continue;

                allFields[i] = childField;
                overriddenParentNames.Add(childField.Name);
                break;
            }
        }

        foreach (var childField in textNode.Fields)
        {
            if (!overriddenParentNames.Contains(childField.Name))
                allFields.Add(childField);
        }

        return allFields;
    }

    private SchemaRegistry RequireSchemaRegistry()
    {
        return SchemaRegistry ?? throw new InvalidOperationException("Schema registry is required to resolve interpretation schema inheritance.");
    }
}
