using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.TemporarySchemas;
using Musoq.Parser;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using static Musoq.Evaluator.Visitors.BuildMetadataAndInferTypesVisitorUtilities;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private ISchemaTable CreatePartialInterpretTable(string schemaName)
    {
        return new DynamicTable(
            Musoq
                .Evaluator.IR.Bindings.PartialInterpretationShape.CreateColumns(schemaName)
                .Select(static column => column.ToSchemaColumn())
                .ToArray()
        );
    }

    private ISchemaTable CreateInterpretTable(string? schemaName)
    {
        if (schemaName == null || SchemaRegistry == null)
            throw new InvalidOperationException(
                $"Cannot create interpret table: schema name is '{schemaName ?? "null"}' and schema registry is {(SchemaRegistry != null ? "present" : "null")}."
            );

        if (!SchemaRegistry.TryGetSchema(schemaName, out var registration) || registration == null)
            throw new UnknownInterpretationSchemaException(
                schemaName,
                $"Interpretation schema '{schemaName}' is not defined.",
                TextSpan.Empty
            );

        var schema = registration;
        if (schema.Node is BinarySchemaNode)
            SchemaRegistry.ParentBindings.RequireRoot(schemaName, TextSpan.Empty);
        var columns = new List<ISchemaColumn>();

        switch (schema.Node)
        {
            case BinarySchemaNode binaryNode:
            {
                var allFields = GetAllBinarySchemaFields(binaryNode);
                columns.AddRange(CreateBinarySchemaColumns(allFields));
                break;
            }
            case TextSchemaNode textSchemaNode:
                columns.AddRange(CreateTextSchemaColumns(textSchemaNode));
                break;
        }

        if (columns.Count == 0)
            return CreateEmptyTable();

        return new DynamicTable(columns.ToArray(), caseSensitive: true);
    }

    private List<SchemaFieldNode> GetAllBinarySchemaFields(BinarySchemaNode binaryNode) =>
        InterpretationSchemaFields.GetFields(binaryNode, RequireSchemaRegistry());

    private List<ISchemaColumn> CreateBinarySchemaColumns(IReadOnlyList<SchemaFieldNode> fields)
    {
        return CreateBinarySchemaColumns(fields, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
    }

    private List<ISchemaColumn> CreateBinarySchemaColumns(
        IReadOnlyList<SchemaFieldNode> fields,
        IReadOnlyDictionary<string, string> typeParamMap
    )
    {
        var columns = new List<ISchemaColumn>();
        var columnIndex = 0;

        foreach (var field in fields)
        {
            if (field.Name == "_")
                continue;

            if (field is FieldDefinitionNode { TypeAnnotation: AlignmentNode })
                continue;

            if (field is FieldDefinitionNode fieldDef)
            {
                var (columnType, intendedTypeName) = ResolveBinaryFieldClrTypeWithIntendedName(fieldDef, typeParamMap);

                if (fieldDef.IsConditional && columnType.IsValueType && Nullable.GetUnderlyingType(columnType) == null)
                {
                    columnType = typeof(Nullable<>).MakeGenericType(columnType);
                }

                columns.Add(
                    InterpretationColumn.Preserve(
                        new SchemaColumn(field.Name, columnIndex++, columnType, intendedTypeName),
                        fieldDef.IsConditional,
                        minimumArrayLength: columnType.IsArray && intendedTypeName != null
                            ? BinarySchemaExpressionTypes.MinimumArrayLength(fieldDef.TypeAnnotation)
                            : 0
                    )
                );
                continue;
            }

            if (field is ComputedFieldNode computedField)
            {
                var columnType = BinarySchemaExpressionTypes.Resolve(computedField.Expression, fields, SchemaRegistry);
                var alias = BinarySchemaRecordAliases.Resolve(computedField.Expression, fields, SchemaRegistry);
                var intendedTypeName = alias is null
                    ? null
                    : ResolveBinaryFieldClrTypeWithIntendedName(alias, typeParamMap).IntendedTypeName;
                columns.Add(
                    InterpretationColumn.Preserve(
                        new SchemaColumn(field.Name, columnIndex++, columnType, intendedTypeName),
                        (intendedTypeName != null || !columnType.IsValueType)
                            && BinarySchemaExpressionTypes.MayBeNull(computedField.Expression, fields, SchemaRegistry),
                        minimumArrayLength: columnType.IsArray && intendedTypeName != null
                            ? BinarySchemaExpressionTypes.MinimumArrayLength(alias?.TypeAnnotation)
                            : 0
                    )
                );
                continue;
            }

            columns.Add(new SchemaColumn(field.Name, columnIndex++, typeof(object)));
        }

        return columns;
    }

    private List<TextFieldDefinitionNode> GetAllTextSchemaFields(TextSchemaNode schema) =>
        InterpretationSchemaFields.GetFields(schema, SchemaRegistry);

    private ISchemaColumn[] CreateTextSchemaColumns(TextSchemaNode schema) =>
        GetAllTextSchemaFields(schema)
            .Where(static field => !field.IsDiscard)
            .Select(
                (field, index) =>
                {
                    var shape = InterpretationTextFieldTypeResolver.Resolve(schema.Name, field, SchemaRegistry);
                    return InterpretationColumn.Preserve(
                        new SchemaColumn(field.Name, index, shape.ClrType, shape.IntendedTypeName),
                        InterpretationTextFieldTypeResolver.MayBeNull(field),
                        shape.ClrType.IsArray && shape.ClrType.GetElementType()?.IsValueType == false
                    );
                }
            )
            .ToArray();

    private SchemaRegistry RequireSchemaRegistry()
    {
        return SchemaRegistry
            ?? throw new InvalidOperationException(
                "Schema registry is required to resolve interpretation schema inheritance."
            );
    }
}
