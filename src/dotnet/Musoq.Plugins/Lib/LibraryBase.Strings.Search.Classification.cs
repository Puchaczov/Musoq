using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Determines whether the string contains only numeric characters (0-9).
    /// </summary>
    /// <param name="value">The string to check</param>
    /// <returns>True if the string contains only digits; otherwise false. Returns null if input is null, false if empty.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public bool? IsNumeric(string? value)
    {
        if (value == null)
            return null;

        if (value.Length == 0)
            return false;

        foreach (var c in value)
            if (!char.IsDigit(c))
                return false;

        return true;
    }

    /// <summary>
    ///     Determines whether the string contains only alphabetic characters (a-z, A-Z).
    /// </summary>
    /// <param name="value">The string to check</param>
    /// <returns>True if the string contains only letters; otherwise false. Returns null if input is null, false if empty.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public bool? IsAlpha(string? value)
    {
        if (value == null)
            return null;

        if (value.Length == 0)
            return false;

        foreach (var c in value)
            if (!char.IsLetter(c))
                return false;

        return true;
    }

    /// <summary>
    ///     Determines whether the string contains only alphanumeric characters (a-z, A-Z, 0-9).
    /// </summary>
    /// <param name="value">The string to check</param>
    /// <returns>
    ///     True if the string contains only letters and digits; otherwise false. Returns null if input is null, false if
    ///     empty.
    /// </returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public bool? IsAlphaNumeric(string? value)
    {
        if (value == null)
            return null;

        if (value.Length == 0)
            return false;

        foreach (var c in value)
            if (!char.IsLetterOrDigit(c))
                return false;

        return true;
    }
}
