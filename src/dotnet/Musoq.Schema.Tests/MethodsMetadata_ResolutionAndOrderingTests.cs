using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins.Attributes;
using Musoq.Schema.Managers;

namespace Musoq.Schema.Tests;

[TestClass]
public class MethodsMetadata_ResolutionAndOrderingTests : MethodsMetadataTestBase
{
    private MethodsMetadata _genericResolution;
    private MethodsMetadata _typeCompatibility;
    private MethodsMetadata _ordering;

    [TestInitialize]
    public void Initialize()
    {
        _genericResolution = CreateMethodsMetadataFor<GenericResolutionMethods>();
        _typeCompatibility = CreateMethodsMetadataFor<TypeCompatibilityMethods>();
        _ordering = CreateMethodsMetadataFor<OrderingMethods>();
    }

    #region Generic Resolution Tests

    [TestMethod]
    public void TryGetMethod_GenericT_ShouldResolveCorrectly()
    {
        var types = new[]
        {
            typeof(string)
        };

        var success = _genericResolution.TryGetMethod("GenericMethod1", types, null, out var method);

        Assert.IsTrue(success);
        Assert.IsNotNull(method);
        Assert.AreEqual("GenericMethod1", method.Name);
        Assert.HasCount(1, method.GetParameters());
    }

    [TestMethod]
    public void TryGetMethod_GenericTArray_ShouldResolveCorrectly()
    {
        var types = new[]
        {
            typeof(string[])
        };

        var success = _genericResolution.TryGetMethod("GenericMethod2", types, null, out var method);

        Assert.IsTrue(success);
        Assert.IsNotNull(method);
        Assert.AreEqual("GenericMethod2", method.Name);
        Assert.HasCount(1, method.GetParameters());
    }

    [TestMethod]
    public void TryGetMethod_GenericEnumerableT_ShouldResolveCorrectly()
    {
        var types = new[]
        {
            typeof(IEnumerable<string>)
        };

        var success = _genericResolution.TryGetMethod("GenericMethod3", types, null, out var method);

        Assert.IsTrue(success);
        Assert.IsNotNull(method);
        Assert.AreEqual("GenericMethod3", method.Name);
        Assert.HasCount(1, method.GetParameters());
    }

    #endregion

    #region Type Compatibility Tests

    [TestMethod]
    public void TryGetMethod_Bool_Compatibility()
    {
        Assert.IsTrue(_typeCompatibility.TryGetMethod("BoolMethod", [typeof(bool)], null, out _),
            "bool -> bool should work");
        Assert.IsFalse(_typeCompatibility.TryGetMethod("ShortMethod", [typeof(bool)], null, out _),
            "bool -> short should not work");
        Assert.IsFalse(_typeCompatibility.TryGetMethod("IntMethod", [typeof(bool)], null, out _),
            "bool -> int should not work");
        Assert.IsFalse(_typeCompatibility.TryGetMethod("LongMethod", [typeof(bool)], null, out _),
            "bool -> long should not work");
    }

    [TestMethod]
    public void TryGetMethod_Short_Compatibility()
    {
        Assert.IsTrue(_typeCompatibility.TryGetMethod("ShortMethod", [typeof(short)], null, out _),
            "short -> short should work");
        Assert.IsFalse(_typeCompatibility.TryGetMethod("BoolMethod", [typeof(short)], null, out _),
            "short -> bool should not work");
        Assert.IsTrue(_typeCompatibility.TryGetMethod("IntMethod", [typeof(short)], null, out _),
            "short -> int should work");
        Assert.IsTrue(_typeCompatibility.TryGetMethod("LongMethod", [typeof(short)], null, out _),
            "short -> long should work");
    }

    [TestMethod]
    public void TryGetMethod_Int_Compatibility()
    {
        Assert.IsTrue(_typeCompatibility.TryGetMethod("IntMethod", [typeof(int)], null, out _),
            "int -> int should work");
        Assert.IsFalse(_typeCompatibility.TryGetMethod("ShortMethod", [typeof(int)], null, out _),
            "int -> short should not work");
        Assert.IsFalse(_typeCompatibility.TryGetMethod("BoolMethod", [typeof(int)], null, out _),
            "int -> bool should not work");
        Assert.IsTrue(_typeCompatibility.TryGetMethod("LongMethod", [typeof(int)], null, out _),
            "int -> long should work");
    }

