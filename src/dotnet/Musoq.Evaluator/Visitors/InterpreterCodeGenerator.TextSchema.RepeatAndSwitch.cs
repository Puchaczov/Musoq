using System.Collections.Generic;
using System.Text;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private void AppendRepeatedElementRead(StringBuilder builder, TextFieldDefinitionNode field,
        string itemVar, string interpreterVar)
    {
        if (field.RepeatElement is { } element)
        {
            builder.Append(Indent(GenerateTextFieldReadCode(element, itemVar, false), 1));
            return;
        }

        AppendGeneratedLine(builder, $"    var {itemVar} = ParseNested({interpreterVar}, data, \"{EscapeCSharpString(field.Name)}\");");
    }

    private string GenerateRepeatCode(TextFieldDefinitionNode field, string localVar, bool isDiscard)
    {
        var schemaName = field.RepeatElement is { } element
            ? GetTextFieldPropertyClrType(element).TrimEnd('?')
            : field.PrimaryValue ?? throw new InvalidOperationException("Repeat field must specify a schema name");
        var read = new StringBuilder();
        AppendRepeatedElementRead(read, field, $"_item_{localVar}", $"_interp_{schemaName}");
        var assignment = isDiscard ? "_" : $"var {localVar}";
        return TextRepeatCodeEmitter.Emit(new TextRepeatEmission(field, localVar, schemaName, read.ToString(), assignment),
            EscapeCSharpString);
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

        var result = $"_{localVar}_switch";
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {result} = new TextSwitch_{field.Name}();");

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
                        $"    {result}.{EscapeCSharpIdentifier(f.Name)} = _result_{schemaName}.{EscapeCSharpIdentifier(f.Name)};");

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
                        $"    {result}.{EscapeCSharpIdentifier(f.Name)} = _result_{schemaName}.{EscapeCSharpIdentifier(f.Name)};");

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

        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {localVar} = {result};");
        if (isDiscard) builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"_ = {localVar};");

        return builder.ToString();
    }

}
