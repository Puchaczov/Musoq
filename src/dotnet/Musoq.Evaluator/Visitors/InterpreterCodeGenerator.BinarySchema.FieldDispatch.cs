using System.Text;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private string GenerateFieldReadCodeInner(FieldDefinitionNode field, string? overrideLocalVar = null)
    {
        var builder = new StringBuilder();
        var localVar = overrideLocalVar ?? GetLocalVarName(field.Name);
        var typeAnnotation = field.TypeAnnotation;

        switch (typeAnnotation)
        {
            case PrimitiveTypeNode primitiveType:
                builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{localVar} = {GetPrimitiveReadMethod(primitiveType)}(data);");
                break;

            case ByteArrayTypeNode byteArrayType:
                var byteSize = GenerateSizeExpression(byteArrayType.SizeExpression);
                builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{localVar} = ReadBytes(data, {byteSize});");
                break;

            case StringTypeNode stringType:
                builder.Append(GenerateStringAssignmentCode(localVar, stringType, field.Name));
                break;

            case BitsTypeNode bitsType:
                var innerBitCount = bitsType.BitCount;
                var bitsCastType = GetBitsClrTypeName(bitsType);
                builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{localVar} = ({bitsCastType})ReadBits(data, {innerBitCount});");
                break;

            case SchemaReferenceTypeNode schemaRef:
                builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {localVar}_interpreter = new {schemaRef.FullTypeName}();");
                AppendGeneratedLine(builder, $"{localVar} = InterpretNested({localVar}_interpreter, data, \"{EscapeString(field.Name)}\");");
                break;

            case ArrayTypeNode arrayType:
                var arraySizeExpr = GenerateSizeExpression(arrayType.SizeExpression);
                var elementTypeName = GetArrayElementClrTypeName(field.Name, arrayType.ElementType);
                var loopVarInner = $"_{localVar}_i";
                builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{localVar} = new {elementTypeName}[{arraySizeExpr}];");
                builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"for (int {loopVarInner} = 0; {loopVarInner} < {arraySizeExpr}; {loopVarInner}++)");
                builder.AppendLine("{");

                if (arrayType.ElementType is PrimitiveTypeNode elemPrimitive)
                {
                    builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture,
                        $"    {localVar}[{loopVarInner}] = {GetPrimitiveReadMethod(elemPrimitive)}(data);");
                }
                else if (arrayType.ElementType is StringTypeNode elemString)
                    AppendStringArrayElementReadCode(builder, localVar, loopVarInner, elemString, field.Name);
                else if (arrayType.ElementType is SchemaReferenceTypeNode elemSchemaRef)
                {
                    var elemSchemaName = elemSchemaRef.FullTypeName;
                    var elemInterpreterVar = $"_{localVar}_elem_interpreter";
                    builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    var {elemInterpreterVar} = new {elemSchemaName}();");
                    AppendGeneratedLine(builder, $"    {localVar}[{loopVarInner}] = InterpretNested({elemInterpreterVar}, data, \"{EscapeString(field.Name)}\");");
                }
                else if (arrayType.ElementType is InlineSchemaTypeNode elemInlineSchema)
                {
                    AppendInlineArrayElementRead(
                        builder,
                        elemInlineSchema,
                        field.Name,
                        localVar,
                        1);
                    var elemVar = GetInlineArrayElementVariable(localVar);
                    builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    {localVar}[{loopVarInner}] = {elemVar};");
                }
                else
                {
                    throw CreateUnsupportedCodeGenerationException(
                        field.Name,
                        arrayType.ElementType,
                        "array element type");
                }

                builder.AppendLine("}");
                break;

            case RepeatUntilTypeNode repeatUntilType:
                builder.Append(GenerateRepeatUntilReadCode(localVar, repeatUntilType, field.Name, false));
                break;

            case AlignmentNode alignmentNode:
                builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"AlignToBits(data, {alignmentNode.AlignmentBits});");
                break;

            case InlineSchemaTypeNode inlineSchema:

                var inlineClassName = GetOrRegisterInlineSchemaClassName(field.Name, inlineSchema, null);
                builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {localVar}_interpreter = new {inlineClassName}();");
                builder.Append(GenerateOuterRefAssignments($"{localVar}_interpreter", inlineSchema));
                AppendGeneratedLine(builder, $"{localVar} = InterpretNested({localVar}_interpreter, data, \"{EscapeString(field.Name)}\");");
                break;

            case BinarySwitchTypeNode switchType:
                builder.Append(GenerateBinarySwitchReadCodeInner(localVar, field.Name, switchType));
                break;

            case SubstreamTypeNode substreamType:
                builder.Append(GenerateSubstreamReadCodeInner(localVar, field.Name, substreamType));
                break;

            default:
                throw CreateUnsupportedCodeGenerationException(
                    field.Name,
                    typeAnnotation,
                    "type annotation");
        }

        return builder.ToString();
    }

    private string GenerateFieldReadCode(FieldDefinitionNode field)
    {
        var builder = new StringBuilder();
        var localVar = GetLocalVarName(field.Name);
        var typeAnnotation = field.TypeAnnotation;

        switch (typeAnnotation)
        {
            case PrimitiveTypeNode primitiveType:
                builder.AppendLine(GeneratePrimitiveReadCode(localVar, primitiveType));
                break;

            case ByteArrayTypeNode byteArrayType:
                builder.AppendLine(GenerateByteArrayReadCode(localVar, byteArrayType));
                break;

            case StringTypeNode stringType:
                builder.AppendLine(GenerateStringReadCode(localVar, stringType, field.Name));
                break;

            case BitsTypeNode bitsType:
                builder.AppendLine(GenerateBitsReadCode(localVar, bitsType));
                break;

            case SchemaReferenceTypeNode schemaRef:
                builder.AppendLine(GenerateSchemaReferenceReadCode(localVar, field.Name, schemaRef));
                break;

            case ArrayTypeNode arrayType:
                builder.AppendLine(GenerateArrayReadCode(localVar, field.Name, arrayType));
                break;

            case RepeatUntilTypeNode repeatUntilType:
                builder.AppendLine(GenerateRepeatUntilReadCode(localVar, repeatUntilType, field.Name));
                break;

            case AlignmentNode alignmentNode:
                builder.AppendLine(GenerateAlignmentCode(alignmentNode));
                break;

            case InlineSchemaTypeNode inlineSchema:
                builder.AppendLine(GenerateInlineSchemaReadCode(localVar, field.Name, inlineSchema));
                break;

            case BinarySwitchTypeNode switchType:
                builder.AppendLine(GenerateBinarySwitchReadCode(localVar, field.Name, switchType));
                break;

            case SubstreamTypeNode substreamType:
                builder.AppendLine(GenerateSubstreamReadCode(localVar, field.Name, substreamType));
                break;

            default:
                throw CreateUnsupportedCodeGenerationException(
                    field.Name,
                    typeAnnotation,
                    "type annotation");
        }

        return builder.ToString();
    }

}
