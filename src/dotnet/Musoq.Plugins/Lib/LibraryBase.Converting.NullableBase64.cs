using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>Converts a byte-array slice to Base64, preserving nullable offset and length values.</summary>
    /// <param name="value">The bytes to encode.</param>
    /// <param name="offset">The first byte, or null.</param>
    /// <param name="length">The byte count, or null.</param>
    /// <returns>The encoded slice, or null when an argument is null.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Conversion)]
    public string? ToBase64(byte[]? value, int? offset, int? length)
    {
        if (!offset.HasValue || !length.HasValue)
            return null;

        return ToBase64(value, offset.GetValueOrDefault(), length.GetValueOrDefault());
    }
}
