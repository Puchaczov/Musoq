using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Musoq.Evaluator.Visitors;

internal static class PartialPayloadCodeEmitter
{
    internal static string Members(string typeName) =>
        $"    private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;\n    public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;\n    protected override void AttachPartialFieldNames({typeName} value,\n        System.Collections.Generic.IReadOnlySet<string> completedFields)\n    {{\n        value.__completedFieldNames = completedFields;\n    }}\n";

    internal static string JsonMembers(IEnumerable<(string Name, string PropertyName)> fields)
    {
        var writes = fields.Select(field =>
            $"if (IsFieldComplete({JsonSerializer.Serialize(field.Name)})) {{ writer.WritePropertyName({JsonSerializer.Serialize(field.Name)}); "
            + $"var fieldValue = this.{field.PropertyName}; global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options); }}"
        );
        return $"public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer,\n    global::System.Text.Json.JsonSerializerOptions? options = null)\n{{\n    writer.WriteStartObject();\n    {string.Join("\n", writes)}\n    writer.WriteEndObject();\n}}\n";
    }
}
