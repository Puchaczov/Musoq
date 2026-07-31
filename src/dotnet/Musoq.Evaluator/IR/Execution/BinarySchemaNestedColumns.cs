using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionShapeResolver
{
    private static void AddNestedBinaryColumns(
        SchemaFieldNode field,
        List<ColumnSchema> columns,
        ColumnSchema parentColumn,
        BinaryColumnResolutionContext context)
    {
        if (field is not FieldDefinitionNode definition)
            return;

        if (definition.TypeAnnotation is InlineSchemaTypeNode inlineSchema)
        {
            foreach (var nestedColumn in CreateInlineBinaryColumns(
                         inlineSchema,
                         parentColumn.Type,
                         context))
            {
                columns.Add(new ColumnSchema(
                    $"{field.Name}.{nestedColumn.Name}",
                    nestedColumn.Type,
                    parentColumn.Index,
                    nestedColumn.IntendedTypeName));
            }

            return;
        }

        if (definition.TypeAnnotation is not SchemaReferenceTypeNode reference)
            return;

        var resolvedReference = BinarySchemaGenericResolver.ResolveReference(reference, context.GenericBindings);
        var nestedSchema = ResolveNestedBinarySchema(resolvedReference, context);
        if (nestedSchema == null)
            return;

        var (registration, nestedBinary, nestedBindings) = nestedSchema;
        var nestedType = parentColumn.Type == typeof(object)
            ? registration.GeneratedType
            : parentColumn.Type;

        foreach (var nestedColumn in CreateBinaryColumns(
                     nestedBinary,
                     nestedType,
                     context with { GenericBindings = nestedBindings }))
        {
            columns.Add(new ColumnSchema(
                $"{field.Name}.{nestedColumn.Name}",
                nestedColumn.Type,
                parentColumn.Index,
                nestedColumn.IntendedTypeName));
        }
    }

    private static NestedBinarySchema? ResolveNestedBinarySchema(
        SchemaReferenceTypeNode reference,
        BinaryColumnResolutionContext context)
    {
        if (context.SchemaRegistry == null ||
            !context.SchemaRegistry.TryGetSchema(reference.SchemaName, out var registration) ||
            registration?.Node is not BinarySchemaNode nestedBinary)
            return null;

        if (!reference.IsGenericInstantiation)
        {
            return new NestedBinarySchema(
                registration,
                nestedBinary,
                BinarySchemaGenericResolver.CreateEmptyBindings());
        }

        if (!nestedBinary.IsGeneric || nestedBinary.TypeParameters.Length != reference.TypeArguments.Length)
            return null;

        var nestedBindings = BinarySchemaGenericResolver.CreateBindings(
            nestedBinary,
            reference,
            context.GenericBindings);
        return new NestedBinarySchema(registration, nestedBinary, nestedBindings);
    }
}
