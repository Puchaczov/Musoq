using System.Collections.Generic;
using System.Text;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private static string GenerateLiteralCode(TextFieldDefinitionNode field, string localVar, bool isDiscard)
    {
        var escapedLiteral = EscapeCSharpString(field.PrimaryValue ?? string.Empty);
        var fieldName = EscapeCSharpString(field.Name);
        var modifierArgs = GenerateModifierArgs(field.Modifiers);
        var read = $"ExpectLiteral(data, \"{escapedLiteral}\", fieldName: \"{fieldName}\"{modifierArgs})";
        return isDiscard ? $"{read};" : $"var {localVar} = {read};";
    }

    private static string GenerateUntilCode(TextFieldDefinitionNode field, string localVar, bool isDiscard)
    {
        var escapedDelimiter = EscapeCSharpString(field.PrimaryValue ?? string.Empty);
        var modifierArgs = GenerateModifierArgs(field.Modifiers);
        var greedinessArgs = GenerateGreedinessArgs(field.Modifiers);
        var fieldName = EscapeCSharpString(field.Name);

        if (isDiscard) return $"_ = ReadUntil(data, \"{escapedDelimiter}\"{modifierArgs}{greedinessArgs}, fieldName: \"{fieldName}\");";
        return $"var {localVar} = ReadUntil(data, \"{escapedDelimiter}\"{modifierArgs}{greedinessArgs}, fieldName: \"{fieldName}\");";
    }

    private static string GenerateBetweenCode(TextFieldDefinitionNode field, string localVar, bool isDiscard)
    {
        var escapedOpen = EscapeCSharpString(field.PrimaryValue ?? string.Empty);
        var escapedClose = EscapeCSharpString(field.SecondaryValue ?? string.Empty);
        var nested = (field.Modifiers & TextFieldModifier.Nested) != 0 ? ", nested: true" : "";
        var hasEscaped = (field.Modifiers & TextFieldModifier.Escaped) != 0;
        var escapedArg = hasEscaped ? ", escaped: true" : "";
        var escapeCharacterArg = hasEscaped && field.EscapeCharacter != null
            ? $", escapeCharacter: \"{EscapeCSharpString(field.EscapeCharacter)}\""
            : "";
        var fieldNameArg = $", fieldName: \"{EscapeCSharpString(field.Name)}\"";
        var modifierArgs = GenerateModifierArgs(field.Modifiers);

        if (isDiscard)
            return $"_ = ReadBetween(data, \"{escapedOpen}\", \"{escapedClose}\"{nested}{escapedArg}{escapeCharacterArg}{modifierArgs}{fieldNameArg});";
        return
            $"var {localVar} = ReadBetween(data, \"{escapedOpen}\", \"{escapedClose}\"{nested}{escapedArg}{escapeCharacterArg}{modifierArgs}{fieldNameArg});";
    }

    private static string GenerateCharsCode(TextFieldDefinitionNode field, string localVar, bool isDiscard)
    {
        var count = field.PrimaryValue ?? "0";
        var modArgs = GenerateModifierArgs(field.Modifiers);
        var fieldName = EscapeCSharpString(field.Name);

        if (isDiscard) return $"_ = ReadChars(data, {count}{modArgs}, fieldName: \"{fieldName}\");";
        return $"var {localVar} = ReadChars(data, {count}{modArgs}, fieldName: \"{fieldName}\");";
    }

    private static string GenerateTokenCode(TextFieldDefinitionNode field, string localVar, bool isDiscard)
    {
        var modifierArgs = GenerateModifierArgs(field.Modifiers);
        var fieldName = EscapeCSharpString(field.Name);

        if (isDiscard) return $"_ = ReadToken(data{modifierArgs}, fieldName: \"{fieldName}\");";
        return $"var {localVar} = ReadToken(data{modifierArgs}, fieldName: \"{fieldName}\");";
    }

    private static string GenerateRestCode(TextFieldDefinitionNode field, string localVar, bool isDiscard)
    {
        var modArgs = GenerateModifierArgs(field.Modifiers);
        var fieldName = EscapeCSharpString(field.Name);

        if (isDiscard) return $"_ = ReadRest(data{modArgs}, fieldName: \"{fieldName}\");";
        return $"var {localVar} = ReadRest(data{modArgs}, fieldName: \"{fieldName}\");";
    }

    private static string GenerateWhitespaceCode(TextFieldDefinitionNode field, string localVar, bool isDiscard)
    {
        var quantifier = field.PrimaryValue ?? "+";
        var fieldName = EscapeCSharpString(field.Name);

        if (!isDiscard)
            return $"var {localVar} = ReadWhitespace(data, \"{EscapeCSharpString(quantifier)}\"{GenerateModifierArgs(field.Modifiers)}, fieldName: \"{fieldName}\");";

        return quantifier switch
        {
            "+" => $"SkipWhitespace(data, true, fieldName: \"{fieldName}\");",
            "*" => $"SkipWhitespace(data, false, fieldName: \"{fieldName}\");",
            "?" => $"SkipOptionalWhitespace(data, fieldName: \"{fieldName}\");",
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
            var fieldName = EscapeCSharpString(field.Name);
            var greedinessArgs = GenerateGreedinessArgs(field.Modifiers);
            var postCaptureArgs = GeneratePostCaptureModifierArgs(field.Modifiers);
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {matchVar} = ReadPatternMatch(data, @\"{escapedPattern}\", fieldName: \"{fieldName}\"{greedinessArgs});");
            builder.Append(System.Globalization.CultureInfo.InvariantCulture, $"var {localVar} = {matchVar}.Success ? new {captureClassName} {{ ");
            var groupInits = new List<string>();
            foreach (var group in field.CaptureGroups)
                groupInits.Add($"{EscapeCSharpIdentifier(group)} = ApplyModifiers({matchVar}.Groups[\"{EscapeCSharpString(group)}\"].Value{postCaptureArgs})");
            builder.Append(string.Join(", ", groupInits));
            builder.AppendLine(" } : null;");
            return builder.ToString();
        }

        var patternFieldName = EscapeCSharpString(field.Name);
        var patternModifierArgs = GenerateModifierArgs(field.Modifiers);
        var patternGreedinessArgs = GenerateGreedinessArgs(field.Modifiers);
        if (isDiscard) return $"_ = ReadPattern(data, @\"{escapedPattern}\", fieldName: \"{patternFieldName}\"{patternGreedinessArgs}{patternModifierArgs});";
        return $"var {localVar} = ReadPattern(data, @\"{escapedPattern}\", fieldName: \"{patternFieldName}\"{patternGreedinessArgs}{patternModifierArgs});";
    }
}
