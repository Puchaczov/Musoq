using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins.Tests;

[TestClass]
public sealed class PrimitiveConversionSurfaceInventoryTests
{
    private static readonly Type[] NumericConversionSourceTypes =
    [
        typeof(string),
        typeof(byte?),
        typeof(sbyte?),
        typeof(short?),
        typeof(ushort?),
        typeof(int?),
        typeof(uint?),
        typeof(long?),
        typeof(ulong?),
        typeof(float?),
        typeof(double?),
        typeof(decimal?),
        typeof(bool?),
        typeof(char?),
        typeof(object)
    ];

    private static readonly string[] NumericConversionTargets =
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
        "ToDecimal"
    ];

    [TestMethod]
    public void RequiredPrimitiveConversionTargets_ShouldExposeTypedOverloadsForCastCodegen()
    {
        foreach (var target in NumericConversionTargets)
        {
            foreach (var sourceType in NumericConversionSourceTypes)
                AssertHasPublicBindableOverload(target, sourceType);
        }

        foreach (var sourceType in NumericConversionSourceTypes)
            AssertHasPublicBindableOverload("ToChar", sourceType);

        foreach (var sourceType in NumericConversionSourceTypes.Concat(
                     [typeof(char?), typeof(DateTime?), typeof(DateTimeOffset?), typeof(TimeSpan?), typeof(Guid?)]))
        {
            AssertHasPublicBindableOverload("ToString", sourceType);
        }

        foreach (var sourceType in new[] { typeof(string), typeof(DateTime?), typeof(DateTimeOffset?), typeof(object) })
        {
            AssertHasPublicBindableOverload("ToDateTime", sourceType);
            AssertHasPublicBindableOverload("ToDateTimeOffset", sourceType);
        }

        foreach (var sourceType in new[] { typeof(string), typeof(TimeSpan?), typeof(object) })
            AssertHasPublicBindableOverload("ToTimeSpan", sourceType);

        foreach (var sourceType in new[] { typeof(string), typeof(Guid?), typeof(object) })
            AssertHasPublicBindableOverload("ToGuid", sourceType);
    }

    private static void AssertHasPublicBindableOverload(string methodName, Type sourceType)
    {
        var method = typeof(LibraryBase)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .SingleOrDefault(method =>
                string.Equals(method.Name, methodName, StringComparison.Ordinal) &&
                method.GetParameters() is [{ ParameterType: var parameterType }] &&
                parameterType == sourceType);

        Assert.IsNotNull(method, $"Missing typed conversion overload {methodName}({sourceType.Name}).");
        Assert.IsNotNull(
            method.GetCustomAttribute<BindableMethodAttribute>(),
            $"Conversion overload {methodName}({sourceType.Name}) must be bindable.");
    }
}
