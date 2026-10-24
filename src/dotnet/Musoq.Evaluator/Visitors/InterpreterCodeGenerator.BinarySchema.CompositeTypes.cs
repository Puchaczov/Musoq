using System.Text;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private string GenerateSchemaReferenceReadCode(string localVar, string fieldName,
        SchemaReferenceTypeNode schemaRef)
    {
        var typeName = schemaRef.FullTypeName;
        var tempInterpreter = $"_{localVar}_interpreter";
        var builder = new StringBuilder();
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {tempInterpreter} = {CreateNestedInterpreter(typeName)};");
        AppendGeneratedLine(builder, $"var {localVar} = InterpretNested({tempInterpreter}, data, \"{EscapeString(fieldName)}\");");
        return builder.ToString();
    }

    private string GenerateArrayReadCode(string localVar, string fieldName, ArrayTypeNode arrayType)
    {
        var builder = new StringBuilder();
        var countExpr = GenerateSizeExpression(arrayType.SizeExpression);
        var countVar = $"_{localVar}_count";
        var elementTypeName = GetBinaryValueClrTypeName(fieldName, arrayType.ElementType);
        var loopVar = $"_{localVar}_i";
        var tempVar = $"_{localVar}_list";

        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {tempVar} = new System.Collections.Generic.List<{elementTypeName}>();");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {countVar} = {countExpr};");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"for (int {loopVar} = 0; {loopVar} < {countVar}; {loopVar}++)");
        builder.AppendLine("{");

        var elementVar = $"_{localVar}_elem";
        builder.Append(Indent(GenerateBinaryValueRead(elementVar, fieldName, arrayType.ElementType), 1));
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    {tempVar}.Add({elementVar});");

        builder.AppendLine("}");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {localVar} = {tempVar}.ToArray();");

        return builder.ToString();
    }

    private static string GenerateAlignmentCode(AlignmentNode alignmentNode)
    {
        var bits = alignmentNode.AlignmentBits;
        return $"AlignToBits(data, {bits});";
    }

    private ConstructionNotYetSupported CreateUnsupportedCodeGenerationException(
        string fieldName,
        TypeAnnotationNode unsupportedType,
        string role)
    {
        return new ConstructionNotYetSupported(
            $"Unsupported interpretation schema code generation for schema '{_currentSchemaName}', field '{fieldName}', {role} '{unsupportedType.GetType().Name}'.",
            DiagnosticCode.MQ4016_UnsupportedSchemaConstruction);
    }
}
