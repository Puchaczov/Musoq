using System.Collections.Generic;
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
        var iterationVar = $"_iteration_{localVar}";
        var startPositionVar = $"_startPos_{localVar}";
        var escapedFieldName = EscapeCSharpString(field.Name);

        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {listVar} = new System.Collections.Generic.List<{schemaName}>();");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {interpreterVar} = new {schemaName}();");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {iterationVar} = 0;");

        if (untilDelimiter != null)
        {
            var escapedDelimiter = EscapeCSharpString(untilDelimiter);
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"while (!IsAtEnd(data) && !LookaheadMatches(data, \"{escapedDelimiter}\"))");
            builder.AppendLine("{");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    EnsureRepeatIteration(\"{escapedFieldName}\", {iterationVar}++);");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    var {startPositionVar} = ParsePosition;");
            AppendGeneratedLine(builder, $"    var {itemVar} = ParseNested({interpreterVar}, data, \"{escapedFieldName}\");");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    EnsureRepeatMadeProgress(\"{escapedFieldName}\", {startPositionVar});");
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
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    EnsureRepeatIteration(\"{escapedFieldName}\", {iterationVar}++);");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    var {startPositionVar} = ParsePosition;");
            AppendGeneratedLine(builder, $"    var {itemVar} = ParseNested({interpreterVar}, data, \"{escapedFieldName}\");");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    EnsureRepeatMadeProgress(\"{escapedFieldName}\", {startPositionVar});");
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

        TextSwitchCaseNode? defaultCase = null;
        foreach (var switchCase in field.SwitchCases)
        {
            if (switchCase.IsDefault)
            {
                if (defaultCase != null)
                    throw new InvalidOperationException($"Switch field '{field.Name}' must have only one default case");

                defaultCase = switchCase;
                continue;
            }

            if (defaultCase != null)
                throw new InvalidOperationException($"Switch field '{field.Name}' must place the default case last");
        }

        var allProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var switchCase in field.SwitchCases)
        {
            var schemaName = switchCase.TypeName;
            var textNode = RequireTextSwitchSchema(field, schemaName);
            foreach (var f in GetAllTextSchemaFields(textNode))
                if (!f.IsDiscard)
                    allProperties.Add(f.Name);
        }

        var expandoVar = $"_{localVar}_expando";
        var dictVar = $"_{localVar}_dict";
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {expandoVar} = new System.Dynamic.ExpandoObject();");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {dictVar} = (System.Collections.Generic.IDictionary<string, object?>){expandoVar};");
        foreach (var prop in allProperties) builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{dictVar}[\"{prop}\"] = null;");

        var isFirstCase = true;
        foreach (var switchCase in field.SwitchCases)
        {
            if (switchCase.IsDefault)
            {
                defaultCase = switchCase;
                continue;
            }

            var escapedPattern = EscapeCSharpRegexString(switchCase.Pattern!);
            var schemaName = switchCase.TypeName;
            var escapedFieldName = EscapeCSharpString(field.Name);

            if (isFirstCase)
            {
                builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"if (LookaheadMatchesPattern(data, @\"{escapedPattern}\", fieldName: \"{escapedFieldName}\"))");
                isFirstCase = false;
            }
            else
            {
                builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"else if (LookaheadMatchesPattern(data, @\"{escapedPattern}\", fieldName: \"{escapedFieldName}\"))");
            }

            builder.AppendLine("{");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    var _interp_{schemaName} = new {schemaName}();");
            AppendGeneratedLine(builder, $"    var _result_{schemaName} = ParseNested(_interp_{schemaName}, data, \"{escapedFieldName}\");");

            var matchedTextNode = RequireTextSwitchSchema(field, schemaName);
            foreach (var f in GetAllTextSchemaFields(matchedTextNode))
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
            var escapedFieldName = EscapeCSharpString(field.Name);
            AppendGeneratedLine(builder, $"    var _result_{schemaName} = ParseNested(_interp_{schemaName}, data, \"{escapedFieldName}\");");
            var matchedTextNode = RequireTextSwitchSchema(field, schemaName);
            foreach (var f in GetAllTextSchemaFields(matchedTextNode))
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
                System.Globalization.CultureInfo.InvariantCulture,
                $"    throw new Musoq.Schema.Interpreters.ParseException(Musoq.Schema.Interpreters.ParseErrorCode.NoAlternativeMatched, SchemaName, \"{EscapeCSharpString(field.Name)}\", ParsePosition, \"No switch case matched at parse position \" + ParsePosition);");
            builder.AppendLine("}");
        }

        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {localVar} = {expandoVar};");
        if (isDiscard) builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"_ = {localVar};");

        return builder.ToString();
    }

}
