using System.Collections.Generic;
using System.Dynamic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class DynamicEntityBoundaryGuardrailTests
{
    [TestMethod]
    public void DynamicMetaObjectProvider_ShouldBeClassifiedAsDynamicEntity()
    {
        Assert.IsTrue(DynamicEntityBoundary.IsDynamicEntity(typeof(ExpandoObject)));
    }

    [TestMethod]
    public void StringObjectDictionary_ShouldBeClassifiedAsDynamicEntity()
    {
        Assert.IsTrue(DynamicEntityBoundary.IsDynamicEntity(typeof(Dictionary<string, object>)));
    }

    [TestMethod]
    public void DeclaredEntity_ShouldNotBeClassifiedAsDynamicEntity()
    {
        Assert.IsFalse(DynamicEntityBoundary.IsDynamicEntity(typeof(string)));
    }

    [TestMethod]
    public void ExpandoObject_ShouldBeRecognizedAsDynamicMetaObjectProvider()
    {
        Assert.IsTrue(DynamicEntityBoundary.IsDynamicMetaObjectProvider(typeof(ExpandoObject)));
    }

    [TestMethod]
    public void DeclaredType_ShouldNotBeRecognizedAsDynamicMetaObjectProvider()
    {
        Assert.IsFalse(DynamicEntityBoundary.IsDynamicMetaObjectProvider(typeof(string)));
    }

    [TestMethod]
    public void StringObjectDictionary_ShouldBeAssignableToStringObjectDictionary()
    {
        Assert.IsTrue(DynamicEntityBoundary.IsAssignableToStringObjectDictionary(typeof(Dictionary<string, object>)));
    }

    [TestMethod]
    public void StringIntDictionary_ShouldNotBeAssignableToStringObjectDictionary()
    {
        Assert.IsFalse(DynamicEntityBoundary.IsAssignableToStringObjectDictionary(typeof(Dictionary<string, int>)));
    }

    [TestMethod]
    public void StringObjectDictionaryInterface_ShouldBeStringObjectDictionaryContext()
    {
        Assert.IsTrue(DynamicEntityBoundary.IsStringObjectDictionaryContext(typeof(IDictionary<string, object>)));
    }

    [TestMethod]
    public void StringIntDictionaryInterface_ShouldNotBeStringObjectDictionaryContext()
    {
        Assert.IsFalse(DynamicEntityBoundary.IsStringObjectDictionaryContext(typeof(IDictionary<string, int>)));
    }

    [TestMethod]
    public void NonGenericDictionaryResult_ShouldBeClassifiedAsDynamicResultShape()
    {
        Assert.IsTrue(DynamicEntityBoundary.IsDynamicResultShape(typeof(System.Collections.Hashtable), static _ => false));
    }

    [TestMethod]
    public void DeclaredResult_ShouldNotBeClassifiedAsDynamicResultShape()
    {
        Assert.IsFalse(DynamicEntityBoundary.IsDynamicResultShape(typeof(string), static _ => false));
    }

    [TestMethod]
    public void StringObjectDictionaryType_ShouldBeStringObjectDictionaryInterface()
    {
        Assert.AreEqual(typeof(IDictionary<string, object>), DynamicEntityBoundary.StringObjectDictionaryType);
    }

    [TestMethod]
    public void ExpandoType_ShouldBeExpandoObject()
    {
        Assert.AreEqual(typeof(ExpandoObject), DynamicEntityBoundary.ExpandoType);
    }
}
