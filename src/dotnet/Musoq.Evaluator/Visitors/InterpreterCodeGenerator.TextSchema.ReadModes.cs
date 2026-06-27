using System.Collections.Generic;
using System.Text;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private static string GenerateLiteralCode(TextFieldDefinitionNode field)
    {
        var escapedLiteral = EscapeCSharpString(field.PrimaryValue ?? string.Empty);
        return $"ExpectLiteral(data, \"{escapedLiteral}\");";
    }

    private static string GenerateUntilCode(TextFieldDefinitionNode field, string localVar, bool isDiscard,
        TextFieldDefinitionNode? nextField)
    {
        var escapedDelimiter = EscapeCSharpString(field.PrimaryValue ?? string.Empty);
        var hasTrim = (field.Modifiers & TextFieldModifier.Trim) != 0;
        var trimArg = hasTrim ? ", trim: true" : "";

        var shouldNotConsume = nextField is { FieldType: TextFieldType.Between }
                               && string.Equals(field.PrimaryValue, nextField.PrimaryValue, StringComparison.Ordinal);
        var consumeArg = shouldNotConsume ? ", consumeDelimiter: false" : "";

        if (isDiscard) return $"_ = ReadUntil(data, \"{escapedDelimiter}\"{trimArg}{consumeArg});";
        return $"var {localVar} = ReadUntil(data, \"{escapedDelimiter}\"{trimArg}{consumeArg});";
    }

    private static string GenerateBetweenCode(TextFieldDefinitionNode field, string localVar, bool isDiscard)
    {
        var escapedOpen = EscapeCSharpString(field.PrimaryValue ?? string.Empty);
        var escapedClose = EscapeCSharpString(field.SecondaryValue ?? string.Empty);
        var nested = (field.Modifiers & TextFieldModifier.Nested) != 0 ? ", nested: true" : "";
        var hasTrim = (field.Modifiers & TextFieldModifier.Trim) != 0;
        var trimArg = hasTrim ? ", trim: true" : "";
        var hasEscaped = (field.Modifiers & TextFieldModifier.Escaped) != 0;
        var escapedArg = hasEscaped ? ", escaped: true" : "";

        if (isDiscard)
            return $"_ = ReadBetween(data, \"{escapedOpen}\", \"{escapedClose}\"{nested}{trimArg}{escapedArg});";
        return
            $"var {localVar} = ReadBetween(data, \"{escapedOpen}\", \"{escapedClose}\"{nested}{trimArg}{escapedArg});";
    }

    private static string GenerateCharsCode(TextFieldDefinitionNode field, string localVar, bool isDiscard)
    {
        var count = field.PrimaryValue ?? "0";
        var modArgs = GenerateModifierArgs(field.Modifiers);

        if (isDiscard) return $"_ = ReadChars(data, {count}{modArgs});";
        return $"var {localVar} = ReadChars(data, {count}{modArgs});";
    }

    private static string GenerateTokenCode(TextFieldDefinitionNode field, string localVar, bool isDiscard)
    {
        var hasTrim = (field.Modifiers & TextFieldModifier.Trim) != 0;
        var trimArg = hasTrim ? ", trim: true" : "";

        if (isDiscard) return $"_ = ReadToken(data{trimArg});";
        return $"var {localVar} = ReadToken(data{trimArg});";
    }

    private static string GenerateRestCode(TextFieldDefinitionNode field, string localVar, bool isDiscard)
    {
        var modArgs = GenerateModifierArgs(field.Modifiers);

        if (isDiscard) return $"_ = ReadRest(data{modArgs});";
        return $"var {localVar} = ReadRest(data{modArgs});";
    }

    private static string GenerateWhitespaceCode(TextFieldDefinitionNode field)
    {
        var quantifier = field.PrimaryValue ?? "+";

        return quantifier switch
        {
            "+" => "SkipWhitespace(data, true);",
            "*" => "SkipWhitespace(data, false);",
            "?" => "SkipOptionalWhitespace(data);",
            _ => "SkipWhitespace(data, true);"
        };
    }

    private static string GeneratePatternCode(TextFieldDefinitionNode field, string localVar, bool isDiscard)
    {
        var escapedPattern = EscapeCSharpRegexString(field.PrimaryValue ?? string.Empty);

        if (field.CaptureGroups.Length > 0 && !isDiscard)
        {
            var builder = new StringBuilder();
            var matchVar = $"_match_{localVar}";
            var captureClassName = $"CaptureResult_{field.Name}";
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {matchVar} = ReadPatternMatch(data, @\"{escapedPattern}\");");
            builder.Append(System.Globalization.CultureInfo.InvariantCulture, $"var {localVar} = {matchVar}.Success ? new {captureClassName} {{ ");
            var groupInits = new List<string>();
            foreach (var group in field.CaptureGroups)
                groupInits.Add($"{group} = {matchVar}.Groups[\"{group}\"].Value");
            builder.Append(string.Join(", ", groupInits));
            builder.AppendLine(" } : null;");
            return builder.ToString();
        }

        if (isDiscard) return $"_ = ReadPattern(data, @\"{escapedPattern}\");";
        return $"var {localVar} = ReadPattern(data, @\"{escapedPattern}\");";
    }
}
