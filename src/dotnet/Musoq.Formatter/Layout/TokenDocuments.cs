using Musoq.Parser.Syntax;

namespace Musoq.Formatter.Layout;

internal sealed class TokenDocuments(FormattingOptions options)
{
    internal Document Of(SyntaxToken token)
    {
        if (token.Role != SyntaxTokenRole.Keyword)
            return Document.SourceText(token.Text);
        var text = string.Join(" ", token.Text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        text = options.KeywordCasing switch
        {
            KeywordCasing.Upper => text.ToUpperInvariant(),
            KeywordCasing.Lower => text.ToLowerInvariant(),
            _ => text
        };
        return Document.Text(text);
    }
}
