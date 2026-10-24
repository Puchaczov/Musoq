using System.Text;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private string GenerateSubstreamReadCode(string localVar, string fieldName, SubstreamTypeNode substreamType)
    {
        if (substreamType.Mode == SubstreamMode.Raw)
            return $"var {localVar} = ReadBytes(data, {GenerateSizeExpression(substreamType.SizeExpression)});";

        return GenerateStructuredSubstreamReadCode(localVar, fieldName, substreamType);
    }

    private string GenerateStructuredSubstreamReadCode(string localVar, string fieldName, SubstreamTypeNode substreamType)
    {
        var sizeExpr = GenerateSizeExpression(substreamType.SizeExpression);
        var lengthVar = $"_{localVar}_substreamLength";
        var sliceVar = $"_{localVar}_substreamSlice";
        var interpreterVar = $"_{localVar}_substreamInterpreter";
        var startVar = $"_{localVar}_substreamStart";
        var culture = System.Globalization.CultureInfo.InvariantCulture;

        var builder = new StringBuilder();
        builder.AppendLine(culture, $"var {lengthVar} = {sizeExpr};");
        builder.AppendLine(culture, $"var {sliceVar} = ReadSubstreamSlice(data, {lengthVar});");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {startVar} = ParsePosition;");
        builder.Append(GenerateSubstreamInterpreterConstruction(interpreterVar, fieldName, substreamType.Target!));
        AppendGeneratedLine(builder, $"var {localVar} = InterpretNestedAt({interpreterVar}, {sliceVar}, 0, \"{EscapeString(fieldName)}\");");

        if (substreamType.Mode == SubstreamMode.Exact)
            builder.AppendLine(culture, $"EnsureSubstreamFullyConsumed(\"{fieldName}\", {lengthVar}, {interpreterVar});");

        builder.Append(culture, $"ParsePosition = {startVar} + {lengthVar}; BitOffset = 0;");
        return builder.ToString();
    }

    private string GenerateSubstreamInterpreterConstruction(string interpreterVar, string fieldName, TypeAnnotationNode target)
    {
        var culture = System.Globalization.CultureInfo.InvariantCulture;
        var builder = new StringBuilder();

        if (target is SchemaReferenceTypeNode schemaRef)
        {
            builder.AppendLine(culture, $"var {interpreterVar} = {CreateNestedInterpreter(schemaRef.FullTypeName)};");
            return builder.ToString();
        }

        if (target is InlineSchemaTypeNode inlineSchema)
        {
            var className = GetOrRegisterInlineSchemaClassName(fieldName, inlineSchema, null);
            builder.AppendLine(culture, $"var {interpreterVar} = {CreateNestedInterpreter(className)};");
            builder.Append(GenerateOuterRefAssignments(interpreterVar, inlineSchema));
            return builder.ToString();
        }

        throw CreateUnsupportedCodeGenerationException(fieldName, target, "substream target");
    }
}
