using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;
using Musoq.Schema.Managers;

namespace Musoq.Schema.Tests;

[TestClass]
public class MethodsMetadata_ParameterHandlingTests : MethodsMetadataTestBase
{
    private MethodsMetadata _nullable;
    private MethodsMetadata _optional;

    [TestInitialize]
    public void Initialize()
    {
        _nullable = CreateMethodsMetadataFor<NullableMethods>();
        _optional = CreateMethodsMetadataFor<OptionalMethods>();
    }

    #region Nullable Parameter Tests

    [TestMethod]
    public void TryGetMethod_NullableValueTypes_WithNullType()
    {
        Assert.IsTrue(
            _nullable.TryGetMethod("NullableInt", [typeof(NullNode.NullType)], null, out var method),
            "Should accept null for nullable int"
        );
        Assert.IsTrue(
            _nullable.TryGetMethod("NullableDateTime", [typeof(NullNode.NullType)], null, out _),
            "Should accept null for nullable DateTime"
        );
        Assert.IsTrue(
            _nullable.TryGetMethod("NullableDecimal", [typeof(NullNode.NullType)], null, out _),
            "Should accept null for nullable decimal"
        );
    }

    [TestMethod]
    public void TryGetMethod_NullableValueTypes_WithActualTypes()
    {
        Assert.IsTrue(
            _nullable.TryGetMethod("NullableInt", [typeof(int)], null, out _),
            "Should accept int for nullable int"
        );
        Assert.IsTrue(
            _nullable.TryGetMethod("NullableDateTime", [typeof(DateTime)], null, out _),
            "Should accept DateTime for nullable DateTime"
        );
        Assert.IsTrue(
            _nullable.TryGetMethod("NullableDecimal", [typeof(decimal)], null, out _),
            "Should accept decimal for nullable decimal"
        );
    }

    [TestMethod]
    public void TryGetMethod_Overloaded_NullableAndNonNullable()
    {
        Assert.IsTrue(
            _nullable.TryGetMethod("OverloadedInt", [typeof(int)], null, out var nonNullable),
            "Should resolve non-nullable overload for int"
        );

        Assert.IsTrue(
            _nullable.TryGetMethod("OverloadedInt", [typeof(NullNode.NullType)], null, out var nullable),
            "Should resolve nullable overload for null"
        );

        Assert.AreNotEqual(nullable, nonNullable, "Should resolve to different overloads");
    }

    [TestMethod]
    public void TryGetMethod_ReferenceTypes_WithNull()
    {
        Assert.IsTrue(
            _nullable.TryGetMethod("StringMethod", [typeof(NullNode.NullType)], null, out _),
            "String should accept null"
        );

        Assert.IsTrue(
            _nullable.TryGetMethod("ObjectMethod", [typeof(NullNode.NullType)], null, out _),
            "Object should accept null"
        );
    }

    [TestMethod]
    public void TryGetMethod_MixedNullables_AllCombinations()
    {
        Assert.IsTrue(
            _nullable.TryGetMethod("MixedNullables",
                [typeof(NullNode.NullType), typeof(string), typeof(DateTime)],
                null,
                out _),
            "Should accept null for nullable int"
        );

        Assert.IsTrue(
            _nullable.TryGetMethod("MixedNullables",
                [typeof(int), typeof(string), typeof(NullNode.NullType)],
                null,
                out _),
            "Should accept null for nullable DateTime"
        );

        Assert.IsTrue(
            _nullable.TryGetMethod("MixedNullables",
                [typeof(int), typeof(NullNode.NullType), typeof(DateTime)],
                null,
                out _),
            "Should accept null for string"
        );
    }

    [TestMethod]
    public void TryGetMethod_OptionalNullable_AllCombinations()
    {
        Assert.IsTrue(
            _nullable.TryGetMethod("OptionalNullable", [], null, out _),
            "Should work with no parameters (default null)"
        );

        Assert.IsTrue(
            _nullable.TryGetMethod("OptionalNullable", [typeof(NullNode.NullType)], null, out _),
            "Should accept explicit null"
        );

        Assert.IsTrue(
            _nullable.TryGetMethod("OptionalNullable", [typeof(int)], null, out _),
            "Should accept actual value"
        );
    }

