using System.Text.Json;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    /// Converts object to json.
    /// </summary>
    /// <param name="obj">Object to convert. </param>
    /// <typeparam name="T">Type of object. </typeparam>
    /// <returns>Json representation of object.</returns>
    [BindableMethod]
    public string? ToJson<T>(T? obj)
    {
        return obj == null ? null : JsonSerializer.Serialize(obj);
    }
}