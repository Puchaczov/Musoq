using System.Collections.Generic;
using System.Linq;
using System.Text;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private string _textInputName = string.Empty;

    private string GenerateTextInterpreterClass(TextSchemaNode schema)
    {
        var plan = BuildTextPlan(schema);

        var builder = new StringBuilder();
        var className = plan.SchemaName;
        var allFields = EnumerateBoundTextFields(plan.Fields).ToArray();

        builder.AppendLine("/// <summary>");
        builder.AppendLine(
            System.Globalization.CultureInfo.InvariantCulture,
            $"/// Generated interpreter for text schema '{className}'."
        );
        builder.AppendLine("/// </summary>");
        var properties = plan
            .Fields.Where(static field => field.EmitsProperty)
            .Select(static field => new PayloadProperty(field.Name, field.PropertyClrType!))
            .ToArray();
        var declaration = new InterpreterDeclaration(className, "TextInterpreterBase", "ITextInterpreterStatus");
        OpenInterpreterClass(builder, declaration, properties);
        AppendPreparedPatternMembers(builder, allFields, className);

        BeginStatusMethod(declaration.TypeName);
        _statusNames.UnionWith(allFields.Select(static field => field.LocalVariableName));
        _textInputName = "__musoqTextInput";
        while (!_statusNames.Add(_textInputName))
            _textInputName += "_";
        builder.Append(InterpreterStatusCodeEmitter.BeginText(declaration.TypeName, _textInputName));
        builder.AppendLine("        ParsePosition = offset;");
        builder.AppendLine("        SetCurrentField(null);");
        builder.AppendLine(
            System.Globalization.CultureInfo.InvariantCulture,
            $"        __parsedValue = new {declaration.TypeName}();"
        );
        builder.AppendLine("        RecordParsedValue(__parsedValue);");
        builder.AppendLine();

        _discardCounter = 0;

        var completionIndex = 0;
        foreach (var field in plan.Fields)
        {
            AppendGeneratedLine(
                builder,
                $"        SetCurrentField(\"{InterpretationStringLiterals.EscapeCSharpString(field.Name)}\");"
            );

            var readCode = plan.UsesDirectDelimitedReads
                ? GenerateDirectDelimitedRead(field)
                : GenerateTextFieldReadCode(field.Source);
            builder.Append(Indent(readCode, 2));

            if (field.EmitsProperty)
            {
                AppendGeneratedLine(
                    builder,
                    $"        __parsedValue.{field.PropertyName} = {field.LocalVariableName};"
                );
                AppendGeneratedLine(builder, $"        base.RecordParsedFieldSlot({completionIndex++});");
            }
        }

        builder.Append(InterpreterStatusCodeEmitter.End(declaration.TypeName));

        foreach (var field in allFields)
        {
            if (!field.IsCaptureResult)
                continue;

            builder.AppendLine();
            builder.AppendLine(
                System.Globalization.CultureInfo.InvariantCulture,
                $"    public sealed class {_textResultTypes.Name(field.Source)}"
            );
            builder.AppendLine("    {");
            foreach (var group in field.CaptureGroups)
                builder.AppendLine(
                    System.Globalization.CultureInfo.InvariantCulture,
                    $"        public string? {EscapeCSharpIdentifier(group)} {{ get; set; }}"
                );
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
