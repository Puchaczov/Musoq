using System.Text;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private string GenerateTextFieldReadCode(TextFieldDefinitionNode field)
    {
        var builder = new StringBuilder();
        var localVar = GetLocalVarName(field.Name);
        var isDiscard = field.IsDiscard;
        var isOptional = (field.Modifiers & TextFieldModifier.Optional) != 0;

        if (isOptional && !isDiscard)
        {
            var propertyClrType = GetTextFieldPropertyClrType(field);
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{propertyClrType} {localVar} = null;");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var _savedPos_{localVar} = ParsePosition;");
            builder.AppendLine("try");
            builder.AppendLine("{");
            builder.Append("    ");
            builder.AppendLine(GenerateTextFieldReadCodeInner(field, $"_temp_{localVar}", false));
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    {localVar} = _temp_{localVar};");
            builder.AppendLine("}");
            builder.AppendLine("catch (Musoq.Schema.Interpreters.ParseException)");
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
            builder.AppendLine(GenerateTextFieldReadCodeInner(field, "_", true));
            builder.AppendLine("}");
            builder.AppendLine("catch (Musoq.Schema.Interpreters.ParseException)");
            builder.AppendLine("{");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    ParsePosition = _savedPos_{discardId};");
            builder.AppendLine("}");
        }
        else
        {
            builder.AppendLine(GenerateTextFieldReadCodeInner(field, localVar, isDiscard));
        }

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
            _ => throw new NotSupportedException($"Text field type '{field.FieldType}' is not yet supported.")
        };
    }
}
