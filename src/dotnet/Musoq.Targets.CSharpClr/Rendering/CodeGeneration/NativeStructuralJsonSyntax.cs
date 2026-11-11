using System.Collections.Generic;
using System.Globalization;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Targets.CSharpClr;

internal static class NativeStructuralJsonSyntax
{
    internal static string ConverterName(int index, IReadOnlyList<int> path) =>
        StructuralCarrierSyntaxFactory.CreateCarrierName(index, path) + "JsonConverter";

    internal static MemberDeclarationSyntax Create(StructuralTypeDescriptor shape, int index, IReadOnlyList<int> path)
    {
        var name = StructuralCarrierSyntaxFactory.CreateCarrierName(index, path);
        var writes = new List<string>();
        var declarations = new List<string>();
        var reads = new List<string>();
        var arguments = new List<string>();
        var required = new List<string>();
        var presenceCount = System.Math.Max(1, (shape.Fields.Count + 63) / 64);
        for (var fieldIndex = 0; fieldIndex < shape.Fields.Count; fieldIndex++)
        {
            var field = shape.Fields[fieldIndex];
            var type = StructuralCarrierSyntaxFactory.CreateStorageType(field.Type, index, [.. path, fieldIndex]);
            var bit = (1UL << (fieldIndex % 64)).ToString(CultureInfo.InvariantCulture);
            declarations.Add($"{type} field{fieldIndex} = default;");
            reads.Add(
                $"if (reader.ValueTextEquals({SyntaxFactory.Literal(field.Name).Text})) {{ "
                    + $"if ((p{fieldIndex / 64} & {bit}UL) != 0) throw new global::System.Text.Json.JsonException(\"A native field occurs more than once.\"); reader.Read(); "
                    + $"field{fieldIndex} = global::System.Text.Json.JsonSerializer.Deserialize<{type}>(ref reader, options)!; p{fieldIndex / 64} |= {bit}UL; continue; }}"
            );
            if (field.Required)
                required.Add(
                    $"if ((p{fieldIndex / 64} & {bit}UL) == 0) throw new global::System.Text.Json.JsonException(\"A required native field is missing.\");"
                );
            arguments.Add($"field{fieldIndex}");
            writes.Add(
                $"if ((value.P{fieldIndex / 64} & {bit}UL) != 0) {{ writer.WritePropertyName({SyntaxFactory.Literal(field.Name).Text}); "
                    + $"global::System.Text.Json.JsonSerializer.Serialize<{type}>(writer, value.F{fieldIndex}, options); }}"
            );
        }
        for (var word = 0; word < presenceCount; word++)
        {
            declarations.Add($"ulong p{word} = 0;");
            arguments.Add($"p{word}");
        }
        return SyntaxFactory.ParseMemberDeclaration(
            $"private sealed class {ConverterName(index, path)} : global::System.Text.Json.Serialization.JsonConverter<{name}>\n{{\n    public {ConverterName(index, path)}() {{ }}\n    public override {name} Read(ref global::System.Text.Json.Utf8JsonReader reader,\n        global::System.Type typeToConvert, global::System.Text.Json.JsonSerializerOptions options)\n    {{\n        if (reader.TokenType != global::System.Text.Json.JsonTokenType.StartObject)\n            throw new global::System.Text.Json.JsonException(\"A native record requires a JSON object.\");\n        {string.Join("\n", declarations)}\n        while (reader.Read())\n        {{\n            if (reader.TokenType == global::System.Text.Json.JsonTokenType.EndObject)\n            {{\n                {string.Join("\n", required)}\n                return new {name}({string.Join(", ", arguments)});\n            }}\n            if (reader.TokenType != global::System.Text.Json.JsonTokenType.PropertyName)\n                throw new global::System.Text.Json.JsonException(\"A native record requires named fields.\");\n            {string.Join("\n", reads)}\n            throw new global::System.Text.Json.JsonException(\"The native record contains an undeclared field.\");\n        }}\n        throw new global::System.Text.Json.JsonException(\"The native record is incomplete.\");\n    }}\n    public override void Write(global::System.Text.Json.Utf8JsonWriter writer, {name} value,\n        global::System.Text.Json.JsonSerializerOptions options)\n    {{\n        writer.WriteStartObject();\n        {string.Join("\n", writes)}\n        writer.WriteEndObject();\n    }}\n}}"
        )!;
    }
}
