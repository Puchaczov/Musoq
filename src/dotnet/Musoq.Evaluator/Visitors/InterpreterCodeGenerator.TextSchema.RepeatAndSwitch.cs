using System.Collections.Generic;
using System.Linq;
using System.Text;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private static string GenerateRepeatCode(TextFieldDefinitionNode field, string localVar, bool isDiscard)
    {
        var schemaName = field.PrimaryValue ??
                         throw new InvalidOperationException("Repeat field must specify a schema name");
        var untilDelimiter = field.SecondaryValue;

        var builder = new StringBuilder();
        var listVar = $"_list_{localVar}";
        var itemVar = $"_item_{localVar}";
        var interpreterVar = $"_interp_{schemaName}";

        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {listVar} = new System.Collections.Generic.List<{schemaName}>();");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {interpreterVar} = new {schemaName}();");

        if (untilDelimiter != null)
        {
            var escapedDelimiter = EscapeCSharpString(untilDelimiter);
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"while (!IsAtEnd(data) && !LookaheadMatches(data, \"{escapedDelimiter}\"))");
            builder.AppendLine("{");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    var {itemVar} = {interpreterVar}.ParseAt(data, ParsePosition);");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    ParsePosition = {interpreterVar}.Position;");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    {listVar}.Add({itemVar});");
            builder.AppendLine("}");

            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"if (LookaheadMatches(data, \"{escapedDelimiter}\"))");
            builder.AppendLine("{");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    ParsePosition += {untilDelimiter.Length};");
            builder.AppendLine("}");
        }
        else
        {
            builder.AppendLine("while (!IsAtEnd(data))");
            builder.AppendLine("{");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    var {itemVar} = {interpreterVar}.ParseAt(data, ParsePosition);");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    ParsePosition = {interpreterVar}.Position;");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    {listVar}.Add({itemVar});");
            builder.AppendLine("}");
        }

        if (isDiscard)
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"_ = {listVar}.ToArray();");
        else
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {localVar} = {listVar}.ToArray();");

        return builder.ToString();
    }

    private string GenerateSwitchCode(TextFieldDefinitionNode field, string localVar, bool isDiscard)
    {
        var builder = new StringBuilder();

        if (field.SwitchCases.Length == 0)
            throw new InvalidOperationException("Switch field must have at least one case");

        var allProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var switchCase in field.SwitchCases)
        {
            var schemaName = switchCase.TypeName;
            var schema = _registry.Schemas.FirstOrDefault(s =>
                string.Equals(s.Name, schemaName, StringComparison.OrdinalIgnoreCase));
            if (schema?.Node is TextSchemaNode textNode)
                foreach (var f in textNode.Fields)
                    if (!f.IsDiscard)
                        allProperties.Add(f.Name);
        }

        var expandoVar = $"_{localVar}_expando";
        var dictVar = $"_{localVar}_dict";
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {expandoVar} = new System.Dynamic.ExpandoObject();");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {dictVar} = (System.Collections.Generic.IDictionary<string, object?>){expandoVar};");
        foreach (var prop in allProperties) builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{dictVar}[\"{prop}\"] = null;");

        var isFirstCase = true;
        TextSwitchCaseNode? defaultCase = null;

        foreach (var switchCase in field.SwitchCases)
        {
            if (switchCase.IsDefault)
            {
                defaultCase = switchCase;
                continue;
            }

            var escapedPattern = EscapeCSharpRegexString(switchCase.Pattern!);
            var schemaName = switchCase.TypeName;

            if (isFirstCase)
            {
                builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"if (LookaheadMatchesPattern(data, @\"{escapedPattern}\"))");
                isFirstCase = false;
            }
            else
            {
                builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"else if (LookaheadMatchesPattern(data, @\"{escapedPattern}\"))");
            }

            builder.AppendLine("{");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    var _interp_{schemaName} = new {schemaName}();");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    var _result_{schemaName} = _interp_{schemaName}.ParseAt(data, ParsePosition);");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    ParsePosition = _interp_{schemaName}.Position;");

            var matchedSchema = _registry.Schemas.FirstOrDefault(s =>
                string.Equals(s.Name, schemaName, StringComparison.OrdinalIgnoreCase));
            if (matchedSchema?.Node is TextSchemaNode matchedTextNode)
                foreach (var f in matchedTextNode.Fields)
                    if (!f.IsDiscard)
                        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture,
                            $"    {dictVar}[\"{f.Name}\"] = _result_{schemaName}.{EscapeCSharpIdentifier(f.Name)};");

            builder.AppendLine("}");
        }

        if (defaultCase != null)
        {
            var schemaName = defaultCase.TypeName;
            if (!isFirstCase) builder.AppendLine("else");
            builder.AppendLine("{");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    var _interp_{schemaName} = new {schemaName}();");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    var _result_{schemaName} = _interp_{schemaName}.ParseAt(data, ParsePosition);");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    ParsePosition = _interp_{schemaName}.Position;");
            var matchedSchema = _registry.Schemas.FirstOrDefault(s =>
                string.Equals(s.Name, schemaName, StringComparison.OrdinalIgnoreCase));
            if (matchedSchema?.Node is TextSchemaNode matchedTextNode)
                foreach (var f in matchedTextNode.Fields)
                    if (!f.IsDiscard)
                        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture,
                            $"    {dictVar}[\"{f.Name}\"] = _result_{schemaName}.{EscapeCSharpIdentifier(f.Name)};");

            builder.AppendLine("}");
        }
        else if (!isFirstCase)
        {
            builder.AppendLine("else");
            builder.AppendLine("{");
            builder.AppendLine(
                "    throw new System.InvalidOperationException(\"No switch case matched at position \" + ParsePosition);");
            builder.AppendLine("}");
        }

        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {localVar} = {expandoVar};");
        if (isDiscard) builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"_ = {localVar};");

        return builder.ToString();
    }
}
