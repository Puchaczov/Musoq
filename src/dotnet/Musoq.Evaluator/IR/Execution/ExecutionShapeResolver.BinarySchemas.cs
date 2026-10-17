using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionShapeResolver
{
    private static IReadOnlyList<ColumnSchema> CreateBinaryColumns(
        BinarySchemaNode binary,
        SchemaRegistry? schemaRegistry,
        Type? generatedType)
    {
        return CreateBinaryColumns(
            binary,
            generatedType,
            new BinaryColumnResolutionContext(
                schemaRegistry,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                BinarySchemaGenericResolver.CreateEmptyBindings()));
    }

    private static IReadOnlyList<ColumnSchema> CreateBinaryColumns(
        BinarySchemaNode binary,
        Type? generatedType,
        BinaryColumnResolutionContext context)
    {
        var schemaKey = BinarySchemaGenericResolver.CreateSchemaKey(binary, context.GenericBindings);
        if (!context.ExpandedSchemas.Add(schemaKey))
            return Array.Empty<ColumnSchema>();

        try
        {
            var fieldsByName = new Dictionary<string, SchemaFieldNode>(StringComparer.OrdinalIgnoreCase);

            AddInheritedBinaryFields(binary, fieldsByName, context.SchemaRegistry);

            foreach (var field in binary.Fields)
                fieldsByName[field.Name] = field;

            return CreateBinaryColumnsFromFields(fieldsByName.Values, generatedType, context);
        }
        finally
        {
            context.ExpandedSchemas.Remove(schemaKey);
        }
    }
    private static List<ColumnSchema> CreateInlineBinaryColumns(
        InlineSchemaTypeNode inlineSchema,
        Type? generatedType,
        BinaryColumnResolutionContext context)
    {
        return CreateBinaryColumnsFromFields(
            inlineSchema.Fields,
            generatedType,
            context with { GenericBindings = BinarySchemaGenericResolver.CreateEmptyBindings() });
    }
    private static List<ColumnSchema> CreateBinaryColumnsFromFields(
        IEnumerable<SchemaFieldNode> fields,
        Type? generatedType,
        BinaryColumnResolutionContext context)
    {
        var columns = new List<ColumnSchema>();
        var topLevelIndex = 0;
        var schemaFields = fields.ToArray();

        foreach (var field in schemaFields)
        {
            if (field.Name.StartsWith('_'))
                continue;

            if (field is FieldDefinitionNode { TypeAnnotation: AlignmentNode })
                continue;

            var fieldType = ResolveBinaryFieldType(field, generatedType, context, schemaFields);

            if (field is FieldDefinitionNode { IsConditional: true } &&
                fieldType.IsValueType &&
                Nullable.GetUnderlyingType(fieldType) == null)
            {
                fieldType = typeof(Nullable<>).MakeGenericType(fieldType);
            }

            var shapeField = field is ComputedFieldNode computed
                ? (SchemaFieldNode?)Visitors.BinarySchemaRecordAliases.Resolve(computed.Expression, schemaFields) ?? field : field;
            var intendedTypeName = InterpretationPropertyTypeNameResolver.ResolveFieldTypeName(
                shapeField,
                context.GenericBindings,
                context.SchemaRegistry);
            var column = new ColumnSchema(field.Name, fieldType, topLevelIndex, intendedTypeName);
            columns.Add(column);
            AddNestedBinaryColumns(shapeField, columns, column, context);
            topLevelIndex++;
        }

        return columns;
    }
    private static Type ResolveSchemaReferenceFieldType(
        SchemaReferenceTypeNode reference,
        BinaryColumnResolutionContext context)
    {
        var resolvedReference = BinarySchemaGenericResolver.ResolveReference(reference, context.GenericBindings);

        if (!resolvedReference.IsGenericInstantiation &&
            context.SchemaRegistry != null &&
            context.SchemaRegistry.TryGetSchema(resolvedReference.SchemaName, out var registration) &&
            registration?.GeneratedType != null)
        {
            return registration.GeneratedType;
        }

        return typeof(object);
    }

    private static void AddInheritedBinaryFields(
        BinarySchemaNode binary,
        IDictionary<string, SchemaFieldNode> fieldsByName,
        SchemaRegistry? schemaRegistry)
    {
        if (string.IsNullOrWhiteSpace(binary.Extends) ||
            schemaRegistry == null ||
            !schemaRegistry.TryGetSchema(binary.Extends, out var registration) ||
            registration?.Node is not BinarySchemaNode parent)
        {
            return;
        }

        AddInheritedBinaryFields(parent, fieldsByName, schemaRegistry);

        foreach (var field in parent.Fields)
            fieldsByName[field.Name] = field;
    }

    private sealed record NestedBinarySchema(
        SchemaRegistration Registration,
        BinarySchemaNode Schema,
        IReadOnlyDictionary<string, SchemaReferenceTypeNode> GenericBindings);

    private sealed record BinaryColumnResolutionContext(
        SchemaRegistry? SchemaRegistry,
        ISet<string> ExpandedSchemas,
        IReadOnlyDictionary<string, SchemaReferenceTypeNode> GenericBindings);
}
