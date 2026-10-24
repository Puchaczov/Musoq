using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private string GenerateFieldReadCodeInner(FieldDefinitionNode field, string? overrideLocalVar = null)
    {
        var localVar = overrideLocalVar ?? GetLocalVarName(field.Name);
        if (field.TypeAnnotation is AlignmentNode alignment)
            return GenerateAlignmentCode(alignment);
        var value = $"{localVar}_value";
        return GenerateBinaryValueRead(value, field.Name, field.TypeAnnotation) + $"{localVar} = {value};\n";
    }

    private string GenerateFieldReadCode(FieldDefinitionNode field) =>
        GenerateBinaryValueRead(GetLocalVarName(field.Name), field.Name, field.TypeAnnotation);

    private string GenerateBinaryValueRead(string localVar, string fieldName, TypeAnnotationNode type) => type switch
    {
        PrimitiveTypeNode primitive => GeneratePrimitiveReadCode(localVar, primitive),
        ByteArrayTypeNode bytes => GenerateByteArrayReadCode(localVar, bytes),
        StringTypeNode text => GenerateStringReadCode(localVar, text, fieldName),
        BitsTypeNode bits => GenerateBitsReadCode(localVar, bits),
        SchemaReferenceTypeNode reference => GenerateSchemaReferenceReadCode(localVar, fieldName, reference),
        ArrayTypeNode array => GenerateArrayReadCode(localVar, fieldName, array),
        RepeatUntilTypeNode repeat => GenerateRepeatUntilReadCode(localVar, repeat, fieldName),
        AlignmentNode alignment => GenerateAlignmentCode(alignment),
        InlineSchemaTypeNode inline => GenerateInlineSchemaReadCode(localVar, fieldName, inline),
        BinarySwitchTypeNode choice => GenerateBinarySwitchReadCode(localVar, fieldName, choice),
        SubstreamTypeNode substream => GenerateSubstreamReadCode(localVar, fieldName, substream),
        _ => throw CreateUnsupportedCodeGenerationException(fieldName, type, "binary value type")
    } + "\n";
}
