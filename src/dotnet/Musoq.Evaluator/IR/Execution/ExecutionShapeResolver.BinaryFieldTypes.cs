using System.Collections.Generic;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionShapeResolver
{
    private static Type ResolveBinaryFieldType(
        SchemaFieldNode field,
        Type? generatedType,
        BinaryColumnResolutionContext context, IReadOnlyList<SchemaFieldNode> fields)
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
            return BinarySchemaExpressionTypes.Resolve(computed.Expression, fields, context.SchemaRegistry);

        var fieldType = field.ReturnType;
        return fieldType == null || fieldType == typeof(void) ? typeof(object) : fieldType;
    }
}
