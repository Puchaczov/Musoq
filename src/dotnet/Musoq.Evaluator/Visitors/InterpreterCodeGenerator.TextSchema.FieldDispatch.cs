using System.Text;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private string? _textDiagnosticName;

    private string GetTextFieldDiagnosticName(TextFieldDefinitionNode field) => _textDiagnosticName ?? field.Name;

    private string GenerateTextBranchReadCode(
        TextFieldDefinitionNode owner,
        TextFieldDefinitionNode body,
        string localName
    )
    {
        var previous = _textDiagnosticName;
        _textDiagnosticName ??= owner.Name;
        try
        {
            return GenerateTextFieldReadCode(body, localName, false);
        }
        finally
        {
            _textDiagnosticName = previous;
        }
    }

    private string GenerateTextFieldReadCode(TextFieldDefinitionNode field)
    {
        var localVar = GetLocalVarName(field.Name);
        var isDiscard = field.IsDiscard;
        return GenerateTextFieldReadCode(field, localVar, isDiscard);
    }

    private string GenerateTextFieldReadCode(TextFieldDefinitionNode field, string localVar, bool isDiscard)
    {
        var builder = new StringBuilder();
        var isOptional = (field.Modifiers & TextFieldModifier.Optional) != 0;

        if (isOptional)
            return GenerateOptionalTextRead(field, localVar, isDiscard);
        builder.AppendLine(GenerateTextFieldReadCodeInner(field, localVar, isDiscard));

        return builder.ToString();
    }

    private string GenerateOptionalTextRead(TextFieldDefinitionNode field, string localVar, bool isDiscard)
    {
        var failed = NextStatusName("__musoqOptionalFailed");
        var completed = NextStatusName("__musoqOptionalDone");
        var saved = NextStatusName("__musoqOptionalPosition");
        var temporary = NextStatusName("__musoqOptionalValue");
        var previous = _statusFailureLabel;
        string read;
        _statusFailureLabel = failed;
        try
        {
            read = GenerateTextFieldReadCodeInner(field, temporary, isDiscard);
        }
        finally
        {
            _statusFailureLabel = previous;
        }

        var builder = new StringBuilder();
        if (!isDiscard)
            builder.AppendLine($"{GetTextFieldPropertyClrType(field)} {localVar} = null;");
        builder.AppendLine("{");
        builder.AppendLine($"var {saved} = ParsePosition;");
        builder.AppendLine("try {");
        builder.Append(read);
        if (!isDiscard)
            builder.AppendLine($"{localVar} = {temporary};");
        builder.AppendLine("} catch (global::Musoq.Schema.Interpreters.ParseException) {");
        builder.AppendLine($"goto {failed};");
        builder.AppendLine("}");
        builder.AppendLine($"goto {completed};");
        builder.AppendLine($"{failed}: ParsePosition = {saved};");
        builder.AppendLine($"{completed}: ;");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private string GenerateTextFieldReadCodeInner(TextFieldDefinitionNode field, string localVar, bool isDiscard)
    {
        return field.FieldType switch
        {
            TextFieldType.Literal => GenerateLiteralCode(field, localVar, isDiscard),
            TextFieldType.Until => GenerateUntilCode(field, localVar, isDiscard),
            TextFieldType.Between => GenerateBetweenCode(field, localVar, isDiscard),
            TextFieldType.Chars => GenerateCharsCode(field, localVar, isDiscard),
            TextFieldType.Token => GenerateTokenCode(field, localVar, isDiscard),
            TextFieldType.Rest => GenerateRestCode(field, localVar, isDiscard),
            TextFieldType.SchemaReference => GenerateSchemaReferenceCode(field, localVar, isDiscard),
            TextFieldType.Whitespace => GenerateWhitespaceCode(field, localVar, isDiscard),
            TextFieldType.Pattern => GeneratePatternCode(field, localVar, isDiscard),
            TextFieldType.Repeat => GenerateRepeatCode(field, localVar, isDiscard),
            TextFieldType.Switch => GenerateSwitchCode(field, localVar, isDiscard),
            _ => throw new NotSupportedException($"Text field type '{field.FieldType}' is not yet supported."),
        };
    }
}