    [TestMethod]
    public void TryGetMethod_OptionalNullableWithDefault_AllCombinations()
    {
        Assert.IsTrue(
            _nullable.TryGetMethod("OptionalNullableWithDefault", [], null, out _),
            "Should work with no parameters (default value)"
        );

        Assert.IsTrue(
            _nullable.TryGetMethod("OptionalNullableWithDefault", [typeof(NullNode.NullType)], null,
                out _),
            "Should accept explicit null"
        );

        Assert.IsTrue(
            _nullable.TryGetMethod("OptionalNullableWithDefault", [typeof(int)], null, out _),
            "Should accept actual value"
        );
    }

    [TestMethod]
    public void TryGetMethod_GenericNullable_ValueTypes()
    {
        Assert.IsTrue(
            _nullable.TryGetMethod("GenericNullable", [typeof(int?)], null, out _),
            "Should accept nullable int"
        );

        Assert.IsTrue(
            _nullable.TryGetMethod("GenericNullable", [typeof(DateTime?)], null, out _),
            "Should accept nullable DateTime"
        );

        Assert.IsTrue(
            _nullable.TryGetMethod("GenericNullable", [typeof(NullNode.NullType)], null, out _),
            "Should accept null"
        );
    }

    #endregion

    #region Optional Parameter Tests

    [TestMethod]
    public void TryGetMethod_SingleOptionalParameter_AllCombinations()
    {
        Assert.IsTrue(
            _optional.TryGetMethod("SingleOptional", [typeof(int)], null, out var withParam),
            "Should resolve with parameter"
        );

        Assert.IsTrue(
            _optional.TryGetMethod("SingleOptional", [], null, out var withoutParam),
            "Should resolve without parameter"
        );
    }

    [TestMethod]
    public void TryGetMethod_TwoOptionalParameters_AllCombinations()
    {
        Assert.IsTrue(
            _optional.TryGetMethod("TwoOptional", [typeof(int), typeof(int)], null, out _),
            "Should resolve with both parameters"
        );

        Assert.IsTrue(
            _optional.TryGetMethod("TwoOptional", [typeof(int)], null, out _),
            "Should resolve with first parameter only"
        );

        Assert.IsTrue(
            _optional.TryGetMethod("TwoOptional", [], null, out _),
            "Should resolve with no parameters"
        );
    }

    [TestMethod]
    public void TryGetMethod_MixedOptionalRequired_Parameters()
    {
        Assert.IsTrue(
            _optional.TryGetMethod("MixedOptional", [typeof(int)], null, out _),
            "Should resolve with required parameter only"
        );

        Assert.IsTrue(
            _optional.TryGetMethod("MixedOptional", [typeof(int), typeof(string)], null, out _),
            "Should resolve with all parameters"
        );

        Assert.IsFalse(
            _optional.TryGetMethod("MixedOptional", [], null, out _),
            "Should not resolve without required parameter"
        );
    }

    [TestMethod]
    public void TryGetMethod_OverloadedWithOptional_Resolution()
    {
        Assert.IsTrue(
            _optional.TryGetMethod("Overloaded", [typeof(int)], null, out var method1),
            "Should resolve single int parameter"
        );

        Assert.IsTrue(
            _optional.TryGetMethod("Overloaded", [typeof(int), typeof(int)], null, out var method2),
            "Should resolve two int parameters"
        );

        Assert.IsTrue(
            _optional.TryGetMethod("Overloaded", [typeof(int), typeof(string)], null, out var method3),
            "Should resolve int and string parameters"
        );

        Assert.AreNotEqual(method1, method2, "Should resolve to different method overloads");
        Assert.AreNotEqual(method2, method3, "Should resolve to different method overloads");
        Assert.AreNotEqual(method1, method3, "Should resolve to different method overloads");
    }

