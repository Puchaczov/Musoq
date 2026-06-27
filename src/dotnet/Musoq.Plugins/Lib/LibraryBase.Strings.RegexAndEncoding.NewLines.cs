using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Split string by Linux-style newlines (\n)
    /// </summary>
    /// <param name="input">The input</param>
    /// <returns>Array of strings</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string[]? SplitByLinuxNewLines(string? input)
    {
        if (input is null)
            return null;

        if (string.IsNullOrEmpty(input))
            return [];


        return input.Split(Separator, StringSplitOptions.None);
    }

    /// <summary>
    ///     Split string by Windows-style newlines (\r\n)
    /// </summary>
    /// <param name="input">The input</param>
    /// <returns>Array of strings</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string[]? SplitByWindowsNewLines(string? input)
    {
        if (input is null)
            return null;

        if (string.IsNullOrEmpty(input))
            return [];

        return input.Split(["\r\n"], StringSplitOptions.None);
    }

    /// <summary>
    ///     Smart split that handles both Windows (\r\n) and Linux (\n) newlines
    /// </summary>
    /// <param name="input">The input</param>
    /// <returns>Array of strings</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string[]? SplitByNewLines(string? input)
    {
        if (input is null)
            return null;

        if (string.IsNullOrEmpty(input))
            return [];

        var normalizedInput = input.Replace("\r\n", "\n", StringComparison.Ordinal);

        return normalizedInput.Split(Separator, StringSplitOptions.None);
    }

    /// <summary>
    ///     Splits the input string into an array of lines, handling both Windows (\r\n) and Linux (\n) newlines
    /// </summary>
    /// <param name="input">The input</param>
    /// <returns>Array of lines</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string[]? Lines(string? input)
    {
        return SplitByNewLines(input);
    }
}
