using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;
using Musoq.Plugins.Attributes;
using Musoq.Schema.Managers;

namespace Musoq.Schema.Tests;

[TestClass]
public class MethodsMetadataEdgeCasesMethodResolutionTests
{
    private MethodsMetadata _methodsMetadata;

    [TestInitialize]
    public void Initialize()
    {
        _methodsMetadata = new TestMethodsMetadata();
    }

    [TestMethod]
    public void TryGetMethod_GenericConstraints_ValueType()
    {
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("ValueTypeGeneric", [typeof(int)], null, out var method),
            "Should resolve for value type"
        );

        Assert.IsFalse(
            _methodsMetadata.TryGetMethod("ValueTypeGeneric", [typeof(string)], null, out _),
            "Should not resolve for reference type"
        );
    }

    [TestMethod]
    public void TryGetMethod_GenericConstraints_ReferenceType()
    {
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("ReferenceTypeGeneric", [typeof(string)], null, out var method),
            "Should resolve for reference type"
        );

        Assert.IsFalse(
            _methodsMetadata.TryGetMethod("ReferenceTypeGeneric", [typeof(int)], null, out _),
            "Should not resolve for value type"
        );
    }

    [TestMethod]
    public void TryGetMethod_GenericConstraints_NullableStruct()
    {
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("NullableStructGeneric", [typeof(int?)], null, out var method),
            "Should resolve for nullable value type"
        );

        Assert.IsFalse(
            _methodsMetadata.TryGetMethod("NullableStructGeneric", [typeof(string)], null, out _),
            "Should not resolve for reference type"
        );

        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("NullableStructGeneric", [typeof(NullNode.NullType)], null, out _),
            "Should resolve for null value"
        );
    }

    [TestMethod]
    public void TryGetMethod_DynamicType_Resolution()
    {
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("DynamicMethod", [typeof(ExpandoObject)], null, out var method),
            "Should resolve dynamic method for ExpandoObject"
        );

        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("DynamicMethod", [typeof(int)], null, out method),
            "Should resolve specific int method"
        );
        Assert.AreEqual(typeof(int), method.GetParameters()[0].ParameterType);
    }

    [TestMethod]
    public void TryGetMethod_NullValue_Resolution()
    {
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("NullableMethod", [typeof(NullNode.NullType)], null, out var method),
            "Should resolve method for null value"
        );

        Assert.AreEqual(typeof(int?), method.GetParameters()[0].ParameterType);
    }

    [TestMethod]
    public void TryGetMethod_ArrayTypes_Resolution()
    {
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("ArrayMethod", [typeof(int[])], null, out var method),
            "Should resolve specific array type"
        );
        Assert.AreEqual(typeof(int[]), method.GetParameters()[0].ParameterType);

        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("ArrayMethod", [typeof(string[])], null, out method),
            "Should resolve to Array parameter for different array type"
        );

        Assert.AreEqual(typeof(Array), method.GetParameters()[0].ParameterType);
    }

    [TestMethod]
    public void TryGetMethod_MultipleInjectedParameters()
    {
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("MultipleInjection", [typeof(int)], typeof(BaseEntity), out var method),
            "Should resolve with multiple injected parameters"
        );

        var parameters = method.GetParameters();
        Assert.HasCount(4, parameters, "Should have all parameters");
        Assert.IsTrue(Attribute.IsDefined(parameters[0], typeof(InjectSpecificSourceAttribute)));
        Assert.IsTrue(Attribute.IsDefined(parameters[1], typeof(InjectGroupAttribute)));
        Assert.IsTrue(Attribute.IsDefined(parameters[2], typeof(InjectQueryStatsAttribute)));
    }

    [TestMethod]
    public void TryGetMethod_ComplexParameterCombination()
    {
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("ComplexMethod",
                [typeof(string), typeof(int?), typeof(string), typeof(string)],
                typeof(BaseEntity),
                out var method),
            "Should resolve complex parameter combination"
        );

        var parameters = method.GetParameters();
        Assert.IsNotNull(parameters[^1].GetCustomAttribute<ParamArrayAttribute>(),
            "Last parameter should be params array");
    }

    [TestMethod]
    public void TryGetMethod_ParamsArray_Resolution()
    {
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("ParamsMethod", [typeof(int)], null, out _),
            "Should resolve without params array"
        );

        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("ParamsMethod",
                [typeof(int), typeof(int), typeof(int)],
                null,
                out _),
            "Should resolve with params array values"
        );

        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("ParamsMethod",
                [typeof(string), typeof(int), typeof(string)],
                null,
                out var objectMethod),
            "Should resolve to object[] params for mixed types"
        );

        Assert.AreEqual(typeof(object[]), objectMethod.GetParameters()[1].ParameterType);
    }

    [TestMethod]
    public void TryGetMethod_EdgeCases_InvalidCalls()
    {
        Assert.IsFalse(
            _methodsMetadata.TryGetMethod("NonExistentMethod", [], null, out _),
            "Should fail for non-existent method"
        );

        Assert.IsFalse(
            _methodsMetadata.TryGetMethod("EmptyMethod", [typeof(int)], null, out _),
            "Should fail with wrong parameter count"
        );

        Assert.IsFalse(
            _methodsMetadata.TryGetMethod("MultipleInjection", [typeof(int)], typeof(string), out _),
            "Should fail with wrong entity type"
        );
    }

    private interface IBase
    {
    }

    private class BaseEntity : IBase
    {
    }

    private class DerivedEntity : BaseEntity
    {
    }

    private class TestClass
    {
        public void EmptyMethod()
        {
        }

        public void EmptyMethod([InjectSpecificSource(typeof(IBase))] IBase entity)
        {
        }

        public void DynamicMethod(dynamic value)
        {
        }

        public void DynamicMethod(int value)
        {
        }

        public void DynamicMethod(string value)
        {
        }


        public void NullableMethod(int? value)
        {
        }

        public void NullableMethod(string value)
        {
        }

        public void NullableMethod(object value)
        {
        }

        public void ValueTypeGeneric<T>(T value) where T : struct
        {
        }

        public void ReferenceTypeGeneric<T>(T value) where T : class
        {
        }

        public void NullableStructGeneric<T>(T? value) where T : struct
        {
        }

        public void ArrayMethod(int[] values)
        {
        }

        public void ArrayMethod(Array values)
        {
        }

        public void ArrayMethod(object values)
        {
        }

        public void MultipleInjection(
            [InjectSpecificSource(typeof(IBase))] IBase entity1,
            [InjectGroup] object group,
            [InjectQueryStats] object stats,
            int value)
        {
        }

        public void ComplexMethod<T>(
            [InjectSpecificSource(typeof(IBase))] IBase entity,
            T value,
            int? nullableValue = null,
            params string[] extraParams)
        {
        }

        public void ParamsMethod(int x, params int[] values)
        {
        }

        public void ParamsMethod(string x, params object[] values)
        {
        }
    }

    private class TestMethodsMetadata : MethodsMetadata
    {
        public TestMethodsMetadata()
        {
            var testClass = typeof(TestClass);
            foreach (var method in testClass.GetMethods(BindingFlags.Public | BindingFlags.Instance |
                                                        BindingFlags.DeclaredOnly)) RegisterMethod(method);
        }

        private new void RegisterMethod(MethodInfo methodInfo)
        {
            base.RegisterMethod(methodInfo);
        }
    }

    private class ManyOverloadsClass
    {
        public void OverloadedMethod(byte value)
        {
        }

        public void OverloadedMethod(sbyte value)
        {
        }

        public void OverloadedMethod(short value)
        {
        }

        public void OverloadedMethod(ushort value)
        {
        }

        public void OverloadedMethod(int value)
        {
        }

        public void OverloadedMethod(uint value)
        {
        }

        public void OverloadedMethod(long value)
        {
        }

        public void OverloadedMethod(ulong value)
        {
        }

        public void OverloadedMethod(float value)
        {
        }

        public void OverloadedMethod(double value)
        {
        }

        public void OverloadedMethod(decimal value)
        {
        }

        public void OverloadedMethod(char value)
        {
        }

        public void OverloadedMethod(bool value)
        {
        }

        public void OverloadedMethod(string value)
        {
        }

        public void OverloadedMethod(object value)
        {
        }

        public void OverloadedMethod(DateTime value)
        {
        }

        public void OverloadedMethod(DateTimeOffset value)
        {
        }

        public void OverloadedMethod(TimeSpan value)
        {
        }

        public void OverloadedMethod(Guid value)
        {
        }

        public void OverloadedMethod(Type value)
        {
        }

        public void OverloadedMethod(byte[] value)
        {
        }

        public void OverloadedMethod(int[] value)
        {
        }

        public void OverloadedMethod(string[] value)
        {
        }

        public void OverloadedMethod(IEnumerable<int> value)
        {
        }

        public void OverloadedMethod(IEnumerable<string> value)
        {
        }

        public void OverloadedMethod(List<int> value)
        {
        }

        public void OverloadedMethod(List<string> value)
        {
        }

        public void OverloadedMethod(Dictionary<string, int> value)
        {
        }

        public void OverloadedMethod(Dictionary<string, string> value)
        {
        }

        public void OverloadedMethod(HashSet<int> value)
        {
        }

        public void OverloadedMethod(HashSet<string> value)
        {
        }

        public void OverloadedMethod(int? value)
        {
        }

        public void OverloadedMethod(long? value)
        {
        }

        public void OverloadedMethod(double? value)
        {
        }

        public void OverloadedMethod(decimal? value)
        {
        }
    }

    private class ManyOverloadsTestMetadata : MethodsMetadata
    {
        public ManyOverloadsTestMetadata()
        {
            var testClass = typeof(ManyOverloadsClass);
            foreach (var method in testClass.GetMethods(BindingFlags.Public | BindingFlags.Instance |
                                                        BindingFlags.DeclaredOnly)) RegisterMethod(method);
        }

        private new void RegisterMethod(MethodInfo methodInfo)
        {
            base.RegisterMethod(methodInfo);
        }
    }

    #region Stack-Allocated Sorting Boundary Tests

    [TestMethod]
    public void TryGetMethod_WhenManyOverloads_AtStackAllocBoundary_ShouldResolveSuccessfully()
    {
        var metadata = new ManyOverloadsTestMetadata();

        Assert.IsTrue(
            metadata.TryGetMethod("OverloadedMethod", [typeof(DateTime)], null, out var method),
            "Should resolve a method for DateTime"
        );
        Assert.IsNotNull(method);

        Assert.IsTrue(
            metadata.TryGetMethod("OverloadedMethod", [typeof(Guid)], null, out method),
            "Should resolve a method for Guid"
        );
        Assert.IsNotNull(method);

        Assert.IsTrue(
            metadata.TryGetMethod("OverloadedMethod", [typeof(Type)], null, out method),
            "Should resolve a method for Type"
        );
        Assert.IsNotNull(method);
    }

    /// <summary>
    ///     Tests method resolution when overload count exceeds stack-allocation boundary.
    ///     Ensures heap-allocated path works correctly.
    /// </summary>
    [TestMethod]
    public void TryGetMethod_WhenManyOverloads_ExceedsStackAllocBoundary_ShouldResolveSuccessfully()
    {
        var metadata = new ManyOverloadsTestMetadata();

        Assert.IsTrue(
            metadata.TryGetMethod("OverloadedMethod", [typeof(TimeSpan)], null, out var method),
            "Should resolve a method for TimeSpan"
        );
        Assert.IsNotNull(method);

        Assert.IsTrue(
            metadata.TryGetMethod("OverloadedMethod", [typeof(DateTimeOffset)], null, out method),
            "Should resolve a method for DateTimeOffset"
        );
        Assert.IsNotNull(method);
    }

    /// <summary>
    ///     Tests that method resolution returns consistent results with many overloads.
    /// </summary>
    [TestMethod]
    public void TryGetMethod_WhenManyOverloads_ShouldReturnConsistentResults()
    {
        var metadata = new ManyOverloadsTestMetadata();

        Assert.IsTrue(metadata.TryGetMethod("OverloadedMethod", [typeof(bool)], null, out var method1));
        Assert.IsTrue(metadata.TryGetMethod("OverloadedMethod", [typeof(bool)], null, out var method2));
        Assert.IsTrue(metadata.TryGetMethod("OverloadedMethod", [typeof(bool)], null, out var method3));

        Assert.AreSame(method1, method2, "Should return same method on repeated calls");
        Assert.AreSame(method2, method3, "Should return same method on repeated calls");
    }

    #endregion
}
