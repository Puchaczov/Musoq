using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class UserMistakesTests
{
    [TestMethod]
    public void FullOuterJoin_MissingOnCondition()
    {
        var analyzer = CreateAnalyzer();
        var query = "SELECT a.Name FROM #A.Entities() a FULL OUTER JOIN #B.Entities() b";

        var result = analyzer.ValidateSyntax(query);

        AssertHasOneOfErrorCodes(result, "FULL OUTER JOIN missing ON",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2007_InvalidJoinCondition);
    }
}
