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

        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2007_InvalidJoinCondition, "FULL OUTER JOIN missing ON");
    }
}
