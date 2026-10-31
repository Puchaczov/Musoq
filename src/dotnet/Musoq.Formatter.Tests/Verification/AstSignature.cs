namespace Musoq.Formatter.Tests.Verification;

internal static class AstSignature
{
    internal static AstSnapshot Parse(string source) => AstProjector.Snapshot(AstParsing.Parse(source));
}
