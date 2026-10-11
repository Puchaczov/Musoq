using System.Text;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

internal sealed record TextRepeatEmission(
    TextFieldDefinitionNode Field,
    string LocalName,
    string SchemaName,
    string ReadCode,
    string Assignment);

internal static class TextRepeatCodeEmitter
{
    internal static string Emit(TextRepeatEmission emission, Func<string, string> escape)
    {
        var (field, localVar, schemaName, _, assignment) = emission;
        var builder = new StringBuilder();
        var listVar = $"_list_{localVar}";
        var interpreterVar = $"_interp_{schemaName}";
        var iterationVar = $"_iteration_{localVar}";

        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {listVar} = new System.Collections.Generic.List<{schemaName}>();");
        if (field.RepeatElement == null)
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {interpreterVar} = new {schemaName}();");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {iterationVar} = 0;");
        AppendLoop(builder, emission, escape);
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{assignment} = {listVar}.ToArray();");

        return builder.ToString();
    }

    private static void AppendLoop(StringBuilder builder, TextRepeatEmission emission, Func<string, string> escape)
    {
        var untilDelimiter = emission.Field.SecondaryValue;
        var condition = "!IsAtEnd(data)";
        if (untilDelimiter != null)
            condition += $" && !LookaheadMatches(data, \"{escape(untilDelimiter)}\")";

        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"while ({condition})");
        builder.AppendLine("{");
        AppendIteration(builder, emission, escape(emission.Field.Name));
        builder.AppendLine("}");
        if (untilDelimiter == null)
            return;

        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"if (LookaheadMatches(data, \"{escape(untilDelimiter)}\"))");
        builder.AppendLine("{");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    ParsePosition += {untilDelimiter.Length};");
        builder.AppendLine("}");
    }

    private static void AppendIteration(StringBuilder builder, TextRepeatEmission emission, string escapedFieldName)
    {
        var localName = emission.LocalName;
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    EnsureRepeatIteration(\"{escapedFieldName}\", _iteration_{localName}++);");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    var _startPos_{localName} = ParsePosition;");
        builder.Append(emission.ReadCode);
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    EnsureRepeatMadeProgress(\"{escapedFieldName}\", _startPos_{localName});");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    _list_{localName}.Add(_item_{localName});");
    }
}
