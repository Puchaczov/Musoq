using System.Text;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
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
