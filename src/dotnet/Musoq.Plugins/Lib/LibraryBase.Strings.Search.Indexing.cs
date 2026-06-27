using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Determine whether the string contains the specified value
    /// </summary>
    /// <param name="content">The content</param>
    /// <param name="what">The what</param>
    /// <returns>True if contains; otherwise false</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public bool? Contains(string? content, string? what)
    {
        if (content == null || what == null)
            return null;

        return content.Contains(what, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Position of the first occurrence of the specified value
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="text">The text</param>
    /// <returns>Index of specific text</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public int? IndexOf(string? value, string? text)
    {
        if (value == null || text == null)
            return null;

        return value.IndexOf(text, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Position of the nth occurrence of the specified value
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="text">The text</param>
    /// <param name="index">The index</param>
    /// <returns>Index of specific text</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public int? NthIndexOf(string? value, string? text, int index)
    {
        if (value == null || text == null || index < 0)
            return null;

        var searchText = text;
        if (string.IsNullOrEmpty(searchText))
            return null;

        var count = 0;
        var position = -1;

        do
        {
            position = value.IndexOf(searchText, position + 1, StringComparison.OrdinalIgnoreCase);

            if (position == -1)
                return null;

            if (count == index)
                return position;

            count++;
        } while (true);
    }

    /// <summary>
    ///     Position of the last occurrence of the specified pattern
    /// </summary>
    /// <param name="value">The content to search in</param>
    /// <param name="text">The pattern to find</param>
    /// <returns>Index of the last occurrence of the pattern</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public int? LastIndexOf(string? value, string? text)
    {
        if (value == null || text == null || text.Length == 0)
            return null;

        var position = value.LastIndexOf(text, StringComparison.OrdinalIgnoreCase);
        return position == -1 ? null : position;
    }
}
