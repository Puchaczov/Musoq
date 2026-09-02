using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private static void AppendGeneratedLine(StringBuilder builder, string line)
    {
        builder.AppendLine(line);
    }

    private HashSet<string> _currentNullableFieldNames = new(StringComparer.OrdinalIgnoreCase);

    private void SetCurrentNullableFieldNames(IReadOnlyList<SchemaFieldNode> fields)
    {
        _currentNullableFieldNames = fields
            .Where(field => field.IsConditional ||
                           (field is ComputedFieldNode computed && ReferencesConditionalField(computed.Expression, fields)))
            .Select(field => field.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private string GenerateSizeFieldReference(string fieldName)
    {
        var localVar = GetLocalVarName(fieldName);
        return _currentNullableFieldNames.Contains(fieldName)
            ? $"System.Convert.ToInt32((object?){localVar})"
            : $"(int){localVar}";
    }

    private void AppendStringArrayElementReadCode(
        StringBuilder builder,
        string localVar,
        string loopVar,
        StringTypeNode stringType,
        string fieldName)
    {
        var elementVar = $"_{localVar}_elem";
        builder.Append(Indent(GenerateStringDeclarationCode(elementVar, stringType, fieldName), 1));
        builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture,
            $"    {localVar}[{loopVar}] = {elementVar};");
    }
}
