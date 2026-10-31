using System.Collections.Generic;

namespace Musoq.Formatter.Tests.Verification;

internal static class AstCoverage
{
    internal static IReadOnlySet<string> Types(string source) => AstProjector.Coverage(AstParsing.Parse(source));
}
