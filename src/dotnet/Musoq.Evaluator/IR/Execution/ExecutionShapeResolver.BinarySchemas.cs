using System.Collections.Generic;
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

        foreach (var field in fields)
        {
            if (field.Name.StartsWith('_'))
                continue;

            if (field is FieldDefinitionNode { TypeAnnotation: AlignmentNode })
                continue;

            var fieldType = ResolveBinaryFieldType(field, generatedType, context);

            if (field is FieldDefinitionNode { IsConditional: true } &&
                fieldType.IsValueType &&
                Nullable.GetUnderlyingType(fieldType) == null)
            {
                fieldType = typeof(Nullable<>).MakeGenericType(fieldType);
            }

            var column = new ColumnSchema(field.Name, fieldType, topLevelIndex);
            columns.Add(column);
            AddNestedBinaryColumns(field, columns, column, context);
            topLevelIndex++;
        }

        return columns;
    }

    private static Type ResolveBinaryFieldType(
        SchemaFieldNode field,
        Type? generatedType,
        BinaryColumnResolutionContext context)
    {
        if (TryGetGeneratedFieldType(generatedType, field.Name, out var generatedFieldType))
            return generatedFieldType;

        if (field is FieldDefinitionNode { TypeAnnotation: SchemaReferenceTypeNode reference })
            return ResolveSchemaReferenceFieldType(reference, context);

        if (field is FieldDefinitionNode
            {
                TypeAnnotation: ArrayTypeNode
                {
                    ElementType: SchemaReferenceTypeNode arrayElementReference
                }
            })
        {
            var elementType = ResolveSchemaReferenceFieldType(arrayElementReference, context);
            return elementType.MakeArrayType();
        }

        if (field is FieldDefinitionNode
            {
                TypeAnnotation: RepeatUntilTypeNode
                {
                    ElementType: SchemaReferenceTypeNode repeatElementReference
                }
            })
        {
            var elementType = ResolveSchemaReferenceFieldType(repeatElementReference, context);
            return elementType.MakeArrayType();
        }

        var fieldType = field.ReturnType;
        return fieldType == null || fieldType == typeof(void) ? typeof(object) : fieldType;
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

    private static bool TryGetGeneratedFieldType(Type? generatedType, string fieldName, out Type fieldType)
    {
        var property = generatedType?.GetProperty(fieldName);
        if (property != null)
        {
            fieldType = property.PropertyType;
            return true;
        }

        var field = generatedType?.GetField(fieldName);
        if (field == null)
        {
            fieldType = typeof(object);
            return false;
        }

        fieldType = field.FieldType;
        return true;
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