    [TestMethod]
    public void TryGetMethod_AllOptionalParameters_AllCombinations()
    {
        Assert.IsTrue(
            _optional.TryGetMethod("AllOptional", [], null, out _),
            "Should resolve with no parameters"
        );

        Assert.IsTrue(
            _optional.TryGetMethod("AllOptional", [typeof(int)], null, out _),
            "Should resolve with one parameter"
        );

        Assert.IsTrue(
            _optional.TryGetMethod("AllOptional", [typeof(int), typeof(int)], null, out _),
            "Should resolve with two parameters"
        );

        Assert.IsTrue(
            _optional.TryGetMethod("AllOptional", [typeof(int), typeof(int), typeof(int)], null, out _),
            "Should resolve with all parameters"
        );
    }

    [TestMethod]
    public void TryGetMethod_MixedTypesOptional_AllCombinations()
    {
        Assert.IsTrue(
            _optional.TryGetMethod("MixedTypes", [typeof(int)], null, out _),
            "Should resolve with required parameter only"
        );

        Assert.IsTrue(
            _optional.TryGetMethod("MixedTypes", [typeof(int), typeof(string)], null, out _),
            "Should resolve with required and first optional"
        );

        Assert.IsTrue(
            _optional.TryGetMethod("MixedTypes", [typeof(int), typeof(string), typeof(int?)], null,
                out _),
            "Should resolve with all parameters"
        );

        Assert.IsFalse(
            _optional.TryGetMethod("MixedTypes", [typeof(int), typeof(int?)], null, out _),
            "Should not resolve with wrong parameter type"
        );
    }

    [TestMethod]
    public void TryGetMethod_NullableParameters_AllCombinations()
    {
        Assert.IsTrue(
            _optional.TryGetMethod("NullableParameters", [], null, out _),
            "Should resolve with no parameters"
        );

        Assert.IsTrue(
            _optional.TryGetMethod("NullableParameters", [typeof(int?)], null, out _),
            "Should resolve with nullable int"
        );

        Assert.IsTrue(
            _optional.TryGetMethod("NullableParameters", [typeof(int?)], null, out _),
            "Should resolve with int"
        );

        Assert.IsTrue(
            _optional.TryGetMethod("NullableParameters", [typeof(int?), typeof(string)], null, out _),
            "Should resolve with all parameters"
        );
    }

    #endregion

    #region Test Method Classes

    private class NullableMethods
    {
        public void NullableInt(int? x)
        {
        }

        public void NullableDateTime(DateTime? dt)
        {
        }

        public void NullableDecimal(decimal? d)
        {
        }

        public void OverloadedInt(int x)
        {
        }

        public void OverloadedInt(int? x)
        {
        }

        public void StringMethod(string s)
        {
        }

        public void ObjectMethod(object o)
        {
        }

        public void MixedNullables(int? x, string y, DateTime? dt)
        {
        }

        public void OptionalNullable(int? x = null)
        {
        }

        public void OptionalNullableWithDefault(int? x = 42)
        {
        }

        public void GenericNullable<T>(T? x) where T : struct
        {
        }
    }

    private class OptionalMethods
    {
        public void SingleOptional(int x = 42)
        {
        }

        public void TwoOptional(int x = 1, int y = 2)
        {
        }

        public void MixedOptional(int required, string optional = "default")
        {
        }

        public void Overloaded(int x)
        {
        }

        public void Overloaded(int x, int y = 10)
        {
        }

        public void Overloaded(int x, string y = "default")
        {
        }

        public void AllOptional(int a = 1, int b = 2, int c = 3)
        {
        }

        public void MixedTypes(int required, string optional1 = "test", int? optional2 = null)
        {
        }

        public void NullableParameters(int? x = null, string y = null)
        {
        }
    }

    #endregion
}