    [TestMethod]
    public void TryGetMethod_Long_Compatibility()
    {
        Assert.IsTrue(_typeCompatibility.TryGetMethod("LongMethod", [typeof(long)], null, out _),
            "long -> long should work");
        Assert.IsFalse(_typeCompatibility.TryGetMethod("IntMethod", [typeof(long)], null, out _),
            "long -> int should not work");
        Assert.IsFalse(_typeCompatibility.TryGetMethod("ShortMethod", [typeof(long)], null, out _),
            "long -> short should not work");
        Assert.IsFalse(_typeCompatibility.TryGetMethod("BoolMethod", [typeof(long)], null, out _),
            "long -> bool should not work");
    }

    [TestMethod]
    public void TryGetMethod_DateTimeTypes_Compatibility()
    {
        Assert.IsTrue(_typeCompatibility.TryGetMethod("DateTimeOffsetMethod", [typeof(DateTimeOffset)], null,
            out _));
        Assert.IsFalse(_typeCompatibility.TryGetMethod("DateTimeOffsetMethod", [typeof(DateTime)], null, out _));

        Assert.IsTrue(_typeCompatibility.TryGetMethod("DateTimeMethod", [typeof(DateTime)], null, out _));
        Assert.IsFalse(_typeCompatibility.TryGetMethod("DateTimeMethod", [typeof(DateTimeOffset)], null, out _));

        Assert.IsTrue(_typeCompatibility.TryGetMethod("TimeSpanMethod", [typeof(TimeSpan)], null, out _));
        Assert.IsFalse(_typeCompatibility.TryGetMethod("TimeSpanMethod", [typeof(DateTime)], null, out _));
    }

    [TestMethod]
    public void TryGetMethod_StringAndDecimal_StrictTypeMatching()
    {
        Assert.IsTrue(_typeCompatibility.TryGetMethod("StringMethod", [typeof(string)], null, out _),
            "string -> string should work");
        Assert.IsFalse(_typeCompatibility.TryGetMethod("StringMethod", [typeof(object)], null, out _),
            "object -> string should not work");

        Assert.IsTrue(_typeCompatibility.TryGetMethod("DecimalMethod", [typeof(decimal)], null, out _),
            "decimal -> decimal should work");
        Assert.IsFalse(_typeCompatibility.TryGetMethod("DecimalMethod", [typeof(double)], null, out _),
            "double -> decimal should not work");
    }

    [TestMethod]
    public void TryGetMethod_InheritanceBasedCompatibility()
    {
        Assert.IsTrue(_typeCompatibility.TryGetMethod("BaseClassMethod", [typeof(Animal)], null, out _),
            "Animal -> Animal should work");
        Assert.IsTrue(_typeCompatibility.TryGetMethod("BaseClassMethod", [typeof(Dog)], null, out _),
            "Dog -> Animal should work");
    }

    #endregion

    #region Method Ordering Tests

    [TestMethod]
    public void TryGetMethod_NumericTypes_ExactMatch()
    {
        var result = _ordering.TryGetMethod("NumericMethod", [typeof(int)], null, out var method);
        Assert.IsTrue(result);
        Assert.AreEqual(typeof(int), method.GetParameters()[0].ParameterType);
    }

    [TestMethod]
    public void TryGetMethod_NumericTypes_ImplicitConversion()
    {
        var result = _ordering.TryGetMethod("NumericMethod", [typeof(short)], null, out var method);
        Assert.IsTrue(result);
        Assert.AreEqual(typeof(short), method.GetParameters()[0].ParameterType);
    }

    [TestMethod]
    public void TryGetMethod_Generic_PreferNonGeneric()
    {
        var result = _ordering.TryGetMethod("GenericMethod", [typeof(int)], null, out var method);
        Assert.IsTrue(result);
        Assert.IsFalse(method.IsGenericMethod);
        Assert.AreEqual(typeof(int), method.GetParameters()[0].ParameterType);

        result = _ordering.TryGetMethod("GenericMethod", [typeof(DateTime)], null, out method);
        Assert.IsTrue(result);
        Assert.IsTrue(method.IsGenericMethod);
    }

    [TestMethod]
    public void TryGetMethod_MixedParams_MostSpecificMatch()
    {
        var result =
            _ordering.TryGetMethod("MixedParams", [typeof(int), typeof(Specific)], null, out var method);
        Assert.IsTrue(result);
        var parameters = method.GetParameters();
        Assert.AreEqual(typeof(int), parameters[0].ParameterType);
        Assert.AreEqual(typeof(Specific), parameters[1].ParameterType);

        result = _ordering.TryGetMethod("MixedParams", [typeof(int), typeof(Base)], null, out method);
        Assert.IsTrue(result);
        parameters = method.GetParameters();
        Assert.AreEqual(typeof(int), parameters[0].ParameterType);
        Assert.AreEqual(typeof(Base), parameters[1].ParameterType);
    }

