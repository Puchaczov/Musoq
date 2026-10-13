using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private static string GetClrTypeName(TypeAnnotationNode typeAnnotation)
    {
        return typeAnnotation switch
        {
            PrimitiveTypeNode p => p.TypeName switch
            {
                PrimitiveTypeName.Byte => "byte",
                PrimitiveTypeName.SByte => "sbyte",
                PrimitiveTypeName.Short => "short",
                PrimitiveTypeName.UShort => "ushort",
                PrimitiveTypeName.Int => "int",
                PrimitiveTypeName.UInt => "uint",
                PrimitiveTypeName.Long => "long",
                PrimitiveTypeName.ULong => "ulong",
                PrimitiveTypeName.Float => "float",
                PrimitiveTypeName.Double => "double",
                _ => "object"
            },
            ByteArrayTypeNode => "byte[]",
            StringTypeNode s => s.AsTextSchemaName ?? "string",
            BitsTypeNode b => GetBitsClrTypeName(b),
            SchemaReferenceTypeNode s => s.FullTypeName,
            ArrayTypeNode a => $"{GetClrTypeName(a.ElementType)}[]",
            RepeatUntilTypeNode r => $"{GetClrTypeName(r.ElementType)}[]",
            BinarySwitchTypeNode => "System.Dynamic.ExpandoObject",
            InlineSchemaTypeNode => "object",
            SubstreamTypeNode { Mode: SubstreamMode.Raw } => "byte[]",
            SubstreamTypeNode sub => GetClrTypeName(sub.Target!),
            _ => "object"
        };
    }

    private string GetClrTypeNameForFieldDefinition(FieldDefinitionNode field)
    {
        if (field.TypeAnnotation is InlineSchemaTypeNode inlineSchema)
            return GetOrRegisterInlineSchemaClassName(field.Name, inlineSchema, null);
        if (field.TypeAnnotation is ArrayTypeNode { ElementType: InlineSchemaTypeNode } arrayType)
            return GetArrayClrTypeName(field.Name, arrayType);
        if (field.TypeAnnotation is RepeatUntilTypeNode { ElementType: InlineSchemaTypeNode repeatInlineSchema })
            return GetRepeatUntilClrTypeName(field.Name, repeatInlineSchema);
        if (field.TypeAnnotation is BinarySwitchTypeNode switchType)
            return GetOrRegisterSwitchClassName(field.Name, switchType);
        if (field.TypeAnnotation is SubstreamTypeNode { Target: InlineSchemaTypeNode substreamInline })
            return GetOrRegisterInlineSchemaClassName(field.Name, substreamInline, null);
        return GetClrTypeName(field.TypeAnnotation);
    }

    private string GetClrTypeNameForFieldWithContext(SchemaFieldNode field, List<SchemaFieldNode> contextFields)
    {
        return field switch
        {
            FieldDefinitionNode { TypeAnnotation: InlineSchemaTypeNode inlineSchema } parsedField => GetOrRegisterInlineSchemaClassName(parsedField.Name, inlineSchema, null),
            FieldDefinitionNode { TypeAnnotation: ArrayTypeNode
                {
                    ElementType: InlineSchemaTypeNode
                } arrayType
            } parsedField => GetArrayClrTypeName(parsedField.Name, arrayType),
            FieldDefinitionNode { TypeAnnotation: RepeatUntilTypeNode
                {
                    ElementType: InlineSchemaTypeNode inlineSchema
                }
            } parsedField => GetRepeatUntilClrTypeName(parsedField.Name, inlineSchema),
            FieldDefinitionNode { TypeAnnotation: BinarySwitchTypeNode switchType } parsedField =>
                GetOrRegisterSwitchClassName(parsedField.Name, switchType),
            FieldDefinitionNode { TypeAnnotation: SubstreamTypeNode { Target: InlineSchemaTypeNode substreamInline } } parsedField =>
                GetOrRegisterInlineSchemaClassName(parsedField.Name, substreamInline, null),
            FieldDefinitionNode parsedField => GetClrTypeName(parsedField.TypeAnnotation),
            ComputedFieldNode computedField => InferComputedFieldTypeName(computedField.Expression, contextFields),
            _ => "object"
        };
    }

    private string GetArrayClrTypeName(string fieldName, ArrayTypeNode arrayType)
    {
        return $"{GetArrayElementClrTypeName(fieldName, arrayType.ElementType)}[]";
    }

    private string GetArrayElementClrTypeName(string fieldName, TypeAnnotationNode elementType)
    {
        return elementType is InlineSchemaTypeNode inlineSchema
            ? GetOrRegisterInlineSchemaClassName(fieldName, inlineSchema, null)
            : GetClrTypeName(elementType);
    }

    private string GetRepeatUntilClrTypeName(string fieldName, InlineSchemaTypeNode inlineSchema)
    {
        return $"{GetRepeatUntilElementClrTypeName(fieldName, inlineSchema)}[]";
    }

    private string GetRepeatUntilElementClrTypeName(string fieldName, TypeAnnotationNode elementType)
    {
        return elementType is InlineSchemaTypeNode inlineSchema
            ? GetOrRegisterInlineSchemaClassName(fieldName, inlineSchema, null)
            : GetClrTypeName(elementType);
    }

    private string InferComputedFieldTypeName(Node expression, List<SchemaFieldNode>? contextFields = null) =>
        MapClrTypeName(BinarySchemaExpressionTypes.Unwrap(BinarySchemaExpressionTypes.Resolve(expression, contextFields ?? [], _registry)));

    private static string MapClrTypeName(Type type)
    {
        return Type.GetTypeCode(type) switch
        {
            TypeCode.Byte => "byte",
            TypeCode.SByte => "sbyte",
            TypeCode.Int16 => "short",
            TypeCode.UInt16 => "ushort",
            TypeCode.Int32 => "int",
            TypeCode.UInt32 => "uint",
            TypeCode.Int64 => "long",
            TypeCode.UInt64 => "ulong",
            TypeCode.Single => "float",
            TypeCode.Double => "double",
            TypeCode.Decimal => "decimal",
            TypeCode.Boolean => "bool",
            TypeCode.String => "string",
            _ => "object"
        };
    }

    private static bool IsReferenceType(string clrTypeName)
    {
        return clrTypeName switch
        {
            "string" => true,
            "object" => true,
            _ when clrTypeName.EndsWith("[]", StringComparison.Ordinal) => true,
            _ => false
        };
    }

    private bool IsTypeParameter(string typeName)
    {
        return _currentTypeParameters.Contains(typeName, StringComparer.Ordinal);
    }

}
