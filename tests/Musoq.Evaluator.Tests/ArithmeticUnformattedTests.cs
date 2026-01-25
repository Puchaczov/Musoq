using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class ArithmeticUnformattedTests : BasicEntityTestBase
{
    [TestMethod]
    public void UnformattedMinus_ShouldBeProperlyRecognized()
    {
        TestMethodTemplate("1-1", 0);
    }
}
