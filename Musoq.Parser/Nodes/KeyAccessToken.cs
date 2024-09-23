using Musoq.Parser.Tokens;

namespace Musoq.Parser.Nodes;

public class KeyAccessToken : Token
{
    public KeyAccessToken(string name, string key, TextSpan span)
        : base(key, TokenType.KeyAccess, span)
    {
        Key = key;
        Name = name;
    }

    public string Key { get; }

    public string Name { get; }
}