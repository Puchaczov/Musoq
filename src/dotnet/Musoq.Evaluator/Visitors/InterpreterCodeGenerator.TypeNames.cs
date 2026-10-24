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

    private string GetClrTypeNameForFieldDefinition(FieldDefinitionNode field) =>
        GetBinaryValueClrTypeName(field.Name, field.TypeAnnotation);

    private string GetBinaryValueClrTypeName(string fieldName, TypeAnnotationNode type) => type switch
    {
        InlineSchemaTypeNode inline => GetOrRegisterInlineSchemaClassName(fieldName, inline, null),
        BinarySwitchTypeNode choice => GetOrRegisterSwitchClassName(fieldName, choice),
        ArrayTypeNode array => $"{GetBinaryValueClrTypeName(fieldName, array.ElementType)}[]",
        RepeatUntilTypeNode repeat => $"{GetBinaryValueClrTypeName(fieldName, repeat.ElementType)}[]",
        SubstreamTypeNode { Target: { } target } => GetBinaryValueClrTypeName(fieldName, target),
        _ => GetClrTypeName(type)
    };

    private string GetClrTypeNameForFieldWithContext(SchemaFieldNode field, List<SchemaFieldNode> contextFields) => field switch
    {
        FieldDefinitionNode parsed => GetClrTypeNameForFieldDefinition(parsed),
        ComputedFieldNode computed => InferComputedFieldTypeName(computed.Expression, contextFields),
        _ => "object"
    };

    private string InferComputedFieldTypeName(Node expression, List<SchemaFieldNode>? contextFields = null) =>
        BinarySchemaRecordAliases.Resolve(expression, contextFields ?? []) is { } record
            ? GetClrTypeNameForFieldDefinition(record)
            : MapClrTypeName(BinarySchemaExpressionTypes.Unwrap(BinarySchemaExpressionTypes.Resolve(expression, contextFields ?? [], _registry)));

    private static string MapClrTypeName(Type type)
        => BinarySchemaClrTypeNames.Map(type);

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
