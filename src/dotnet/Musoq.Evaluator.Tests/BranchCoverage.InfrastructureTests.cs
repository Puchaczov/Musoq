using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.TemporarySchemas;

namespace Musoq.Evaluator.Tests;

public partial class BranchCoverageImprovementTests
{
    #region SafeArrayAccess Branch Coverage

    [TestMethod]
    public void SafeArrayAccess_GetArrayElement_NullArray_ShouldReturnDefault()
    {
        var result = Helpers.SafeArrayAccess.GetArrayElement<int>(null, 0);

        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetArrayElement_EmptyArray_ShouldReturnDefault()
    {
        var result = Helpers.SafeArrayAccess.GetArrayElement(Array.Empty<int>(), 0);

        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetArrayElement_NegativeIndex_ShouldWrapAround()
    {
        var arr = new[] { 10, 20, 30 };

        Assert.AreEqual(30, Helpers.SafeArrayAccess.GetArrayElement(arr, -1));
        Assert.AreEqual(20, Helpers.SafeArrayAccess.GetArrayElement(arr, -2));
        Assert.AreEqual(10, Helpers.SafeArrayAccess.GetArrayElement(arr, -3));
    }

    [TestMethod]
    public void SafeArrayAccess_GetArrayElement_IndexBeyondLength_ShouldReturnDefault()
    {
        var arr = new[] { 10, 20 };

        Assert.AreEqual(0, Helpers.SafeArrayAccess.GetArrayElement(arr, 5));
    }

    [TestMethod]
    public void SafeArrayAccess_GetArrayElement_ValidIndex_ShouldReturnElement()
    {
        var arr = new[] { 10, 20, 30 };

        Assert.AreEqual(20, Helpers.SafeArrayAccess.GetArrayElement(arr, 1));
    }

    [TestMethod]
    public void SafeArrayAccess_GetStringCharacter_NullString_ShouldReturnNullChar()
    {
        Assert.AreEqual('\0', Helpers.SafeArrayAccess.GetStringCharacter(null, 0));
    }

    [TestMethod]
    public void SafeArrayAccess_GetStringCharacter_EmptyString_ShouldReturnNullChar()
    {
        Assert.AreEqual('\0', Helpers.SafeArrayAccess.GetStringCharacter(string.Empty, 0));
    }

    [TestMethod]
    public void SafeArrayAccess_GetStringCharacter_NegativeIndex_ShouldWrapAround()
    {
        Assert.AreEqual('c', Helpers.SafeArrayAccess.GetStringCharacter("abc", -1));
    }

    [TestMethod]
    public void SafeArrayAccess_GetStringCharacter_IndexBeyondLength_ShouldReturnNullChar()
    {
        Assert.AreEqual('\0', Helpers.SafeArrayAccess.GetStringCharacter("abc", 10));
    }

    [TestMethod]
    public void SafeArrayAccess_GetStringCharacter_ValidIndex_ShouldReturnChar()
    {
        Assert.AreEqual('b', Helpers.SafeArrayAccess.GetStringCharacter("abc", 1));
    }

    [TestMethod]
    public void SafeArrayAccess_GetDictionaryValue_NullDictionary_ShouldReturnDefault()
    {
        var result = Helpers.SafeArrayAccess.GetDictionaryValue<string, int>(null, "key");

        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetDictionaryValue_NullKey_ShouldReturnDefault()
    {
        var dict = new Dictionary<string, int> { ["a"] = 1 };

        var result = Helpers.SafeArrayAccess.GetDictionaryValue<string, int>(dict, null!);

        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetDictionaryValue_MissingKey_ShouldReturnDefault()
    {
        var dict = new Dictionary<string, int> { ["a"] = 1 };

        var result = Helpers.SafeArrayAccess.GetDictionaryValue(dict, "missing");

        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetDictionaryValue_ExistingKey_ShouldReturnValue()
    {
        var dict = new Dictionary<string, int> { ["a"] = 42 };

        var result = Helpers.SafeArrayAccess.GetDictionaryValue(dict, "a");

        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetListElement_NullList_ShouldReturnDefault()
    {
        var result = Helpers.SafeArrayAccess.GetListElement<int>(null, 0);

        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetListElement_EmptyList_ShouldReturnDefault()
    {
        var result = Helpers.SafeArrayAccess.GetListElement(new List<int>(), 0);

        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetListElement_NegativeIndex_ShouldWrapAround()
    {
        var list = new List<int> { 10, 20, 30 };

        Assert.AreEqual(30, Helpers.SafeArrayAccess.GetListElement(list, -1));
    }

    [TestMethod]
    public void SafeArrayAccess_GetListElement_IndexBeyondCount_ShouldReturnDefault()
    {
        var list = new List<int> { 10, 20 };

        Assert.AreEqual(0, Helpers.SafeArrayAccess.GetListElement(list, 5));
    }

    [TestMethod]
    public void SafeArrayAccess_GetListElement_ValidIndex_ShouldReturnElement()
    {
        var list = new List<int> { 10, 20, 30 };

        Assert.AreEqual(20, Helpers.SafeArrayAccess.GetListElement(list, 1));
    }

    [TestMethod]
    public void SafeArrayAccess_TypedIndexedAccess_NullObject_ShouldReturnDefault()
    {
        var result = Helpers.SafeArrayAccess.GetTypedListElement<int[], int>(null, 0);

        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void SafeArrayAccess_TypedIndexedAccess_NullIndex_ShouldReturnDefault()
    {
        var result = Helpers.SafeArrayAccess.GetTypedListElement<int[], int>(new[] { 1, 2 }, null);

        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void SafeArrayAccess_TypedIndexedAccess_String_ShouldReturnChar()
    {
        var result = Helpers.SafeArrayAccess.GetTypedStringCharacter("abc", 1);

        Assert.AreEqual('b', result);
    }

    [TestMethod]
    public void SafeArrayAccess_TypedIndexedAccess_Array_ShouldReturnElement()
    {
        var result = Helpers.SafeArrayAccess.GetTypedListElement<int[], int>(new[] { 10, 20, 30 }, 1);

        Assert.AreEqual(20, result);
    }

    [TestMethod]
    public void SafeArrayAccess_TypedIndexedAccess_Array_NegativeIndex_ShouldWrapAround()
    {
        var result = Helpers.SafeArrayAccess.GetTypedListElement<int[], int>(new[] { 10, 20, 30 }, -1);

        Assert.AreEqual(30, result);
    }

    [TestMethod]
    public void SafeArrayAccess_TypedIndexedAccess_Array_OutOfBounds_ShouldReturnDefault()
    {
        var result = Helpers.SafeArrayAccess.GetTypedListElement<int[], int>(new[] { 10 }, 5);

        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void SafeArrayAccess_TypedIndexedAccess_EmptyArray_ShouldReturnDefault()
    {
        var result = Helpers.SafeArrayAccess.GetTypedListElement<int[], int>(Array.Empty<int>(), 0);

        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void SafeArrayAccess_TypedIndexedAccess_DictionaryByStringKey_ShouldReturnValue()
    {
        var dict = new Dictionary<string, int> { ["hello"] = 42 };

        var result = Helpers.SafeArrayAccess.GetTypedDictionaryValue<Dictionary<string, int>, string, int>(dict, "hello");

        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public void SafeArrayAccess_TypedIndexedAccess_DictionaryMissingKey_ShouldReturnDefault()
    {
        var dict = new Dictionary<string, int> { ["hello"] = 42 };

        var result = Helpers.SafeArrayAccess.GetTypedDictionaryValue<Dictionary<string, int>, string, int>(dict, "missing");

        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void SafeArrayAccess_TypedIndexedAccess_NullableType_ShouldReturnNull()
    {
        var result = Helpers.SafeArrayAccess.GetTypedListElement<int?[], int?>(null, 0);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void SafeArrayAccess_TypedIndexedAccess_ReferenceType_ShouldReturnNull()
    {
        var result = Helpers.SafeArrayAccess.GetTypedListElement<string[], string>(null, 0);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void SafeArrayAccess_TypedIndexedAccess_NullElementType_ShouldReturnNull()
    {
        var result = Helpers.SafeArrayAccess.GetTypedListElement<string[], string>(null, 0);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void SafeArrayAccess_TypedIndexedAccess_ListWithIndexer_ShouldReturnElement()
    {
        var list = new List<string> { "a", "b", "c" };

        var result = Helpers.SafeArrayAccess.GetTypedListElement<List<string>, string>(list, 1);

        Assert.AreEqual("b", result);
    }

    [TestMethod]
    public void SafeArrayAccess_TypedIndexedAccess_ListWithOutOfBoundsIndexer_ShouldReturnDefault()
    {
        var list = new List<string> { "a" };

        var result = Helpers.SafeArrayAccess.GetTypedListElement<List<string>, string>(list, 99);

        Assert.IsNull(result);
    }

    #endregion

    #region ExpandoObjectPropertyInfo Branch Coverage

    [TestMethod]
    public void ExpandoObjectPropertyInfo_Properties_ShouldReturnExpectedValues()
    {
        var propInfo = new ExpandoObjectPropertyInfo("TestProp", typeof(string));

        Assert.AreEqual("TestProp", propInfo.Name);
        Assert.AreEqual(typeof(string), propInfo.PropertyType);
        Assert.AreEqual(typeof(System.Dynamic.ExpandoObject), propInfo.DeclaringType);
        Assert.AreEqual(typeof(System.Dynamic.ExpandoObject), propInfo.ReflectedType);
        Assert.IsTrue(propInfo.CanRead);
        Assert.IsFalse(propInfo.CanWrite);
        Assert.AreEqual(System.Reflection.PropertyAttributes.None, propInfo.Attributes);
    }

    [TestMethod]
    public void ExpandoObjectPropertyInfo_GetCustomAttributes_Inherit_ShouldReturnEmpty()
    {
        var propInfo = new ExpandoObjectPropertyInfo("TestProp", typeof(int));

        Assert.IsEmpty(propInfo.GetCustomAttributes(false));
    }

    [TestMethod]
    public void ExpandoObjectPropertyInfo_GetCustomAttributes_WithType_ShouldReturnEmpty()
    {
        var propInfo = new ExpandoObjectPropertyInfo("TestProp", typeof(int));

        Assert.IsEmpty(propInfo.GetCustomAttributes(typeof(object), false));
    }

    [TestMethod]
    public void ExpandoObjectPropertyInfo_IsDefined_ShouldReturnFalse()
    {
        var propInfo = new ExpandoObjectPropertyInfo("TestProp", typeof(int));

        Assert.IsFalse(propInfo.IsDefined(typeof(object), false));
    }

    [TestMethod]
    public void ExpandoObjectPropertyInfo_GetAccessors_ShouldReturnEmpty()
    {
        var propInfo = new ExpandoObjectPropertyInfo("TestProp", typeof(int));

        Assert.IsEmpty(propInfo.GetAccessors(false));
    }

    [TestMethod]
    public void ExpandoObjectPropertyInfo_GetGetMethod_ShouldReturnNullSyntheticAccessor()
    {
        var propInfo = new ExpandoObjectPropertyInfo("TestProp", typeof(int));

        Assert.IsNull(propInfo.GetGetMethod(false));
    }

    [TestMethod]
    public void ExpandoObjectPropertyInfo_GetSetMethod_ShouldReturnNullSyntheticAccessor()
    {
        var propInfo = new ExpandoObjectPropertyInfo("TestProp", typeof(int));

        Assert.IsNull(propInfo.GetSetMethod(false));
    }

    [TestMethod]
    public void ExpandoObjectPropertyInfo_GetIndexParameters_ShouldReturnEmpty()
    {
        var propInfo = new ExpandoObjectPropertyInfo("TestProp", typeof(int));

        Assert.IsEmpty(propInfo.GetIndexParameters());
    }

    [TestMethod]
    public void ExpandoObjectPropertyInfo_GetValue_ShouldReadDictionaryBackedValue()
    {
        var propInfo = new ExpandoObjectPropertyInfo("TestProp", typeof(int));
        var row = new System.Dynamic.ExpandoObject();
        ((IDictionary<string, object?>)row)["TestProp"] = 42;

        Assert.AreEqual(42, propInfo.GetValue(row, System.Reflection.BindingFlags.Default, null, null, null));
    }

    [TestMethod]
    public void ExpandoObjectPropertyInfo_SetValue_ShouldRejectReadOnlyMutation()
    {
        var propInfo = new ExpandoObjectPropertyInfo("TestProp", typeof(int));

        Assert.Throws<NotSupportedException>(() =>
            propInfo.SetValue(null, null, System.Reflection.BindingFlags.Default, null, null, null));
    }

    #endregion

    #region NullLogger Branch Coverage

    [TestMethod]
    public void NullLogger_BeginScope_ShouldReturnNull()
    {
        var logger = new NullLogger<object>();

        var scope = logger.BeginScope("state");

        Assert.IsNull(scope);
    }

    [TestMethod]
    public void NullLogger_IsEnabled_ShouldReturnFalse()
    {
        var logger = new NullLogger<object>();

        Assert.IsFalse(logger.IsEnabled(LogLevel.Trace));
        Assert.IsFalse(logger.IsEnabled(LogLevel.Debug));
        Assert.IsFalse(logger.IsEnabled(LogLevel.Information));
        Assert.IsFalse(logger.IsEnabled(LogLevel.Warning));
        Assert.IsFalse(logger.IsEnabled(LogLevel.Error));
        Assert.IsFalse(logger.IsEnabled(LogLevel.Critical));
    }

    [TestMethod]
    public void NullLogger_Log_ShouldNotThrow()
    {
        var logger = new NullLogger<object>();

        logger.Log(LogLevel.Error, new EventId(1), "state", null, (s, _) => s.ToString());
    }

    #endregion

    #region TransitionSchemaProvider Branch Coverage

    [TestMethod]
    public void TransitionSchemaProvider_GetSchema_WhenTransientExists_ShouldReturnTransient()
    {
        var innerProvider = new TestSchemaProvider();
        var provider = new TransitionSchemaProvider(innerProvider);
        var transientSchema = new TestSchema("transient");

        provider.AddTransitionSchema(transientSchema);

        var result = provider.GetSchema("transient");

        Assert.AreEqual(transientSchema, result);
    }

    [TestMethod]
    public void TransitionSchemaProvider_GetSchema_WhenTransientMissing_ShouldFallbackToInner()
    {
        var innerSchema = new TestSchema("test");
        var innerProvider = new TestSchemaProvider(innerSchema);
        var provider = new TransitionSchemaProvider(innerProvider);

        var result = provider.GetSchema("test");

        Assert.AreEqual(innerSchema, result);
    }

    #endregion

}
