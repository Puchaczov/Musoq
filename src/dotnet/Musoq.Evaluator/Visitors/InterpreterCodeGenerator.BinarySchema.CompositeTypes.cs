using System.Text;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private static string GenerateSchemaReferenceReadCode(string localVar, string fieldName,
        SchemaReferenceTypeNode schemaRef)
    {
        var typeName = schemaRef.FullTypeName;
        var tempInterpreter = $"_{localVar}_interpreter";
        var builder = new StringBuilder();
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {tempInterpreter} = new {typeName}();");
        AppendGeneratedLine(builder, $"var {localVar} = InterpretNested({tempInterpreter}, data, \"{EscapeString(fieldName)}\");");
        return builder.ToString();
    }

    private string GenerateArrayReadCode(string localVar, string fieldName, ArrayTypeNode arrayType)
    {
        var builder = new StringBuilder();
        var countExpr = GenerateSizeExpression(arrayType.SizeExpression);
        var elementTypeName = GetArrayElementClrTypeName(fieldName, arrayType.ElementType);
        var loopVar = $"_{localVar}_i";
        var tempVar = $"_{localVar}_list";

        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {tempVar} = new System.Collections.Generic.List<{elementTypeName}>();");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"for (int {loopVar} = 0; {loopVar} < {countExpr}; {loopVar}++)");
        builder.AppendLine("{");

        if (arrayType.ElementType is PrimitiveTypeNode primitiveElement)
        {
            var readMethod = GetPrimitiveReadMethod(primitiveElement);
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    {tempVar}.Add({readMethod}(data));");
        }
        else if (arrayType.ElementType is StringTypeNode stringElement)
        {
            var elemVar = $"_{localVar}_elem";
            builder.Append(Indent(GenerateStringDeclarationCode(elemVar, stringElement, fieldName), 1));
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    {tempVar}.Add({elemVar});");
        }
        else if (arrayType.ElementType is SchemaReferenceTypeNode schemaRefElement)
        {
            var typeName = schemaRefElement.FullTypeName;
            var tempInterpreter = $"_{localVar}_elemInterpreter";
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    var {tempInterpreter} = new {typeName}();");
            AppendGeneratedLine(builder, $"    var _elem = InterpretNested({tempInterpreter}, data, \"{EscapeString(fieldName)}\");");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    {tempVar}.Add(_elem);");
        }
        else if (arrayType.ElementType is InlineSchemaTypeNode inlineSchemaElement)
        {
            AppendInlineArrayElementRead(
                builder,
                inlineSchemaElement,
                fieldName,
                tempVar,
                1);
            var elemVar = GetInlineArrayElementVariable(tempVar);
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    {tempVar}.Add({elemVar});");
        }
        else
        {
            throw CreateUnsupportedCodeGenerationException(
                fieldName,
                arrayType.ElementType,
                "array element type");
        }

        builder.AppendLine("}");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {localVar} = {tempVar}.ToArray();");

        return builder.ToString();
    }

    private void AppendInlineArrayElementRead(
        StringBuilder builder,
        InlineSchemaTypeNode inlineSchema,
        string fieldName,
        string localVar,
        int indentationLevel)
    {
        var indent = new string(' ', indentationLevel * 4);
        var inlineClassName = GetOrRegisterInlineSchemaClassName(fieldName, inlineSchema, null);
        var tempInterpreter = GetInlineArrayElementInterpreterVariable(localVar);
        var elemVar = GetInlineArrayElementVariable(localVar);

        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{indent}var {tempInterpreter} = new {inlineClassName}();");

        var outerAssignments = GenerateOuterRefAssignments(tempInterpreter, inlineSchema);
        if (!string.IsNullOrEmpty(outerAssignments))
            builder.AppendLine(Indent(outerAssignments, indentationLevel));

        AppendGeneratedLine(builder, $"{indent}var {elemVar} = InterpretNested({tempInterpreter}, data, \"{EscapeString(fieldName)}\");");
    }

    private static string GetInlineArrayElementInterpreterVariable(string localVar)
    {
        return $"_{localVar}_elemInterpreter";
    }

    private static string GetInlineArrayElementVariable(string localVar)
    {
        return $"_{localVar}_elem";
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
