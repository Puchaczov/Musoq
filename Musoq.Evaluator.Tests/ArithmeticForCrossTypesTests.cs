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
        TestMethodTemplate<int>("1ub + 1ub", 2);
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
    public void WhenByteAndFloatUsed_ShouldHaveColumnOfFloatType()
    {
        TestMethodTemplate<float>("1ub + ToFloat(1)", 2);
    }

    [TestMethod]
    public void WhenByteAndDoubleUsed_ShouldHaveColumnOfDoubleType()
    {
        TestMethodTemplate<double>("1ub + ToDouble(1)", 2);
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
        TestMethodTemplate<int>("1b + 1b", 2);
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
    public void WhenSByteAndFloatUsed_ShouldHaveColumnOfFloatType()
    {
        TestMethodTemplate<float>("1b + ToFloat(1)", 2);
    }
    
    [TestMethod]
    public void WhenSByteAndDoubleUsed_ShouldHaveColumnOfDoubleType()
    {
        TestMethodTemplate<double>("1b + ToDouble(1)", 2);
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
    public void WhenShortAndFloatUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<float>("1s + ToFloat(1)", 2);
    }
    
    [TestMethod]
    public void WhenShortAndDoubleUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<double>("1s + ToDouble(1)", 2);
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
    public void WhenUShortAndFloatUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<float>("1us + ToFloat(1)", 2);
    }
    
    [TestMethod]
    public void WhenUShortAndDoubleUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<double>("1us + ToDouble(1)", 2);
    }

    [TestMethod]
    public void WhenUShortAndDecimalUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("1us + 1d", 2);
    }
    
    [TestMethod]
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
    public void WhenIntAndFloatUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<float>("1i + ToFloat(1)", 2);
    }
    
    [TestMethod]
    public void WhenIntAndDoubleUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<double>("1i + ToDouble(1)", 2);
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
    public void WhenUIntAndFloatUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<float>("1ui + ToFloat(1)", 2);
    }
    
    [TestMethod]
    public void WhenUIntAndDoubleUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<double>("1ui + ToDouble(1)", 2);
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
    public void WhenLongAndFloatUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<float>("1l + ToFloat(1)", 2);
    }
    
    [TestMethod]
    public void WhenLongAndDoubleUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<double>("1l + ToDouble(1)", 2);
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
    public void WhenByteAndFloatUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<float>("1ub + ToFloat(1)", 2);
    }
    
    [TestMethod]
    public void WhenByteAndDoubleUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<double>("1ub + ToDouble(1)", 2);
    }
    
    [TestMethod]
    public void WhenFloatAndSByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<float>("ToFloat(1) + 1b", 2.0f);
    }

    [TestMethod]
    public void WhenFloatAndByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<float>("ToFloat(1) + 1ub", 2.0f);
    }

    [TestMethod]
    public void WhenFloatAndShortUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<float>("ToFloat(1) + 1s", 2.0f);
    }

    [TestMethod]
    public void WhenFloatAndUShortUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<float>("ToFloat(1) + 1us", 2.0f);
    }

    [TestMethod]
    public void WhenFloatAndIntUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<float>("ToFloat(1) + 1i", 2.0f);
    }

    [TestMethod]
    public void WhenFloatAndUIntUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<float>("ToFloat(1) + 1ui", 2.0f);
    }

    [TestMethod]
    public void WhenFloatAndLongUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<float>("ToFloat(1) + 1l", 2.0f);
    }

    [TestMethod]
    public void WhenFloatAndULongUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<float>("ToFloat(1) + 1ul", 2.0f);
    }
    
    [TestMethod]
    public void WhenDoubleAndSByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<double>("ToDouble(1) + 1b", 2.0);
    }

    [TestMethod]
    public void WhenDoubleAndByteUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<double>("ToDouble(1) + 1ub", 2.0);
    }

    [TestMethod]
    public void WhenDoubleAndShortUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<double>("ToDouble(1) + 1s", 2.0);
    }

    [TestMethod]
    public void WhenDoubleAndUShortUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<double>("ToDouble(1) + 1us", 2.0);
    }

    [TestMethod]
    public void WhenDoubleAndIntUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<double>("ToDouble(1) + 1i", 2.0);
    }

    [TestMethod]
    public void WhenDoubleAndUIntUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<double>("ToDouble(1) + 1ui", 2.0);
    }

    [TestMethod]
    public void WhenDoubleAndLongUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<double>("ToDouble(1) + 1l", 2.0);
    }

    [TestMethod]
    public void WhenDoubleAndULongUsed_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<double>("ToDouble(1) + 1ul", 2.0);
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