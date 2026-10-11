using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionShapeResolver
{
    private static Type ResolveBinaryFieldType(
        SchemaFieldNode field,
        Type? generatedType,
        BinaryColumnResolutionContext context, IReadOnlyList<ColumnSchema> columns)
    {
        var generatedFieldType = GeneratedInterpreterFieldType.Find(generatedType, field.Name);
        if (generatedFieldType is not null)
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

        if (field is ComputedFieldNode computed)
            return BuildMetadataAndInferTypesVisitorUtilities.InferComputedFieldType(computed.Expression,
                columns.Select(static column => column.ToSchemaColumn()).ToList());

        var fieldType = field.ReturnType;
        return fieldType == null || fieldType == typeof(void) ? typeof(object) : fieldType;
    }
}
