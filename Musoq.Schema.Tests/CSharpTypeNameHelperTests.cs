using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Helpers;

namespace Musoq.Schema.Tests;

[TestClass]
public class CSharpTypeNameHelperTests
{
    #region GetCSharpTypeName Tests

    [TestMethod]
    public void GetCSharpTypeName_NullType_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => CSharpTypeNameHelper.GetCSharpTypeName(null));
    }

    [TestMethod]
    public void GetCSharpTypeName_PrimitiveTypes_ReturnsCorrectAliases()
    {
        Assert.AreEqual("int", CSharpTypeNameHelper.GetCSharpTypeName(typeof(int)));
        Assert.AreEqual("long", CSharpTypeNameHelper.GetCSharpTypeName(typeof(long)));
        Assert.AreEqual("short", CSharpTypeNameHelper.GetCSharpTypeName(typeof(short)));
        Assert.AreEqual("byte", CSharpTypeNameHelper.GetCSharpTypeName(typeof(byte)));
        Assert.AreEqual("sbyte", CSharpTypeNameHelper.GetCSharpTypeName(typeof(sbyte)));
        Assert.AreEqual("uint", CSharpTypeNameHelper.GetCSharpTypeName(typeof(uint)));
        Assert.AreEqual("ulong", CSharpTypeNameHelper.GetCSharpTypeName(typeof(ulong)));
        Assert.AreEqual("ushort", CSharpTypeNameHelper.GetCSharpTypeName(typeof(ushort)));
        Assert.AreEqual("bool", CSharpTypeNameHelper.GetCSharpTypeName(typeof(bool)));
        Assert.AreEqual("char", CSharpTypeNameHelper.GetCSharpTypeName(typeof(char)));
        Assert.AreEqual("float", CSharpTypeNameHelper.GetCSharpTypeName(typeof(float)));
        Assert.AreEqual("double", CSharpTypeNameHelper.GetCSharpTypeName(typeof(double)));
        Assert.AreEqual("decimal", CSharpTypeNameHelper.GetCSharpTypeName(typeof(decimal)));
    }

    [TestMethod]
    public void GetCSharpTypeName_ReferenceTypes_ReturnsCorrectAliases()
    {
        Assert.AreEqual("string", CSharpTypeNameHelper.GetCSharpTypeName(typeof(string)));
        Assert.AreEqual("object", CSharpTypeNameHelper.GetCSharpTypeName(typeof(object)));
    }

    [TestMethod]
    public void GetCSharpTypeName_VoidType_ReturnsVoid()
    {
        Assert.AreEqual("void", CSharpTypeNameHelper.GetCSharpTypeName(typeof(void)));
    }

    [TestMethod]
    public void GetCSharpTypeName_NullableValueTypes_ReturnsQuestionMarkSyntax()
    {
        Assert.AreEqual("int?", CSharpTypeNameHelper.GetCSharpTypeName(typeof(int?)));
        Assert.AreEqual("long?", CSharpTypeNameHelper.GetCSharpTypeName(typeof(long?)));
        Assert.AreEqual("bool?", CSharpTypeNameHelper.GetCSharpTypeName(typeof(bool?)));
        Assert.AreEqual("decimal?", CSharpTypeNameHelper.GetCSharpTypeName(typeof(decimal?)));
        Assert.AreEqual("DateTime?", CSharpTypeNameHelper.GetCSharpTypeName(typeof(DateTime?)));
        Assert.AreEqual("Guid?", CSharpTypeNameHelper.GetCSharpTypeName(typeof(Guid?)));
    }

    [TestMethod]
    public void GetCSharpTypeName_SingleDimensionArrays_ReturnsBracketSyntax()
    {
        Assert.AreEqual("int[]", CSharpTypeNameHelper.GetCSharpTypeName(typeof(int[])));
        Assert.AreEqual("string[]", CSharpTypeNameHelper.GetCSharpTypeName(typeof(string[])));
        Assert.AreEqual("DateTime[]", CSharpTypeNameHelper.GetCSharpTypeName(typeof(DateTime[])));
        Assert.AreEqual("int?[]", CSharpTypeNameHelper.GetCSharpTypeName(typeof(int?[])));
    }

    [TestMethod]
    public void GetCSharpTypeName_MultiDimensionalArrays_ReturnsCorrectBrackets()
    {
        Assert.AreEqual("int[,]", CSharpTypeNameHelper.GetCSharpTypeName(typeof(int[,])));
        Assert.AreEqual("string[,]", CSharpTypeNameHelper.GetCSharpTypeName(typeof(string[,])));
        Assert.AreEqual("int[,,]", CSharpTypeNameHelper.GetCSharpTypeName(typeof(int[,,])));
        Assert.AreEqual("double[,,,]", CSharpTypeNameHelper.GetCSharpTypeName(typeof(double[,,,])));
    }

    [TestMethod]
    public void GetCSharpTypeName_SimpleGenericTypes_ReturnsAngleBracketSyntax()
    {
        Assert.AreEqual("List<int>", CSharpTypeNameHelper.GetCSharpTypeName(typeof(List<int>)));
        Assert.AreEqual("List<string>", CSharpTypeNameHelper.GetCSharpTypeName(typeof(List<string>)));
        Assert.AreEqual("IEnumerable<decimal>", CSharpTypeNameHelper.GetCSharpTypeName(typeof(IEnumerable<decimal>)));
    }

    [TestMethod]
    public void GetCSharpTypeName_GenericTypesWithNullableArguments_ReturnsCorrectFormat()
    {
        Assert.AreEqual("List<int?>", CSharpTypeNameHelper.GetCSharpTypeName(typeof(List<int?>)));
        Assert.AreEqual("IEnumerable<DateTime?>",
            CSharpTypeNameHelper.GetCSharpTypeName(typeof(IEnumerable<DateTime?>)));
    }

    [TestMethod]
    public void GetCSharpTypeName_GenericTypesWithMultipleArguments_ReturnsCorrectFormat()
    {
        Assert.AreEqual("Dictionary<int, string>",
            CSharpTypeNameHelper.GetCSharpTypeName(typeof(Dictionary<int, string>)));
        Assert.AreEqual("Dictionary<string, List<int>>",
            CSharpTypeNameHelper.GetCSharpTypeName(typeof(Dictionary<string, List<int>>)));
        Assert.AreEqual("KeyValuePair<int, string>",
            CSharpTypeNameHelper.GetCSharpTypeName(typeof(KeyValuePair<int, string>)));
    }

    [TestMethod]
    public void GetCSharpTypeName_NestedGenericTypes_ReturnsCorrectFormat()
    {
        Assert.AreEqual("List<List<int>>", CSharpTypeNameHelper.GetCSharpTypeName(typeof(List<List<int>>)));
        Assert.AreEqual("Dictionary<string, List<int?>>",
            CSharpTypeNameHelper.GetCSharpTypeName(typeof(Dictionary<string, List<int?>>)));
    }

    [TestMethod]
    public void GetCSharpTypeName_GenericTypeParameter_ReturnsParameterName()
    {
        var method =
            typeof(CSharpTypeNameHelperTests).GetMethod(nameof(GenericMethod),
                BindingFlags.NonPublic | BindingFlags.Static);
        var genericParam = method.GetGenericArguments()[0];

        Assert.AreEqual("T", CSharpTypeNameHelper.GetCSharpTypeName(genericParam));
    }

    [TestMethod]
    public void GetCSharpTypeName_ComplexGenericTypeParameter_ReturnsParameterName()
    {
        var method = typeof(CSharpTypeNameHelperTests).GetMethod(nameof(ComplexGenericMethod),
            BindingFlags.NonPublic | BindingFlags.Static);
        var genericParams = method.GetGenericArguments();

        Assert.AreEqual("TKey", CSharpTypeNameHelper.GetCSharpTypeName(genericParams[0]));
        Assert.AreEqual("TValue", CSharpTypeNameHelper.GetCSharpTypeName(genericParams[1]));
    }

    [TestMethod]
    public void GetCSharpTypeName_CustomType_ReturnsSimpleName()
    {
        Assert.AreEqual("CSharpTypeNameHelperTests",
            CSharpTypeNameHelper.GetCSharpTypeName(typeof(CSharpTypeNameHelperTests)));
        Assert.AreEqual("TestClass", CSharpTypeNameHelper.GetCSharpTypeName(typeof(TestClass)));
    }

    [TestMethod]
    public void GetCSharpTypeName_GenericArrays_ReturnsCorrectFormat()
    {
        Assert.AreEqual("List<int>[]", CSharpTypeNameHelper.GetCSharpTypeName(typeof(List<int>[])));
        Assert.AreEqual("Dictionary<string, int>[]",
            CSharpTypeNameHelper.GetCSharpTypeName(typeof(Dictionary<string, int>[])));
    }

    #endregion

    #region GetCSharpTypeAlias Tests

    [TestMethod]
    public void GetCSharpTypeAlias_NullType_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => CSharpTypeNameHelper.GetCSharpTypeAlias(null));
    }

    [TestMethod]
    public void GetCSharpTypeAlias_AllPrimitiveTypes_ReturnsCorrectAliases()
    {
        Assert.AreEqual("int", CSharpTypeNameHelper.GetCSharpTypeAlias(typeof(int)));
        Assert.AreEqual("long", CSharpTypeNameHelper.GetCSharpTypeAlias(typeof(long)));
        Assert.AreEqual("short", CSharpTypeNameHelper.GetCSharpTypeAlias(typeof(short)));
        Assert.AreEqual("byte", CSharpTypeNameHelper.GetCSharpTypeAlias(typeof(byte)));
        Assert.AreEqual("sbyte", CSharpTypeNameHelper.GetCSharpTypeAlias(typeof(sbyte)));
        Assert.AreEqual("uint", CSharpTypeNameHelper.GetCSharpTypeAlias(typeof(uint)));
        Assert.AreEqual("ulong", CSharpTypeNameHelper.GetCSharpTypeAlias(typeof(ulong)));
        Assert.AreEqual("ushort", CSharpTypeNameHelper.GetCSharpTypeAlias(typeof(ushort)));
        Assert.AreEqual("bool", CSharpTypeNameHelper.GetCSharpTypeAlias(typeof(bool)));
        Assert.AreEqual("char", CSharpTypeNameHelper.GetCSharpTypeAlias(typeof(char)));
        Assert.AreEqual("float", CSharpTypeNameHelper.GetCSharpTypeAlias(typeof(float)));
        Assert.AreEqual("double", CSharpTypeNameHelper.GetCSharpTypeAlias(typeof(double)));
        Assert.AreEqual("decimal", CSharpTypeNameHelper.GetCSharpTypeAlias(typeof(decimal)));
        Assert.AreEqual("string", CSharpTypeNameHelper.GetCSharpTypeAlias(typeof(string)));
        Assert.AreEqual("object", CSharpTypeNameHelper.GetCSharpTypeAlias(typeof(object)));
        Assert.AreEqual("void", CSharpTypeNameHelper.GetCSharpTypeAlias(typeof(void)));
    }

    [TestMethod]
    public void GetCSharpTypeAlias_CustomType_ReturnsSimpleName()
    {
        Assert.AreEqual("TestClass", CSharpTypeNameHelper.GetCSharpTypeAlias(typeof(TestClass)));
        Assert.AreEqual("DateTime", CSharpTypeNameHelper.GetCSharpTypeAlias(typeof(DateTime)));
        Assert.AreEqual("Guid", CSharpTypeNameHelper.GetCSharpTypeAlias(typeof(Guid)));
    }

    #endregion

    #region FormatMethodSignature Tests

    [TestMethod]
    public void FormatMethodSignature_NullMethodInfo_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => CSharpTypeNameHelper.FormatMethodSignature(null));
    }

    [TestMethod]
    public void FormatMethodSignature_SimpleMethod_ReturnsCorrectSignature()
    {
        var method = typeof(TestClass).GetMethod(nameof(TestClass.SimpleMethod));
        var signature = CSharpTypeNameHelper.FormatMethodSignature(method);

        Assert.AreEqual("void SimpleMethod()", signature);
    }

    [TestMethod]
    public void FormatMethodSignature_MethodWithParameters_ReturnsCorrectSignature()
    {
        var method = typeof(TestClass).GetMethod(nameof(TestClass.MethodWithParameters));
        var signature = CSharpTypeNameHelper.FormatMethodSignature(method);

        Assert.AreEqual("int MethodWithParameters(int x, string y)", signature);
    }

    [TestMethod]
    public void FormatMethodSignature_MethodWithNullableParameters_ReturnsCorrectSignature()
    {
        var method = typeof(TestClass).GetMethod(nameof(TestClass.MethodWithNullableParameters));
        var signature = CSharpTypeNameHelper.FormatMethodSignature(method);

        Assert.AreEqual("int? MethodWithNullableParameters(int? value)", signature);
    }

    [TestMethod]
    public void FormatMethodSignature_GenericMethod_ReturnsCorrectSignature()
    {
        var method = typeof(TestClass).GetMethod(nameof(TestClass.GenericMethod));
        var signature = CSharpTypeNameHelper.FormatMethodSignature(method);

        Assert.AreEqual("T GenericMethod<T>(T value)", signature);
    }

    [TestMethod]
    public void FormatMethodSignature_GenericMethodWithMultipleParameters_ReturnsCorrectSignature()
    {
        var method = typeof(TestClass).GetMethod(nameof(TestClass.ComplexGenericMethod));
        var signature = CSharpTypeNameHelper.FormatMethodSignature(method);

        Assert.AreEqual("TResult ComplexGenericMethod<T, TResult>(T input, List<T> items)", signature);
    }

    [TestMethod]
    public void FormatMethodSignature_MethodWithArrayParameters_ReturnsCorrectSignature()
    {
        var method = typeof(TestClass).GetMethod(nameof(TestClass.MethodWithArrays));
        var signature = CSharpTypeNameHelper.FormatMethodSignature(method);

        Assert.AreEqual("string[] MethodWithArrays(int[] numbers, string[] texts)", signature);
    }

    [TestMethod]
    public void FormatMethodSignature_MethodWithComplexGenericParameters_ReturnsCorrectSignature()
    {
        var method = typeof(TestClass).GetMethod(nameof(TestClass.MethodWithComplexGenerics));
        var signature = CSharpTypeNameHelper.FormatMethodSignature(method);

        Assert.AreEqual("List<int> MethodWithComplexGenerics(Dictionary<string, List<int>> data)", signature);
    }

    #endregion

    #region Helper Methods and Test Classes

    private static T GenericMethod<T>(T value)
    {
        return value;
    }

    private static TValue ComplexGenericMethod<TKey, TValue>(TKey key, TValue value)
    {
        return value;
    }

    public class TestClass
    {
        public void SimpleMethod()
        {
        }

        public int MethodWithParameters(int x, string y)
        {
            return x;
        }

        public int? MethodWithNullableParameters(int? value)
        {
            return value;
        }

        public T GenericMethod<T>(T value)
        {
            return value;
        }

        public TResult ComplexGenericMethod<T, TResult>(T input, List<T> items)
        {
            return default;
        }

        public string[] MethodWithArrays(int[] numbers, string[] texts)
        {
            return texts;
        }

        public List<int> MethodWithComplexGenerics(Dictionary<string, List<int>> data)
        {
            return null;
        }
    }

    #endregion
}