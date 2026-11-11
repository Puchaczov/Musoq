using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public class ArithmeticUnformattedTests : BasicEntityTestBase
{
    [TestMethod]
    public void UnformattedMinus_ShouldBeProperlyRecognized()
    {
        TestMethodTemplate("1-1", 0);
    }
}
