using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Determines whether the string starts with the specified prefix
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="prefix">The prefix</param>
    /// <returns>True if starts with; otherwise false</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public bool? StartsWith(string? value, string? prefix)
    {
        if (value == null || prefix == null)
            return null;

        return value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Determines whether the string starts with the specified prefix
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="prefix">The prefix</param>
    /// <param name="comparison">The comparison</param>
    /// <returns>True if starts with; otherwise false</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public bool? StartsWith(string? value, string? prefix, string comparison)
    {
        if (value == null || prefix == null)
            return null;

        return value.StartsWith(prefix, Enum.Parse<StringComparison>(comparison));
    }

    /// <summary>
    ///     Determines whether the string ends with the specified suffix
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="suffix">The suffix</param>
    /// <returns>True if ends with; otherwise false</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public bool? EndsWith(string? value, string? suffix)
    {
        if (value == null || suffix == null)
            return null;

        return value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Determines whether the string ends with the specified suffix
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="suffix">The suffix</param>
    /// <param name="comparison">The comparison</param>
    /// <returns>True if ends with; otherwise false</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public bool? EndsWith(string? value, string? suffix, string comparison)
    {
        if (value == null || suffix == null)
            return null;

        return value.EndsWith(suffix, Enum.Parse<StringComparison>(comparison));
    }
}
