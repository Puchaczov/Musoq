using System.Text;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private static string GenerateArrayAllocation(string elementType, string count)
    {
        var suffix = string.Empty;
        while (elementType.EndsWith("[]", StringComparison.Ordinal))
        {
            elementType = elementType[..^2];
            suffix += "[]";
        }
        return $"new {elementType}[{count}]{suffix}";
    }

    private string GenerateCollectionPayloadRead(string localVar, string fieldName, TypeAnnotationNode type) => type switch
    {
        ByteArrayTypeNode bytes => GenerateByteArrayReadCode(localVar, bytes),
        SubstreamTypeNode substream => GenerateSubstreamReadCode(localVar, fieldName, substream),
        _ => throw CreateUnsupportedCodeGenerationException(fieldName, type, "collection payload")
    };

    private static void AppendGeneratedLine(StringBuilder builder, string line)
    {
        builder.AppendLine(line);
    }

    private void AppendStringArrayElementReadCode(
        StringBuilder builder,
        string localVar,
        string loopVar,
        StringTypeNode stringType,
        string fieldName)
    {
        var elementVar = $"_{localVar}_elem";
        builder.Append(Indent(GenerateStringDeclarationCode(elementVar, stringType, fieldName), 1));
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture,
            $"    {localVar}[{loopVar}] = {elementVar};");
    }
}
