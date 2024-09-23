using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class LogicalFunctionsTests : BasicEntityTestBase
{
    [TestMethod]
    public void IfFunctionTrueTest()
    {
        TestMethodTemplate("If(3 > 2, 5, 2)", 5);
    }

    [TestMethod]
    public void IfFunctionFalseTest()
    {
        TestMethodTemplate("If(2 > 3, 5, 2)", 2);
    }

    [TestMethod]
    public void IfFunctionEqualityFalseTest()
    {
        TestMethodTemplate("If(2 = 3, 5, 2)", 2);
    }

    [TestMethod]
    public void IfFunctionEqualityTrueTest()
    {
        TestMethodTemplate("If(2 <> 3, 5, 2)", 5);
    }

    [TestMethod]
    public void IfFunctionLessTrueTest()
    {
        TestMethodTemplate("If(2 < 3, 5, 2)", 5);
    }

    [TestMethod]
    public void IfFunctionEqualityStringFalseTest()
    {
        TestMethodTemplate("If(Name = 'dasda', 5, 2)", 2);
    }

    [TestMethod]
    public void IfFunctionEqualityStringTrueTest()
    {
        TestMethodTemplate("If(Name = 'ABCAACBA', 5, 2)", 5);
    }

    [TestMethod]
    public void IfFunctionGreaterEqualTrueTest()
    {
        TestMethodTemplate("If(3 >= 3, 5, 2)", 5);
    }

    [TestMethod]
    public void IfFunctionGreaterEqualFalseTest()
    {
        TestMethodTemplate("If(2 >= 3, 5, 2)", 2);
    }

    [TestMethod]
    public void IfFunctionLessEqualTrueTest()
    {
        TestMethodTemplate("If(3 <= 3, 5, 2)", 5);
    }

    [TestMethod]
    public void IfFunctionLessEqualFalseTest()
    {
        TestMethodTemplate("If(3 <= 2, 5, 2)", 2);
    }

    [TestMethod]
    public void IfFunctionNestedTest()
    {
        TestMethodTemplate("If(3 <= 2, 5, If(4 > 5, 7, 8))", 8);
    }
}