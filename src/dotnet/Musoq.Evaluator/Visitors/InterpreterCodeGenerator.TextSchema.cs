using System.Collections.Generic;
using System.Text;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private string GenerateTextInterpreterClass(TextSchemaNode schema)
    {
        var plan = BuildTextPlan(schema);

        var builder = new StringBuilder();
        var className = plan.SchemaName;

        builder.AppendLine("/// <summary>");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"/// Generated interpreter for text schema '{className}'.");
        builder.AppendLine("/// </summary>");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"public sealed class {className} : TextInterpreterBase<{className}>");
        builder.AppendLine("{");

        foreach (var field in plan.Fields)
        {
            if (!field.EmitsProperty) continue;

            var summary = field.IsCaptureResult ? "capture result" : "field value";
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    /// <summary>Gets the {field.Name} {summary}.</summary>");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    public {field.PropertyClrType} {field.PropertyName} {{ get; init; }}");
            builder.AppendLine();
        }

        builder.AppendLine("    /// <inheritdoc />");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    public override string SchemaName => \"{className}\";");
        builder.AppendLine();

        builder.AppendLine("    /// <inheritdoc />");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    public override {className} ParseAt(ReadOnlySpan<char> data, int offset)");
        builder.AppendLine("    {");
        builder.AppendLine("        ParsePosition = offset;");
        builder.AppendLine("        SetCurrentField(null);");
        builder.AppendLine();

        _discardCounter = 0;

        var fieldInitializers = new List<string>();
        foreach (var field in plan.Fields)
        {
            AppendGeneratedLine(builder, $"        SetCurrentField(\"{EscapeCSharpString(field.Name)}\");");

            var readCode = GenerateTextFieldReadCode(field.Source);
            builder.Append(Indent(readCode, 2));

            if (field.EmitsProperty)
            {
                AppendGeneratedLine(builder, $"        RecordParsedField(\"{EscapeCSharpString(field.Name)}\", {field.LocalVariableName});");
                fieldInitializers.Add($"{field.PropertyName} = {field.LocalVariableName}");
            }
        }

        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"        return new {className}");
        builder.AppendLine("        {");
        for (var i = 0; i < fieldInitializers.Count; i++)
        {
            var comma = i < fieldInitializers.Count - 1 ? "," : "";
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"            {fieldInitializers[i]}{comma}");
        }

        builder.AppendLine("        };");
        builder.AppendLine("    }");

        foreach (var field in plan.Fields)
        {
            if (!field.IsCaptureResult) continue;

            builder.AppendLine();
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    public sealed class CaptureResult_{field.Name}");
            builder.AppendLine("    {");
            foreach (var group in field.CaptureGroups)
                builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"        public string? {EscapeCSharpIdentifier(group)} {{ get; init; }}");
            builder.AppendLine("    }");
        }

        builder.AppendLine("}");

        return builder.ToString();
    }

}
