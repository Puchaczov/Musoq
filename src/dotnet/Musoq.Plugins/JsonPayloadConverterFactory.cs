using System.Text.Json;
using System.Text.Json.Serialization;

namespace Musoq.Plugins;

/// <summary>Creates typed payload converters, including for closed generic interpretation schemas.</summary>
public sealed class JsonPayloadConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) => typeof(IJsonPayload).IsAssignableFrom(typeToConvert);

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);
        if (!CanConvert(typeToConvert))
            throw new ArgumentException("The type must implement IJsonPayload.", nameof(typeToConvert));
        return (JsonConverter)Activator.CreateInstance(typeof(JsonPayloadConverter<>).MakeGenericType(typeToConvert))!;
    }
}
