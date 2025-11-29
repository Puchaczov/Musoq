using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
// ReSharper disable RedundantTypeArgumentsOfMethod

namespace Musoq.Evaluator.Tests;

[TestClass]
public class ArithmeticForMethodCallAndTypeSpecificOperationTests : BasicEntityTestBase
{
    [TestMethod]
    public void WhenByteMethodCallAndByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("DoNothing(1ub) + 1ub", 2);
    }

    [TestMethod]
    public void WhenByteMethodCallAndSByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("DoNothing(1ub) + 1b", 2);
    }

    [TestMethod]
    public void WhenByteMethodCallAndShortUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("DoNothing(1ub) + 1s", 2);
    }

    [TestMethod]
    public void WhenByteMethodCallAndUShortUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("DoNothing(1ub) + 1us", 2);
    }

    [TestMethod]
    public void WhenByteMethodCallAndIntUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("DoNothing(1ub) + 1i", 2);
    }

    [TestMethod]
    public void WhenByteMethodCallAndUIntUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<uint>("DoNothing(1ub) + 1ui", 2);
    }

    [TestMethod]
    public void WhenByteMethodCallAndLongUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<long>("DoNothing(1ub) + 1l", 2);
    }

    [TestMethod]
    public void WhenByteMethodCallAndULongUsed_ShouldHaveColumnOfThatType()
    {
        //var p = (int)1 + 1UL;
        TestMethodTemplate<ulong>("DoNothing(1ub) + 1ul", 2);
    }

    [TestMethod]
    public void WhenByteMethodCallAndDecimalUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("DoNothing(1ub) + 1d", 2);
    }
    
    [TestMethod]
    public void WhenSByteMethodCallAndByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("DoNothing(1b) + 1b", 2);
    }

    [TestMethod]
    public void WhenSByteMethodCallAndSByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("DoNothing(1b) + 1b", 2);
    }

    [TestMethod]
    public void WhenSByteMethodCallAndShortUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("DoNothing(1b) + 1s", 2);
    }

    [TestMethod]
    public void WhenSByteMethodCallAndUShortUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("DoNothing(1b) + 1us", 2);
    }

    [TestMethod]
    public void WhenSByteMethodCallAndIntUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("DoNothing(1b) + 1i", 2);
    }

    [TestMethod]
    public void WhenSByteMethodCallAndUIntUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<uint>("DoNothing(1b) + 1ui", 2);
    }

    [TestMethod]
    public void WhenSByteMethodCallAndLongUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<long>("DoNothing(1b) + 1l", 2);
    }

    [TestMethod]
    public void WhenSByteMethodCallAndDecimalUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("DoNothing(1b) + 1d", 2);
    }
    
    [TestMethod]
    public void WhenShortMethodCallAndByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("DoNothing(1s) + 1ub", 2);
    }

    [TestMethod]
    public void WhenShortMethodCallAndSByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("DoNothing(1s) + 1b", 2);
    }

    [TestMethod]
    public void WhenShortMethodCallAndShortUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<short>("DoNothing(1s) + 1s", 2);
    }

    [TestMethod]
    public void WhenShortMethodCallAndUShortUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("DoNothing(1s) + 1us", 2);
    }

    [TestMethod]
    public void WhenShortMethodCallAndIntUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("DoNothing(1s) + 1i", 2);
    }

    [TestMethod]
    public void WhenShortMethodCallAndUIntUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<uint>("DoNothing(1s) + 1ui", 2);
    }

    [TestMethod]
    public void WhenShortMethodCallAndLongUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<long>("DoNothing(1s) + 1l", 2);
    }

    [TestMethod]
    public void WhenShortMethodCallAndDecimalUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("DoNothing(1s) + 1d", 2);
    }
    
    [TestMethod]
    public void WhenUShortMethodCallAndByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("DoNothing(1us) + 1ub", 2);
    }

    [TestMethod]
    public void WhenUShortMethodCallAndSByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("DoNothing(1us) + 1b", 2);
    }

    [TestMethod]
    public void WhenUShortMethodCallAndShortUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("DoNothing(1us) + 1s", 2);
    }

    [TestMethod]
    public void WhenUShortMethodCallAndUShortUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<ushort>("DoNothing(1us) + 1us", 2);
    }

    [TestMethod]
    public void WhenUShortMethodCallAndIntUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("DoNothing(1us) + 1i", 2);
    }

    [TestMethod]
    public void WhenUShortMethodCallAndUIntUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<uint>("DoNothing(1us) + 1ui", 2);
    }

    [TestMethod]
    public void WhenUShortMethodCallAndLongUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<long>("DoNothing(1us) + 1l", 2);
    }

    [TestMethod]
    public void WhenUShortMethodCallAndULongUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<ulong>("DoNothing(1us) + 1ul", 2);
    }

    [TestMethod]
    public void WhenUShortMethodCallAndDecimalUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("DoNothing(1us) + 1d", 2);
    }
    
    [TestMethod]
    public void WhenIntMethodCallAndByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("DoNothing(1) + 1ub", 2);
    }

    [TestMethod]
    public void WhenIntMethodCallAndSByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("DoNothing(1) + 1b", 2);
    }

    [TestMethod]
    public void WhenIntMethodCallAndShortUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("DoNothing(1) + 1s", 2);
    }

    [TestMethod]
    public void WhenIntMethodCallAndUShortUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("DoNothing(1) + 1us", 2);
    }

    [TestMethod]
    public void WhenIntMethodCallAndIntUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("DoNothing(1) + 1i", 2);
    }

    [TestMethod]
    public void WhenIntMethodCallAndUIntUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<uint>("DoNothing(1) + 1ui", 2);
    }

    [TestMethod]
    public void WhenIntMethodCallAndLongUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<long>("DoNothing(1) + 1l", 2);
    }

    [TestMethod]
    public void WhenIntMethodCallAndDecimalUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("DoNothing(1) + 1d", 2);
    }
    
    [TestMethod]
    public void WhenUIntMethodCallAndByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<uint>("DoNothing(1ui) + 1ub", 2);
    }

    [TestMethod]
    public void WhenUIntMethodCallAndSByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<uint>("DoNothing(1ui) + 1b", 2);
    }

    [TestMethod]
    public void WhenUIntMethodCallAndShortUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<uint>("DoNothing(1ui) + 1s", 2);
    }

    [TestMethod]
    public void WhenUIntMethodCallAndUShortUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<uint>("DoNothing(1ui) + 1us", 2);
    }

    [TestMethod]
    public void WhenUIntMethodCallAndIntUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<uint>("DoNothing(1ui) + 1i", 2);
    }

    [TestMethod]
    public void WhenUIntMethodCallAndUIntUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<uint>("DoNothing(1ui) + 1ui", 2);
    }

    [TestMethod]
    public void WhenUIntMethodCallAndLongUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<ulong>("DoNothing(1ui) + 1l", 2);
    }

    [TestMethod]
    public void WhenUIntMethodCallAndULongUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<ulong>("DoNothing(1ui) + 1ul", 2);
    }

    [TestMethod]
    public void WhenUIntMethodCallAndDecimalUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("DoNothing(1ui) + 1d", 2);
    }
    
    [TestMethod]
    public void WhenLongMethodCallAndByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<long>("DoNothing(1l) + 1ub", 2);
    }

    [TestMethod]
    public void WhenLongMethodCallAndSByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<long>("DoNothing(1l) + 1b", 2);
    }

    [TestMethod]
    public void WhenLongMethodCallAndShortUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<long>("DoNothing(1l) + 1s", 2);
    }

    [TestMethod]
    public void WhenLongMethodCallAndUShortUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<long>("DoNothing(1l) + 1us", 2);
    }

    [TestMethod]
    public void WhenLongMethodCallAndIntUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<long>("DoNothing(1l) + 1i", 2);
    }

    [TestMethod]
    public void WhenLongMethodCallAndUIntUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<ulong>("DoNothing(1l) + 1ui", 2);
    }

    [TestMethod]
    public void WhenLongMethodCallAndLongUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<long>("DoNothing(1l) + 1l", 2);
    }

    [TestMethod]
    public void WhenLongMethodCallAndDecimalUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("DoNothing(1l) + 1d", 2);
    }
    
    [TestMethod]
    public void WhenULongMethodCallAndByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<ulong>("DoNothing(1ul) + 1ub", 2);
    }

    [TestMethod]
    public void WhenULongMethodCallAndUShortUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<ulong>("DoNothing(1ul) + 1us", 2);
    }

    [TestMethod]
    public void WhenULongMethodCallAndUIntUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<ulong>("DoNothing(1ul) + 1ui", 2);
    }

    [TestMethod]
    public void WhenULongMethodCallAndULongUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<ulong>("DoNothing(1ul) + 1ul", 2);
    }

    [TestMethod]
    public void WhenULongMethodCallAndDecimalUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("DoNothing(1ul) + 1d", 2);
    }
    
    [TestMethod]
    public void WhenDecimalMethodCallAndByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("DoNothing(1d) + 1ub", 2);
    }

    [TestMethod]
    public void WhenDecimalMethodCallAndSByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("DoNothing(1d) + 1b", 2);
    }

    [TestMethod]
    public void WhenDecimalMethodCallAndShortUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("DoNothing(1d) + 1s", 2);
    }

    [TestMethod]
    public void WhenDecimalMethodCallAndUShortUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("DoNothing(1d) + 1us", 2);
    }

    [TestMethod]
    public void WhenDecimalMethodCallAndIntUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("DoNothing(1d) + 1i", 2);
    }

    [TestMethod]
    public void WhenDecimalMethodCallAndUIntUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("DoNothing(1d) + 1ui", 2);
    }

    [TestMethod]
    public void WhenDecimalMethodCallAndLongUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("DoNothing(1d) + 1l", 2);
    }

    [TestMethod]
    public void WhenDecimalMethodCallAndULongUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("DoNothing(1d) + 1ul", 2);
    }

    [TestMethod]
    public void WhenDecimalMethodCallAndDecimalUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("DoNothing(1d) + 1d", 2);
    }
}
