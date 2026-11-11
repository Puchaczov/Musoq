using System.Collections.Generic;
using System.Text;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private string GenerateLiteralCode(TextFieldDefinitionNode field, string localVar, bool isDiscard)
    {
        var escapedLiteral = InterpretationStringLiterals.EscapeCSharpString(field.PrimaryValue ?? string.Empty);
        var fieldName = InterpretationStringLiterals.EscapeCSharpString(GetTextFieldDiagnosticName(field));
        var modifierArgs = GenerateModifierArgs(field.Modifiers);
        return TextStatusRead(
            localVar,
            $"base.ExpectLiteralStatus(ref {_textInputName}, \"{escapedLiteral}\", fieldName: \"{fieldName}\"{modifierArgs})",
            isDiscard
        );
    }

    private string GenerateUntilCode(TextFieldDefinitionNode field, string localVar, bool isDiscard)
    {
        var escapedDelimiter = InterpretationStringLiterals.EscapeCSharpString(field.PrimaryValue ?? string.Empty);
        var modifierArgs = GenerateModifierArgs(field.Modifiers);
        var greedinessArgs = GenerateGreedinessArgs(field.Modifiers);
        var fieldName = InterpretationStringLiterals.EscapeCSharpString(GetTextFieldDiagnosticName(field));

        return TextStatusRead(
            localVar,
            $"base.ReadUntilStatus(data, \"{escapedDelimiter}\"{modifierArgs}{greedinessArgs}, fieldName: \"{fieldName}\")",
            isDiscard
        );
    }

    private string GenerateBetweenCode(TextFieldDefinitionNode field, string localVar, bool isDiscard)
    {
        var escapedOpen = InterpretationStringLiterals.EscapeCSharpString(field.PrimaryValue ?? string.Empty);
        var escapedClose = InterpretationStringLiterals.EscapeCSharpString(field.SecondaryValue ?? string.Empty);
        var nested = (field.Modifiers & TextFieldModifier.Nested) != 0 ? ", nested: true" : "";
        var hasEscaped = (field.Modifiers & TextFieldModifier.Escaped) != 0;
        var escapedArg = hasEscaped ? ", escaped: true" : "";
        var escapeCharacterArg =
            hasEscaped && field.EscapeCharacter != null
                ? $", escapeCharacter: \"{InterpretationStringLiterals.EscapeCSharpString(field.EscapeCharacter)}\""
                : "";
        var fieldNameArg =
            $", fieldName: \"{InterpretationStringLiterals.EscapeCSharpString(GetTextFieldDiagnosticName(field))}\"";
        var modifierArgs = GenerateModifierArgs(field.Modifiers);

        return TextStatusRead(
            localVar,
            $"base.ReadBetweenStatus(data, \"{escapedOpen}\", \"{escapedClose}\"{nested}{escapedArg}{escapeCharacterArg}{modifierArgs}{fieldNameArg})",
            isDiscard
        );
    }

    private string GenerateCharsCode(TextFieldDefinitionNode field, string localVar, bool isDiscard)
    {
        var count = field.PrimaryValue ?? "0";
        var modArgs = GenerateModifierArgs(field.Modifiers);
        var fieldName = InterpretationStringLiterals.EscapeCSharpString(GetTextFieldDiagnosticName(field));

        return TextStatusRead(
            localVar,
            $"base.ReadCharsStatus(data, {count}{modArgs}, fieldName: \"{fieldName}\")",
            isDiscard
        );
    }

    private string GenerateTokenCode(TextFieldDefinitionNode field, string localVar, bool isDiscard)
    {
        var modifierArgs = GenerateModifierArgs(field.Modifiers);
        var fieldName = InterpretationStringLiterals.EscapeCSharpString(GetTextFieldDiagnosticName(field));

        return TextStatusRead(
            localVar,
            $"base.ReadTokenStatus(data{modifierArgs}, fieldName: \"{fieldName}\")",
            isDiscard
        );
    }

    private string GenerateRestCode(TextFieldDefinitionNode field, string localVar, bool isDiscard)
    {
        var modArgs = GenerateModifierArgs(field.Modifiers);
        var fieldName = InterpretationStringLiterals.EscapeCSharpString(GetTextFieldDiagnosticName(field));

        return TextStatusRead(localVar, $"base.ReadRestStatus(data{modArgs}, fieldName: \"{fieldName}\")", isDiscard);
    }

    private string GenerateWhitespaceCode(TextFieldDefinitionNode field, string localVar, bool isDiscard)
    {
        var quantifier = field.PrimaryValue ?? "+";
        var fieldName = InterpretationStringLiterals.EscapeCSharpString(GetTextFieldDiagnosticName(field));

        if (!isDiscard)
            return StatusRead(
                localVar,
                $"base.ReadWhitespaceStatus(data, \"{InterpretationStringLiterals.EscapeCSharpString(quantifier)}\"{GenerateModifierArgs(field.Modifiers)}, fieldName: \"{fieldName}\")"
            );

        return quantifier switch
        {
            "+" => StatusCheck($"base.SkipWhitespaceStatus(data, true, fieldName: \"{fieldName}\")"),
            "*" => StatusCheck($"base.SkipWhitespaceStatus(data, false, fieldName: \"{fieldName}\")"),
            "?" => StatusCheck($"base.SkipOptionalWhitespaceStatus(data, fieldName: \"{fieldName}\")"),
            _ => StatusCheck("base.SkipWhitespaceStatus(data, true)"),
        };
    }

    private string GeneratePatternCode(TextFieldDefinitionNode field, string localVar, bool isDiscard)
    {
        var pattern = _textPatterns[field];
        var fieldName = InterpretationStringLiterals.EscapeCSharpString(GetTextFieldDiagnosticName(field));

        if (isDiscard)
            return StatusCheck($"base.SkipPreparedPatternStatus(data, {pattern.Name}, \"{fieldName}\")");
        if (pattern.Pattern.CaptureGroups.Count == 0)
            return StatusRead(localVar, $"base.ReadPreparedPatternStatus(data, {pattern.Name}, \"{fieldName}\")");

        return GeneratePreparedCaptureRead(field.Name, localVar, fieldName, pattern);
    }

    private string GeneratePreparedCaptureRead(
        string field,
        string localVar,
        string fieldName,
        PreparedPatternMember pattern
    )
    {
        var builder = new StringBuilder();
        var matchVar = NextStatusName("__musoqMatch");
        builder.Append(
            StatusRead(
                matchVar,
                $"base.ReadPreparedPatternMatchStatus(ref {_textInputName}, {pattern.Name}, \"{fieldName}\")"
            )
        );
        builder.Append(
            System.Globalization.CultureInfo.InvariantCulture,
            $"var {localVar} = new CaptureResult_{field} {{ "
        );
        var groupInits = new List<string>(pattern.Pattern.CaptureGroups.Count);
        for (var slot = 0; slot < pattern.Pattern.CaptureGroups.Count; slot++)
            groupInits.Add(
                $"{EscapeCSharpIdentifier(pattern.Pattern.CaptureGroups[slot])} = GetPreparedCapture({pattern.Name}, {matchVar}, {slot})"
            );
        builder.Append(string.Join(", ", groupInits));
        builder.AppendLine(" };");
        return builder.ToString();
    }
}
