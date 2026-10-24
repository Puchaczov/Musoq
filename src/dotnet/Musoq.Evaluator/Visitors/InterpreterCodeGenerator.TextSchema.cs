using System.Collections.Generic;
using System.Linq;
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
        builder.AppendLine("[global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"public sealed class {className} : TextInterpreterBase<{className}>, global::Musoq.Plugins.IJsonPayload");
        builder.AppendLine("{");
        builder.Append(PartialPayloadCodeEmitter.Members(className));
        builder.Append(PartialPayloadCodeEmitter.JsonMembers(plan.Fields.Where(static field => field.EmitsProperty)
            .Select(static field => (field.Name, field.PropertyName ?? throw new InvalidOperationException("An emitted payload field requires a property name.")))));

        foreach (var field in plan.Fields)
        {
            if (!field.EmitsProperty) continue;

            var summary = field.IsCaptureResult ? "capture result" : "field value";
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    /// <summary>Gets the {field.Name} {summary}.</summary>");
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    public {field.PropertyClrType} {field.PropertyName} {{ get; set; }}");
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
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"        var __parsedValue = new {className}();");
        builder.AppendLine("        RecordParsedValue(__parsedValue);");
        builder.AppendLine();

        _discardCounter = 0;

        foreach (var field in plan.Fields)
        {
            AppendGeneratedLine(builder, $"        SetCurrentField(\"{InterpretationStringLiterals.EscapeCSharpString(field.Name)}\");");

            var readCode = GenerateTextFieldReadCode(field.Source);
            builder.Append(Indent(readCode, 2));

            if (field.EmitsProperty)
            {
                AppendGeneratedLine(builder, $"        __parsedValue.{field.PropertyName} = {field.LocalVariableName};");
                AppendGeneratedLine(builder, $"        RecordParsedField(\"{InterpretationStringLiterals.EscapeCSharpString(field.Name)}\");");
            }
        }

        builder.AppendLine("        return __parsedValue;");
        builder.AppendLine("    }");

        foreach (var field in EnumerateBoundTextFields(plan.Fields))
        {
            if (!field.IsCaptureResult) continue;

            builder.AppendLine();
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    public sealed class CaptureResult_{field.Name}");
            builder.AppendLine("    {");
            foreach (var group in field.CaptureGroups)
                builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"        public string? {EscapeCSharpIdentifier(group)} {{ get; set; }}");
            builder.AppendLine("    }");
        }

        builder.AppendLine("}");

        return builder.ToString();
    }

    private static IEnumerable<BoundTextField> EnumerateBoundTextFields(IEnumerable<BoundTextField> fields)
    {
        foreach (var field in fields)
        {
            yield return field;
            foreach (var arm in field.SwitchArms)
                foreach (var nested in EnumerateBoundTextFields([arm.Body]))
                    yield return nested;
            if (field.RepeatElement is { } element)
                foreach (var nested in EnumerateBoundTextFields([element]))
                    yield return nested;
        }
    }

}
