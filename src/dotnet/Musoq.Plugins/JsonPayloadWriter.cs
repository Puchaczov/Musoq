using System.Buffers;
using System.Text;
using System.Text.Json;

namespace Musoq.Plugins;

/// <summary>Serializes concrete payload values without erased field access.</summary>
public static class JsonPayloadWriter
{
    /// <summary>Writes a concrete scalar or declared payload.</summary>
    public static void Write<T>(Utf8JsonWriter writer, in T value, JsonSerializerOptions? options = null)
    {
        if (!typeof(T).IsValueType && value is IJsonPayload payload)
            payload.WriteJson(writer, options);
        else
            JsonSerializer.Serialize(writer, value, options);
    }

    /// <summary>Writes a concrete collection, retaining each element type.</summary>
    public static void Write<T>(Utf8JsonWriter writer, in T[]? values, JsonSerializerOptions? options = null)
    {
        if (values == null)
        {
            writer.WriteNullValue();
            return;
        }
        writer.WriteStartArray();
        foreach (var value in values)
            Write(writer, in value, options);
        writer.WriteEndArray();
    }

    /// <summary>Writes binary data using the standard JSON base64 representation.</summary>
    public static void Write(Utf8JsonWriter writer, in byte[]? values, JsonSerializerOptions? options = null)
    {
        if (values == null)
            writer.WriteNullValue();
        else
            writer.WriteBase64StringValue(values);
    }

    /// <summary>Serializes a concrete value as UTF-8 JSON text.</summary>
    public static string Serialize<T>(in T value)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
            Write(writer, in value);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
