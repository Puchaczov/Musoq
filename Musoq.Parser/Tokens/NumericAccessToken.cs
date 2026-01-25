namespace Musoq.Parser.Tokens;

/// <summary>
///     Represents a numeric array access token (e.g., "array[5]").
/// </summary>
public class NumericAccessToken : Token
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="NumericAccessToken" /> class.
    /// </summary>
    /// <param name="name">The name of the array or object being accessed.</param>
    /// <param name="value">The numeric index value as a string.</param>
    /// <param name="span">The text span of the token.</param>
    public NumericAccessToken(string name, string value, TextSpan span)
        : base(value, TokenType.NumericAccess, span)
    {
        Index = int.Parse(value);
        Name = name;
    }

    /// <summary>
    ///     Gets the numeric index value.
    /// </summary>
    public int Index { get; }

    /// <summary>
    ///     Gets the name of the array or object being accessed.
    /// </summary>
    public string Name { get; }
}
