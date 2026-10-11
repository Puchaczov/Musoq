using System.Collections.Generic;
using System.Text;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

internal static class TextSwitchClassEmitter
{
    internal static string Generate(TextFieldDefinitionNode field,
        Func<TextFieldDefinitionNode, string, TextSchemaNode> requireSchema,
        Func<TextSchemaNode, List<TextFieldDefinitionNode>> fields,
        Func<TextFieldDefinitionNode, string> propertyType, Func<string, string> identifier)
    {
        var properties = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var branch in field.SwitchCases)
        {
            var schema = requireSchema(field, branch.TypeName);
            foreach (var member in fields(schema))
            {
                if (member.IsDiscard)
                    continue;
                var type = propertyType(member);
                if (member is { FieldType: TextFieldType.Pattern, CaptureGroups.Length: > 0 })
                    type = $"{schema.Name}.{type}";
                if (properties.TryGetValue(member.Name, out var previous) && previous != type)
                    throw new SourcePlanContractException($"Switch field '{field.Name}.{member.Name}' has incompatible concrete branch types '{previous}' and '{type}'.", TextSpan.Empty);
                properties[member.Name] = type;
            }
        }
        var code = new StringBuilder();
        code.AppendLine($"public sealed class TextSwitch_{field.Name}");
        code.AppendLine("{");
        foreach (var property in properties)
            code.AppendLine($"    public {property.Value} {identifier(property.Key)} {{ get; set; }}");
        code.AppendLine("}");
        return code.ToString();
    }
}
