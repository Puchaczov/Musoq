using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins.Tests;

[TestClass]
public class PrimitiveConversionSurfaceTests : PluginsTestBase
{
    private static readonly string[] RequiredConversionMethods =
    [
        "ToBoolean",
        "ToByte",
        "ToSByte",
        "ToInt16",
        "ToUInt16",
        "ToInt32",
        "ToUInt32",
        "ToInt64",
        "ToUInt64",
        "ToSingle",
        "ToDouble",
        "ToDecimal",
        "ToChar",
        "ToString",
        "ToDateTime",
        "ToDateTimeOffset",
        "ToTimeSpan",
        "ToGuid"
    ];

    [TestMethod]
    public void RequiredPrimitiveConversionTargets_ShouldExposeBindableToMethods()
    {
        var methods = typeof(LibraryBase)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(method => method.GetCustomAttribute<BindableMethodAttribute>() != null)
            .Select(method => method.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var methodName in RequiredConversionMethods)
            Assert.IsTrue(methods.Contains(methodName), $"Missing bindable conversion method {methodName}.");
    }

    [TestMethod]
    public void NewPrimitiveConversions_FromStrings_ShouldReturnExpectedValues()
    {
        var guid = Guid.Parse("5f38db3f-6a65-4ab6-8ad4-c14d8de21a1c");

        Assert.AreEqual(true, Library.ToBoolean("true"));
        Assert.AreEqual((byte?)12, Library.ToByte("12"));
        Assert.AreEqual((sbyte?)-12, Library.ToSByte("-12"));
        Assert.AreEqual((short?)-1234, Library.ToInt16("-1234"));
        Assert.AreEqual((ushort?)1234, Library.ToUInt16("1234"));
        Assert.AreEqual((uint?)1234, Library.ToUInt32("1234"));
        Assert.AreEqual((ulong?)1234, Library.ToUInt64("1234"));
        Assert.AreEqual(12.5f, Library.ToSingle("12.5"));
        Assert.AreEqual(guid, Library.ToGuid(guid.ToString()));
    }

    [TestMethod]
    public void NewPrimitiveConversions_InvalidStrings_ShouldReturnNull()
    {
        Assert.IsNull(Library.ToBoolean("1"));
        Assert.IsNull(Library.ToByte("999"));
        Assert.IsNull(Library.ToSByte("999"));
        Assert.IsNull(Library.ToInt16("999999"));
        Assert.IsNull(Library.ToUInt16("-1"));
        Assert.IsNull(Library.ToUInt32("-1"));
        Assert.IsNull(Library.ToUInt64("-1"));
        Assert.IsNull(Library.ToSingle("not-a-number"));
        Assert.IsNull(Library.ToGuid("not-a-guid"));
    }

    [TestMethod]
    public void NewPrimitiveConversions_NullInputs_ShouldReturnNull()
    {
        Assert.IsNull(Library.ToBoolean((string?)null));
        Assert.IsNull(Library.ToByte((int?)null));
        Assert.IsNull(Library.ToSByte((long?)null));
        Assert.IsNull(Library.ToInt16((decimal?)null));
        Assert.IsNull(Library.ToUInt16((double?)null));
        Assert.IsNull(Library.ToUInt32((object?)null));
        Assert.IsNull(Library.ToUInt64((string?)null));
        Assert.IsNull(Library.ToSingle((object?)null));
        Assert.IsNull(Library.ToGuid((object?)null));
    }

    [TestMethod]
    public void NewPrimitiveConversions_NumericOverflow_ShouldReturnNull()
    {
        Assert.IsNull(Library.ToByte((int?)256));
        Assert.IsNull(Library.ToSByte((int?)128));
        Assert.IsNull(Library.ToInt16((int?)32768));
        Assert.IsNull(Library.ToUInt16((int?)-1));
        Assert.IsNull(Library.ToUInt32((long?)-1));
        Assert.IsNull(Library.ToUInt64((long?)-1));
    }

    [TestMethod]
    public void ExistingPrimitiveConversions_NewOverloads_ShouldReturnExpectedValues()
    {
        Assert.AreEqual(12.5f, Library.ToFloat((double?)12.5));
        Assert.AreEqual(12.5f, Library.ToFloat((object)12.5));
        Assert.AreEqual((decimal?)42m, Library.ToDecimal((int?)42));
        Assert.AreEqual((decimal?)42m, Library.ToDecimal((uint?)42));
        Assert.AreEqual((decimal?)42m, Library.ToDecimal((decimal?)42m));
        Assert.AreEqual("42", Library.ToString((short?)42));
        Assert.AreEqual("42", Library.ToString((ushort?)42));
    }

    [TestMethod]
    public void ToGuid_FromGuidAndObject_ShouldReturnExpectedValue()
    {
        var guid = Guid.Parse("176be1c0-6b48-4c15-95d0-e489f1823da1");

        Assert.AreEqual(guid, Library.ToGuid((Guid?)guid));
        Assert.AreEqual(guid, Library.ToGuid((object)guid));
        Assert.AreEqual(guid, Library.ToGuid((object)guid.ToString()));
    }

}
