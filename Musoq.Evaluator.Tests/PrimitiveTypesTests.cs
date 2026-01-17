using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

// ReSharper disable RedundantTypeArgumentsOfMethod

namespace Musoq.Evaluator.Tests;

[TestClass]
public class PrimitiveTypesTests : BasicEntityTestBase
{
    [TestMethod]
    public void WhenValueOfSbyteTypeOccured_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<sbyte>("1b", 1);
    }

    [TestMethod]
    public void WhenValueOfByteTypeOccured_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<byte>("1ub", 1);
    }

    [TestMethod]
    public void WhenValueOfShortTypeOccured_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<short>("1s", 1);
    }

    [TestMethod]
    public void WhenValueOfUshortTypeOccured_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<ushort>("1us", 1);
    }

    [TestMethod]
    public void WhenValueOfIntTypeOccured_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<int>("1i", 1);
    }

    [TestMethod]
    public void WhenValueOfUintTypeOccured_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<uint>("1ui", 1);
    }

    [TestMethod]
    public void WhenValueOfLongTypeOccured_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<long>("1l", 1);
    }

    [TestMethod]
    public void WhenValueOfUlongTypeOccured_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<ulong>("1ul", 1);
    }

    [TestMethod]
    public void WhenValueOfDecimalTypeOccured_ShouldHaveColumnOfThatType()
    {
        TestMethodTemplate<decimal>("1d", 1);
    }
}