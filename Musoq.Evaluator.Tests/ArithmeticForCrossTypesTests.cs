using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
// ReSharper disable RedundantTypeArgumentsOfMethod

namespace Musoq.Evaluator.Tests;

[TestClass]
public class ArithmeticForCrossTypesTests : BasicEntityTestBase
{
    //ub - byte / 0 to 255
    //b - signed byte / -128 to 127
    
    [TestMethod]
    public void WhenSByteAndSByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<byte>("1ub + 1ub", 2);
    }

    [TestMethod]
    public void WhenByteAndSByteUsed_ShouldHaveColumnOfIntType()
    {
        TestMethodTemplate<int>("1ub + 1b", 2);
    }

    [TestMethod]
    public void WhenByteAndShortUsed_ShouldHaveColumnOfIntType()
    {
        TestMethodTemplate<int>("1ub + 1s", 2);
    }

    [TestMethod]
    public void WhenByteAndUShortUsed_ShouldHaveColumnOfIntType()
    {
        TestMethodTemplate<int>("1ub + 1us", 2);
    }

    [TestMethod]
    public void WhenByteAndIntUsed_ShouldHaveColumnOfIntType()
    {
        TestMethodTemplate<int>("1ub + 1i", 2);
    }

    [TestMethod]
    public void WhenByteAndUIntUsed_ShouldHaveColumnOfUIntType()
    {
        TestMethodTemplate<uint>("1ub + 1ui", 2);
    }

    [TestMethod]
    public void WhenByteAndLongUsed_ShouldHaveColumnOfLongType()
    {
        TestMethodTemplate<long>("1ub + 1l", 2);
    }

    [TestMethod]
    public void WhenByteAndULongUsed_ShouldHaveColumnOfULongType()
    {
        TestMethodTemplate<ulong>("1ub + 1ul", 2);
    }

    [TestMethod]
    public void WhenByteAndDecimalUsed_ShouldHaveColumnOfDecimalType()
    {
        TestMethodTemplate<decimal>("1ub + 1", 2);
    }
}