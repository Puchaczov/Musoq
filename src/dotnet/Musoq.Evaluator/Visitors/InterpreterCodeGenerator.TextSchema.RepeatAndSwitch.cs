using System.Linq;
using System.Text;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private void AppendRepeatedElementRead(
        StringBuilder builder,
        TextFieldDefinitionNode field,
        string itemVar,
        string interpreterVar
    )
    {
        if (field.RepeatElement is { } element)
        {
            builder.Append(Indent(GenerateTextFieldReadCode(element, itemVar, false), 1));
            return;
        }

        builder.Append(
            StatusRead(
                itemVar,
                $"base.ParseNestedStatus({interpreterVar}, ref {_textInputName}, \"{InterpretationStringLiterals.EscapeCSharpString(GetTextFieldDiagnosticName(field))}\")"
            )
        );
    }

    private string GenerateRepeatCode(TextFieldDefinitionNode field, string localVar, bool isDiscard)
    {
        var schemaName = field.RepeatElement is { } element
            ? GetTextFieldPropertyClrType(element).TrimEnd('?')
            : field.PrimaryValue ?? throw new InvalidOperationException("Repeat field must specify a schema name");
        var read = new StringBuilder();
        AppendRepeatedElementRead(read, field, $"_item_{localVar}", $"_interp_{schemaName}");
        var assignment = isDiscard ? "_" : $"var {localVar}";
        var name = InterpretationStringLiterals.EscapeCSharpString(GetTextFieldDiagnosticName(field));
        return TextRepeatCodeEmitter.Emit(
            new TextRepeatEmission(field, localVar, schemaName, read.ToString(), assignment)
            {
                MaxRepeatIterations = _maxRepeatIterations,
                DiagnosticName = GetTextFieldDiagnosticName(field),
                IterationCheck = StatusCheck(
                    $"base.CheckRepeatIterationStatus(\"{name}\", _iteration_{localVar}++, {_maxRepeatIterations})"
                ),
                ProgressCheck = StatusCheck($"base.CheckRepeatProgressStatus(\"{name}\", _startPos_{localVar})"),
            },
            InterpretationStringLiterals.EscapeCSharpString
        );
    }

    private string GenerateSwitchCode(TextFieldDefinitionNode field, string localVar, bool isDiscard)
    {
        if (field.SwitchCases.Length == 0)
            throw new InvalidOperationException("Switch field must have at least one case");
        if (field.SwitchCases.Take(field.SwitchCases.Length - 1).Any(arm => arm.IsDefault))
            throw new InvalidOperationException(
                $"Switch field '{field.Name}' must place its single default case last."
            );
        var builder = new StringBuilder();
        builder.AppendLine($"{GetTextSwitchResultClrType(field)} {localVar} = default;");
        var first = true;
        foreach (var arm in field.SwitchCases)
        {
            if (!arm.IsDefault)
            {
                var pattern = _textLookaheads[arm.Pattern!];
                var name = InterpretationStringLiterals.EscapeCSharpString(GetTextFieldDiagnosticName(field));
                builder.AppendLine(
                    $"{(first ? "if" : "else if")} (LookaheadMatchesPatternPrepared(data, {pattern}, fieldName: \"{name}\"))"
                );
            }
            else if (!first)
                builder.AppendLine("else");
            builder.AppendLine("{");
            var item = $"{localVar}_selected";
            builder.Append(Indent(GenerateTextBranchReadCode(field, arm.Body, item), 1));
            builder.AppendLine($"    {localVar} = {item};");
            builder.AppendLine("}");
            first = false;
        }
        if (!field.SwitchCases[^1].IsDefault)
        {
            builder.AppendLine("else");
            builder.AppendLine("{");
            builder.AppendLine(
                StatusFailure(
                    $"global::Musoq.Schema.Interpreters.ParseFailure.ForText(Musoq.Schema.Interpreters.ParseErrorCode.NoAlternativeMatched, SchemaName, \"{InterpretationStringLiterals.EscapeCSharpString(GetTextFieldDiagnosticName(field))}\", ParsePosition, \"No switch case matched at parse position \" + ParsePosition)"
                )
            );
            builder.AppendLine("}");
        }
        if (isDiscard)
            builder.AppendLine($"_ = {localVar};");
        return builder.ToString();
    }
}
