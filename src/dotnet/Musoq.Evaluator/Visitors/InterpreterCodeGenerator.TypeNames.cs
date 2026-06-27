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

    private static string GetClrTypeNameForField(SchemaFieldNode field)
    {
        return field switch
        {
            FieldDefinitionNode parsedField => GetClrTypeName(parsedField.TypeAnnotation),
            ComputedFieldNode computedField => InferComputedFieldTypeNameStatic(computedField.Expression),
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

    private static string InferComputedFieldTypeNameStatic(Node expression)
    {
        if (expression is EqualityNode or DiffNode or GreaterNode or GreaterOrEqualNode
            or LessNode or LessOrEqualNode or AndNode or OrNode)
            return "bool";

        if (expression is WordNode)
            return "string";

        if (expression is AccessMethodNode methodNode)
            if (methodNode.Name.Equals("ToString", StringComparison.OrdinalIgnoreCase))
                return "string";

        if (expression is AddNode addNode)
        {
            var leftType = InferComputedFieldTypeNameStatic(addNode.Left);
            var rightType = InferComputedFieldTypeNameStatic(addNode.Right);
            if (leftType == "string" || rightType == "string")
                return "string";
        }

        if (expression is AddNode or HyphenNode or StarNode or FSlashNode or ModuloNode) return "int";

        return "object";
    }

    private string InferComputedFieldTypeName(Node expression, List<SchemaFieldNode>? contextFields = null)
    {
        if (expression is EqualityNode or DiffNode or GreaterNode or GreaterOrEqualNode
            or LessNode or LessOrEqualNode or AndNode or OrNode)
            return "bool";

        if (expression is AddNode addNode)
        {
            var leftType = InferExpressionType(addNode.Left, contextFields);
            var rightType = InferExpressionType(addNode.Right, contextFields);
            if (leftType == "string" || rightType == "string")
                return "string";
        }

        if (expression is BinaryNode binaryNode && contextFields != null)
        {
            var leftType = InferOperandType(binaryNode.Left, contextFields);
            var rightType = InferOperandType(binaryNode.Right, contextFields);

            return GetWiderArithmeticType(leftType, rightType);
        }

        var returnType = expression.ReturnType;
        if (returnType != null && returnType != typeof(object) && returnType != typeof(void))
            return MapClrTypeName(returnType);

        if (expression is AddNode or HyphenNode or StarNode or FSlashNode or ModuloNode
            or BitwiseAndNode or BitwiseOrNode or BitwiseXorNode or LeftShiftNode or RightShiftNode)
            return "int";

        if (expression is WordNode)
            return "string";

        if (expression is AccessMethodNode methodNode)
            if (methodNode.Name.Equals("ToString", StringComparison.OrdinalIgnoreCase))
                return "string";

        return "object";
    }

    private static string InferExpressionType(Node node, List<SchemaFieldNode>? contextFields)
    {
        return node switch
        {
            WordNode => "string",
            AccessMethodNode method when method.Name.Equals("ToString", StringComparison.OrdinalIgnoreCase) => "string",
            IntegerNode or HexIntegerNode => "int",
            DecimalNode => "decimal",
            IdentifierNode id => contextFields?.FirstOrDefault(f =>
                f.Name.Equals(id.Name, StringComparison.OrdinalIgnoreCase)) is { } field
                ? GetClrTypeNameForField(field)
                : "object",
            AddNode add => InferExpressionType(add.Left, contextFields) == "string" ||
                           InferExpressionType(add.Right, contextFields) == "string"
                ? "string"
                : "int",
            _ => "object"
        };
    }

    private string InferOperandType(Node operand, List<SchemaFieldNode> contextFields)
    {
        if (operand is BinaryNode binaryOp) return InferComputedFieldTypeName(binaryOp, contextFields);

        if (operand is IdentifierNode identifier)
        {
            var field = contextFields.FirstOrDefault(f =>
                f.Name.Equals(identifier.Name, StringComparison.OrdinalIgnoreCase));

            if (field != null) return GetClrTypeNameForField(field);
        }

        if (operand is IntegerNode or HexIntegerNode) return "int";

        return "object";
    }

    private static string GetWiderArithmeticType(string left, string right)
    {
        var typeOrder = new[] { "byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong", "float", "double" };
        var leftIndex = Array.IndexOf(typeOrder, left);
        var rightIndex = Array.IndexOf(typeOrder, right);

        if (leftIndex < 0 && rightIndex < 0) return "int";
        if (leftIndex < 0) return right;
        if (rightIndex < 0) return left;

        return leftIndex > rightIndex ? left : right;
    }

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
