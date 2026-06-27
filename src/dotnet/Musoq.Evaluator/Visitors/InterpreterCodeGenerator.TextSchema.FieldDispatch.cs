using System.Text;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private string GenerateTextFieldReadCode(TextFieldDefinitionNode field, TextFieldDefinitionNode? nextField)
    {
        var builder = new StringBuilder();
        var localVar = GetLocalVarName(field.Name);
        var isDiscard = field.IsDiscard;
        var isOptional = (field.Modifiers & TextFieldModifier.Optional) != 0;
        var hasLower = (field.Modifiers & TextFieldModifier.Lower) != 0;
        var hasUpper = (field.Modifiers & TextFieldModifier.Upper) != 0;

        if (isOptional && !isDiscard)
        {
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"string? {localVar} = null;");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var _savedPos_{localVar} = ParsePosition;");
            builder.AppendLine("try");
            builder.AppendLine("{");
            builder.Append("    ");
            builder.AppendLine(GenerateTextFieldReadCodeInner(field, $"_temp_{localVar}", false, nextField));
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    {localVar} = _temp_{localVar};");
            builder.AppendLine("}");
            builder.AppendLine("catch");
            builder.AppendLine("{");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    ParsePosition = _savedPos_{localVar};");
            builder.AppendLine("}");
        }
        else if (isOptional && isDiscard)
        {
            var discardId = Guid.NewGuid().ToString("N")[..8];
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var _savedPos_{discardId} = ParsePosition;");
            builder.AppendLine("try");
            builder.AppendLine("{");
            builder.Append("    ");
            builder.AppendLine(GenerateTextFieldReadCodeInner(field, "_", true, nextField));
            builder.AppendLine("}");
            builder.AppendLine("catch");
            builder.AppendLine("{");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    ParsePosition = _savedPos_{discardId};");
            builder.AppendLine("}");
        }
        else
        {
            builder.AppendLine(GenerateTextFieldReadCodeInner(field, localVar, isDiscard, nextField));
        }

        if (!isDiscard && (hasLower || hasUpper) && field.FieldType != TextFieldType.Repeat &&
            field.FieldType != TextFieldType.Switch)
        {
            if (hasLower)
                builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{localVar} = {localVar}?.ToLowerInvariant();");
            else if (hasUpper)
                builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{localVar} = {localVar}?.ToUpperInvariant();");
        }

        return builder.ToString();
    }

    private string GenerateTextFieldReadCodeInner(TextFieldDefinitionNode field, string localVar, bool isDiscard,
        TextFieldDefinitionNode? nextField)
    {
        return field.FieldType switch
        {
            TextFieldType.Literal => GenerateLiteralCode(field),
            TextFieldType.Until => GenerateUntilCode(field, localVar, isDiscard, nextField),
            TextFieldType.Between => GenerateBetweenCode(field, localVar, isDiscard),
            TextFieldType.Chars => GenerateCharsCode(field, localVar, isDiscard),
            TextFieldType.Token => GenerateTokenCode(field, localVar, isDiscard),
            TextFieldType.Rest => GenerateRestCode(field, localVar, isDiscard),
            TextFieldType.Whitespace => GenerateWhitespaceCode(field),
            TextFieldType.Pattern => GeneratePatternCode(field, localVar, isDiscard),
            TextFieldType.Repeat => GenerateRepeatCode(field, localVar, isDiscard),
            TextFieldType.Switch => GenerateSwitchCode(field, localVar, isDiscard),
            _ => throw new NotSupportedException($"Text field type '{field.FieldType}' is not yet supported.")
        };
    }
}
