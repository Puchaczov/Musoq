using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Musoq.Evaluator.Visitors;

internal static class PartialPayloadCodeEmitter
{
    internal static string Members(string typeName) => $$"""
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames({{typeName}} value,
            System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

    """;
    internal static string JsonMembers(IEnumerable<(string Name, string PropertyName)> fields)
    {
        var writes = fields.Select(field =>
            $"if (IsFieldComplete({JsonSerializer.Serialize(field.Name)})) {{ writer.WritePropertyName({JsonSerializer.Serialize(field.Name)}); " +
            $"var fieldValue = this.{field.PropertyName}; global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options); }}");
        return $$"""
            public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer,
                global::System.Text.Json.JsonSerializerOptions? options = null)
            {
                writer.WriteStartObject();
                {{string.Join("\n", writes)}}
                writer.WriteEndObject();
            }

            """;
    }

}
