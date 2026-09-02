using System.Collections.Generic;
using System.Linq;
using System.Text;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private string GenerateSchemaReferenceCode(TextFieldDefinitionNode field, string localVar, bool isDiscard)
    {
        var schemaName = field.PrimaryValue ??
                         throw new InvalidOperationException("Text schema reference field must specify a schema name");
        _ = RequireTextSchema(field, schemaName);

        var variableName = localVar == "_"
            ? $"discard{_discardCounter++}"
            : localVar.TrimStart('_');
        var interpreterVar = $"_interp_{variableName}";
        var resultVar = $"_result_{variableName}";

        var builder = new StringBuilder();
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {interpreterVar} = new {schemaName}();");
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture,
            $"var {resultVar} = ParseNested({interpreterVar}, data, \"{EscapeCSharpString(field.Name)}\");");

        if (isDiscard)
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"_ = {resultVar};");
        else
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var {localVar} = {resultVar};");

        return builder.ToString();
    }

    private TextSchemaNode RequireTextSwitchSchema(TextFieldDefinitionNode field, string schemaName)
    {
        return RequireTextSchema(field, schemaName, "switch");
    }

    private TextSchemaNode RequireTextSchema(
        TextFieldDefinitionNode field,
        string schemaName,
        string context = "schema reference")
    {
        var registration = _registry.Schemas.FirstOrDefault(s =>
            string.Equals(s.Name, schemaName, StringComparison.OrdinalIgnoreCase));
        if (registration?.Node is TextSchemaNode textNode)
            return textNode;

        throw new InvalidOperationException(
            $"Text {context} field '{field.Name}' references '{schemaName}', which is not a registered text schema.");
    }
}
