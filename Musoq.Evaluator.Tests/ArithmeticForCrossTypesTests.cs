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
        TestMethodTemplate<decimal>("1ub + 1d", 2);
    }
    
    [TestMethod]
    public void WhenSByteAndByteUsed_ShouldHaveColumnOfIntType()
    {
        TestMethodTemplate<int>("1b + 1ub", 2);
    }

    [TestMethod]
    public void WhenSByteAndSByteUsed_ShouldHaveColumnOfSByteType()
    {
        TestMethodTemplate<sbyte>("1b + 1b", 2);
    }

    [TestMethod]
    public void WhenSByteAndShortUsed_ShouldHaveColumnOfIntType()
    {
        TestMethodTemplate<int>("1b + 1s", 2);
    }

    [TestMethod]
    public void WhenSByteAndUShortUsed_ShouldHaveColumnOfIntType()
    {
        TestMethodTemplate<int>("1b + 1us", 2);
    }

    [TestMethod]
    public void WhenSByteAndIntUsed_ShouldHaveColumnOfIntType()
    {
        TestMethodTemplate<int>("1b + 1i", 2);
    }

    [TestMethod]
    public void WhenSByteAndUIntUsed_ShouldHaveColumnOfUIntType()
    {
        TestMethodTemplate<uint>("1b + 1ui", 2);
    }

    [TestMethod]
    public void WhenSByteAndLongUsed_ShouldHaveColumnOfLongType()
    {
        TestMethodTemplate<long>("1b + 1l", 2);
    }

    [TestMethod]
    public void WhenSByteAndDecimalUsed_ShouldHaveColumnOfDecimalType()
    {
        TestMethodTemplate<decimal>("1b + 1d", 2);
    }
    
    [TestMethod]
    public void WhenShortAndByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("1s + 1ub", 2);
    }

    [TestMethod]
    public void WhenShortAndSByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("1s + 1b", 2);
    }

    [TestMethod]
    public void WhenShortAndShortUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<short>("1s + 1s", 2);
    }

    [TestMethod]
    public void WhenShortAndUShortUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("1s + 1us", 2);
    }

    [TestMethod]
    public void WhenShortAndIntUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("1s + 1i", 2);
    }

    [TestMethod]
    public void WhenShortAndUIntUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<uint>("1s + 1ui", 2);
    }

    [TestMethod]
    public void WhenShortAndLongUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<long>("1s + 1l", 2);
    }

    [TestMethod]
    public void WhenShortAndDecimalUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("1s + 1d", 2);
    }
    
    [TestMethod]
    public void WhenUShortAndByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("1us + 1ub", 2);
    }

    [TestMethod]
    public void WhenUShortAndSByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("1us + 1b", 2);
    }

    [TestMethod]
    public void WhenUShortAndShortUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("1us + 1s", 2);
    }

    [TestMethod]
    public void WhenUShortAndUShortUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<ushort>("1us + 1us", 2);
    }

    [TestMethod]
    public void WhenUShortAndIntUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("1us + 1i", 2);
    }

    [TestMethod]
    public void WhenUShortAndUIntUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<uint>("1us + 1ui", 2);
    }

    [TestMethod]
    public void WhenUShortAndLongUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<long>("1us + 1l", 2);
    }

    [TestMethod]
    public void WhenUShortAndULongUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<ulong>("1us + 1ul", 2);
    }

    [TestMethod]
    public void WhenUShortAndDecimalUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("1us + 1d", 2);
    }[TestMethod]
    public void WhenIntAndByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("1i + 1ub", 2);
    }

    [TestMethod]
    public void WhenIntAndSByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("1i + 1b", 2);
    }

    [TestMethod]
    public void WhenIntAndShortUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("1i + 1s", 2);
    }

    [TestMethod]
    public void WhenIntAndUShortUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("1i + 1us", 2);
    }

    [TestMethod]
    public void WhenIntAndIntUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("1i + 1i", 2);
    }

    [TestMethod]
    public void WhenIntAndUIntUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<uint>("1i + 1ui", 2);
    }

    [TestMethod]
    public void WhenIntAndLongUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<long>("1i + 1l", 2);
    }

    [TestMethod]
    public void WhenIntAndDecimalUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("1i + 1d", 2);
    }
    
    [TestMethod]
    public void WhenUIntAndByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<uint>("1ui + 1ub", 2u);
    }

    [TestMethod]
    public void WhenUIntAndSByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<uint>("1ui + 1b", 2u);
    }

    [TestMethod]
    public void WhenUIntAndShortUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<uint>("1ui + 1s", 2u);
    }

    [TestMethod]
    public void WhenUIntAndUShortUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<uint>("1ui + 1us", 2u);
    }

    [TestMethod]
    public void WhenUIntAndIntUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<uint>("1ui + 1i", 2u);
    }

    [TestMethod]
    public void WhenUIntAndUIntUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<uint>("1ui + 1ui", 2u);
    }

    [TestMethod]
    public void WhenUIntAndLongUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<ulong>("1ui + 1l", 2ul);
    }

    [TestMethod]
    public void WhenUIntAndULongUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<ulong>("1ui + 1ul", 2ul);
    }

    [TestMethod]
    public void WhenUIntAndDecimalUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("1ui + 1d", 2m);
    }
    
    [TestMethod]
    public void WhenLongAndByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<long>("1l + 1ub", 2);
    }

    [TestMethod]
    public void WhenLongAndSByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<long>("1l + 1b", 2);
    }

    [TestMethod]
    public void WhenLongAndShortUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<long>("1l + 1s", 2);
    }

    [TestMethod]
    public void WhenLongAndUShortUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<long>("1l + 1us", 2);
    }

    [TestMethod]
    public void WhenLongAndIntUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<long>("1l + 1i", 2);
    }

    [TestMethod]
    public void WhenLongAndUIntUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<ulong>("1l + 1ui", 2);
    }

    [TestMethod]
    public void WhenLongAndLongUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<long>("1l + 1l", 2);
    }

    [TestMethod]
    public void WhenLongAndDecimalUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("1l + 1d", 2);
    }[TestMethod]
    public void WhenULongAndByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<ulong>("1ul + 1ub", 2);
    }

    [TestMethod]
    public void WhenULongAndUShortUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<ulong>("1ul + 1us", 2);
    }

    [TestMethod]
    public void WhenULongAndUIntUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<ulong>("1ul + 1ui", 2);
    }

    [TestMethod]
    public void WhenULongAndULongUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<ulong>("1ul + 1ul", 2);
    }

    [TestMethod]
    public void WhenULongAndDecimalUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("1ul + 1d", 2);
    }

    [TestMethod]
    public void WhenByteAndULongUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<ulong>("1ub + 1ul", 2);
    }

    [TestMethod]
    public void WhenDecimalAndULongUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("1d + 1ul", 2);
    }
    
    [TestMethod]
    public void WhenDecimalAndByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("1.0d + 1ub", 2.0m);
    }

    [TestMethod]
    public void WhenDecimalAndSByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("1.0d + 1b", 2.0m);
    }

    [TestMethod]
    public void WhenDecimalAndShortUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("1.0d + 1s", 2.0m);
    }

    [TestMethod]
    public void WhenDecimalAndUShortUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("1.0d + 1us", 2.0m);
    }

    [TestMethod]
    public void WhenDecimalAndIntUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("1.0d + 1i", 2.0m);
    }

    [TestMethod]
    public void WhenDecimalAndUIntUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("1.0d + 1ui", 2.0m);
    }

    [TestMethod]
    public void WhenDecimalAndLongUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("1.0d + 1l", 2.0m);
    }

    [TestMethod]
    public void WhenDecimalAndDecimalUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("1.0d + 1.0d", 2.0m);
    }
}