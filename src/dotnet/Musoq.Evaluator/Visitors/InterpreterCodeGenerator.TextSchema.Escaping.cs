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
        var args = new List<string>();
        if ((modifiers & TextFieldModifier.Trim) != 0)
            args.Add("trim: true");
        else
        {
            if ((modifiers & TextFieldModifier.LTrim) != 0)
                args.Add("ltrim: true");
            if ((modifiers & TextFieldModifier.RTrim) != 0)
                args.Add("rtrim: true");
        }

        if ((modifiers & TextFieldModifier.Lower) != 0)
            args.Add("lower: true");
        if ((modifiers & TextFieldModifier.Upper) != 0)
            args.Add("upper: true");

        return args.Count > 0 ? ", " + string.Join(", ", args) : "";
    }

    private static string GeneratePostCaptureModifierArgs(TextFieldModifier modifiers)
    {
        var args = new List<string>();
        if ((modifiers & (TextFieldModifier.Trim | TextFieldModifier.LTrim)) != 0)
            args.Add("ltrim: true");
        if ((modifiers & (TextFieldModifier.Trim | TextFieldModifier.RTrim)) != 0)
            args.Add("rtrim: true");
        if ((modifiers & TextFieldModifier.Lower) != 0)
            args.Add("lower: true");
        if ((modifiers & TextFieldModifier.Upper) != 0)
            args.Add("upper: true");

        return args.Count > 0 ? ", " + string.Join(", ", args) : "";
    }

    private static string GenerateGreedinessArgs(TextFieldModifier modifiers)
    {
        var args = new List<string>();
        if ((modifiers & TextFieldModifier.Greedy) != 0)
            args.Add("greedy: true");
        if ((modifiers & TextFieldModifier.Lazy) != 0)
            args.Add("lazy: true");

        return args.Count > 0 ? ", " + string.Join(", ", args) : "";
    }
}
