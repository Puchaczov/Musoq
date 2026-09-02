using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticCore004OperatorTests : BasicEntityTestBase
{
    [TestMethod]
    public void Operators_ShouldEvaluateWithDocumentedPrecedenceAndAssociativity()
    {
        TestMethodBatchTemplate(
            ("256 + 256 / 2", 384),
            ("(256 + 256) / 2", 256),
            ("10 - 3 - 2", 5),
            ("1 << 2 + 1", 8),
            ("1 & 2 | 3", 3),
            ("'a' + 'b'", "ab"),
            ("true or false and false", true),
            ("not false", true),
            ("not (true or false)", false),
            ("null ?? null ?? 'fallback'", "fallback"),
            ("5 between 1 and 10", true));
    }
}
