using System.Text.Json;
using Musoq.Plugins.Attributes;
using Musoq.Plugins.Helpers;

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
    
    /// <summary>
    /// Extracts values from json by path.
    /// </summary>
    /// <param name="json">Json to extract value from. </param>
    /// <param name="path">Path to value. </param>
    /// <returns>Value from json by path.</returns>
    [BindableMethod]
    public string[] ExtractFromJsonToArray(string? json, string? path)
    {
        if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(path))
            return [];

        return JsonExtractorHelper.ExtractFromJson(json, path);
    }
    
    /// <summary>
    /// Extracts values from json by path and joins them with comma.
    /// </summary>
    /// <param name="json">Json to extract value from. </param>
    /// <param name="path">Path to value. </param>
    /// <returns>Value from json by path.</returns>
    [BindableMethod]
    public string? ExtractFromJson(string? json, string? path)
    {
        if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(path))
            return null;
        
        return string.Join(",", JsonExtractorHelper.ExtractFromJson(json, path));
    }
}