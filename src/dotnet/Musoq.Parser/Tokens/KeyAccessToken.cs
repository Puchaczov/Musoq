namespace Musoq.Parser.Tokens;

/// <summary>
///     Represents a key-based access token (e.g., "object['key']" or "array[expression]").
/// </summary>
public class KeyAccessToken : Token
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="KeyAccessToken" /> class.
    /// </summary>
    /// <param name="name">The name of the object being accessed.</param>
    /// <param name="key">The key or expression used for access.</param>
    /// <param name="span">The text span of the token.</param>
    public KeyAccessToken(string name, string key, TextSpan span)
        : base(key, TokenType.KeyAccess, span)
    {
        Key = key;
        Name = name;
    }

    /// <summary>
    ///     Gets the key or expression used for access.
    /// </summary>
    public string Key { get; }

    /// <summary>
    ///     Gets the name of the object being accessed.
    /// </summary>
    public string Name { get; }
}
