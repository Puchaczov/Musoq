namespace Musoq.Formatter.Tests.Verification;

internal sealed partial class AstProjector
{
    private void Project(global::Musoq.Parser.Tokens.NumericAccessToken value)
    {
        Member(nameof(value.Index));
        Write(value.Index);
        Member(nameof(value.IsOutOfRange));
        Write(value.IsOutOfRange);
        Member(nameof(value.Name));
        Write(value.Name);
        Member(nameof(value.TokenType));
        Write(value.TokenType);
        Member(nameof(value.Value));
        Write(value.Value);
    }

    private void Project(global::Musoq.Parser.Tokens.KeyAccessToken value)
    {
        Member(nameof(value.Key));
        Write(value.Key);
        Member(nameof(value.Name));
        Write(value.Name);
        Member(nameof(value.TokenType));
        Write(value.TokenType);
        Member(nameof(value.Value));
        Write(value.Value);
    }
}