    [TestMethod]
    public void TryGetMethod_OptionalParams_PreferFewerParameters()
    {
        var result =
            _ordering.TryGetMethod("OptionalParams", [typeof(int), typeof(string)], null, out var method);
        Assert.IsTrue(result);
        Assert.HasCount(2, method.GetParameters());

        result = _ordering.TryGetMethod("OptionalParams", [typeof(int), typeof(string), typeof(int)], null,
            out method);
        Assert.IsTrue(result);
        Assert.HasCount(3, method.GetParameters());
    }

    [TestMethod]
    public void TryGetMethod_Context_PreferContextSpecific()
    {
        var result = _ordering.TryGetMethod("ContextMethod", [typeof(int)], null, out var method);
        Assert.IsTrue(result);
        Assert.IsFalse(Attribute.IsDefined(method, typeof(AggregationMethodAttribute)));

        result = _ordering.TryGetMethod("ContextMethod", [typeof(string), typeof(int)], null, out method);
        Assert.IsTrue(result);
        Assert.IsTrue(Attribute.IsDefined(method, typeof(AggregationMethodAttribute)));
    }

    [TestMethod]
    public void TryGetMethod_AmbiguousCall_ShouldResolveCorrectly()
    {
        var result =
            _ordering.TryGetMethod("MixedParams", [typeof(int), typeof(MoreSpecific)], null, out var method);
        Assert.IsTrue(result);
        var parameters = method.GetParameters();

        Assert.AreEqual(typeof(int), parameters[0].ParameterType);
        Assert.AreEqual(typeof(Specific), parameters[1].ParameterType);
    }

    #endregion

    #region Test Method Classes

    private class GenericResolutionMethods
    {
        public void GenericMethod1<T>(T obj)
        {
        }

        public void GenericMethod2<T>(T[] objs)
        {
        }

        public void GenericMethod3<T>(IEnumerable<T> objs)
        {
        }
    }

    private class TypeCompatibilityMethods
    {
        public void BoolMethod(bool x)
        {
        }

        public void ShortMethod(short x)
        {
        }

        public void IntMethod(int x)
        {
        }

        public void LongMethod(long x)
        {
        }

        public void DateTimeOffsetMethod(DateTimeOffset date)
        {
        }

        public void DateTimeMethod(DateTime date)
        {
        }

        public void TimeSpanMethod(TimeSpan span)
        {
        }

        public void StringMethod(string text)
        {
        }

        public void DecimalMethod(decimal value)
        {
        }

        public void BaseClassMethod(Animal animal)
        {
        }
    }

    private class Animal
    {
    }

    private class Dog : Animal
    {
    }

    private interface IBase
    {
    }

    private interface ISpecific : IBase
    {
    }

    private class Base : IBase
    {
    }

    private class Specific : Base, ISpecific
    {
    }

    private class MoreSpecific : Specific
    {
    }

    private class OrderingMethods
    {
        public void NumericMethod(int x)
        {
        }

        public void NumericMethod(long x)
        {
        }

        public void NumericMethod(short x)
        {
        }

        public string InheritanceMethod([InjectSpecificSource(typeof(IBase))] IBase x)
        {
            return "IBase";
        }

        public string InheritanceMethod([InjectSpecificSource(typeof(ISpecific))] ISpecific x)
        {
            return "ISpecific";
        }

        public string InheritanceMethod([InjectSpecificSource(typeof(Base))] Base x)
        {
            return "Base";
        }

        public string InheritanceMethod([InjectSpecificSource(typeof(Specific))] Specific x)
        {
            return "Specific";
        }

        public void GenericMethod<T>(T value)
        {
        }

        public void GenericMethod(int value)
        {
        }

        public void GenericMethod(string value)
        {
        }

        public string MixedParams(int x, Base b)
        {
            return "int,Base";
        }

        public string MixedParams(long x, Specific s)
        {
            return "long,Specific";
        }

        public string MixedParams(int x, Specific s)
        {
            return "int,Specific";
        }

        public string OptionalParams(int x, string s = "default")
        {
            return "optional";
        }

        // ReSharper disable once MethodOverloadWithOptionalParameter
        public string OptionalParams(int x, string s, int y = 42)
        {
            return "more_params";
        }

        [AggregationMethod]
        public void ContextMethod(string name, int value)
        {
        }

        public void ContextMethod(int value)
        {
        }
    }

    #endregion
}
