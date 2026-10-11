using System.Text.Json;

namespace Musoq.Plugins;

/// <summary>A concrete reference payload that writes its declared fields directly.</summary>
public interface IJsonPayload
{
    /// <summary>Writes only the payload fields that are present.</summary>
    void WriteJson(Utf8JsonWriter writer, JsonSerializerOptions? options = null);
}
