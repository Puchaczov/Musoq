using System.Collections.Generic;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private static string EscapeCSharpRegexString(string value)
    {
        return value.Replace("\"", "\"\"", StringComparison.Ordinal);
    }

    private static string EscapeCSharpString(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal);
    }

    private static string GenerateModifierArgs(TextFieldModifier modifiers)
    {
        var hasTrim = (modifiers & TextFieldModifier.Trim) != 0;
        var hasLTrim = (modifiers & TextFieldModifier.LTrim) != 0;
        var hasRTrim = (modifiers & TextFieldModifier.RTrim) != 0;

        if (hasTrim)
            return ", trim: true";

        var args = new List<string>();
        if (hasLTrim)
            args.Add("ltrim: true");
        if (hasRTrim)
            args.Add("rtrim: true");

        return args.Count > 0 ? ", " + string.Join(", ", args) : "";
    }
}
