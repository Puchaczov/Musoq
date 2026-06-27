using System.Text;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Converts a string to snake_case format.
    /// </summary>
    /// <param name="value">The string to convert</param>
    /// <returns>The string in snake_case format</returns>
    /// <example>
    ///     ToSnakeCase("HelloWorld") returns "hello_world"
    ///     ToSnakeCase("XMLParser") returns "xml_parser"
    /// </example>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? ToSnakeCase(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var result = new StringBuilder();

        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];

            if (char.IsUpper(c))
            {
                if (i > 0 && !char.IsUpper(value[i - 1]))
                    result.Append('_');
                else if (i > 0 && i < value.Length - 1 && char.IsUpper(value[i - 1]) && !char.IsUpper(value[i + 1]))
                    result.Append('_');

                result.Append(char.ToLowerInvariant(c));
            }
            else if (c is ' ' or '-')
            {
                result.Append('_');
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }

    /// <summary>
    ///     Converts a string to kebab-case format.
    /// </summary>
    /// <param name="value">The string to convert</param>
    /// <returns>The string in kebab-case format</returns>
    /// <example>
    ///     ToKebabCase("HelloWorld") returns "hello-world"
    ///     ToKebabCase("XMLParser") returns "xml-parser"
    /// </example>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? ToKebabCase(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var result = new StringBuilder();

        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];

            if (char.IsUpper(c))
            {
                if (i > 0 && !char.IsUpper(value[i - 1]))
                    result.Append('-');
                else if (i > 0 && i < value.Length - 1 && char.IsUpper(value[i - 1]) && !char.IsUpper(value[i + 1]))
                    result.Append('-');

                result.Append(char.ToLowerInvariant(c));
            }
            else if (c is ' ' or '_')
            {
                result.Append('-');
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }

    /// <summary>
    ///     Converts a string to camelCase format.
    /// </summary>
    /// <param name="value">The string to convert</param>
    /// <returns>The string in camelCase format</returns>
    /// <example>
    ///     ToCamelCase("hello_world") returns "helloWorld"
    ///     ToCamelCase("Hello World") returns "helloWorld"
    /// </example>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? ToCamelCase(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var result = new StringBuilder();
        var capitalizeNext = false;
        var isFirst = true;

        foreach (var c in value)
            if (c is '_' or '-' or ' ')
            {
                capitalizeNext = true;
            }
            else if (capitalizeNext)
            {
                result.Append(char.ToUpperInvariant(c));
                capitalizeNext = false;
            }
            else if (isFirst)
            {
                result.Append(char.ToLowerInvariant(c));
                isFirst = false;
            }
            else
            {
                result.Append(c);
            }

        return result.ToString();
    }

    /// <summary>
    ///     Converts a string to PascalCase format.
    /// </summary>
    /// <param name="value">The string to convert</param>
    /// <returns>The string in PascalCase format</returns>
    /// <example>
    ///     ToPascalCase("hello_world") returns "HelloWorld"
    ///     ToPascalCase("hello world") returns "HelloWorld"
    /// </example>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? ToPascalCase(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var result = new StringBuilder();
        var capitalizeNext = true;

        foreach (var c in value)
            if (c is '_' or '-' or ' ')
            {
                capitalizeNext = true;
            }
            else if (capitalizeNext)
            {
                result.Append(char.ToUpperInvariant(c));
                capitalizeNext = false;
            }
            else
            {
                result.Append(c);
            }

        return result.ToString();
    }

    /// <summary>
    ///     Reverses a string.
    /// </summary>
    /// <param name="value">The string to reverse</param>
    /// <returns>The reversed string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? ReverseString(string? value)
    {
        if (value == null)
            return null;

        var charArray = value.ToCharArray();
        Array.Reverse(charArray);
        return new string(charArray);
    }
}
