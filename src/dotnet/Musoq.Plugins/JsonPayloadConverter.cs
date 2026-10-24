using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Musoq.Plugins;

/// <summary>Writes generated interpretation payloads through their declared field contract.</summary>
public sealed class JsonPayloadConverter<T> : JsonConverter<T> where T : IJsonPayload
{
    /// <inheritdoc />
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        throw new NotSupportedException("Interpretation payload converters support serialization only.");

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
        value.WriteJson(writer, options);
}
