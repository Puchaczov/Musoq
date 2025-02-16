using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins.Attributes;
using Musoq.Schema.Managers;

namespace Musoq.Schema.Tests;

[TestClass]
public class MethodsMetadataEntityTypeInjectionTests
{
    private interface IBaseEntity { }
    private interface ISpecificEntity : IBaseEntity { }
    private class BaseEntity : IBaseEntity { }
    private class SpecificEntity : BaseEntity, ISpecificEntity { }
    private class OtherEntity : IBaseEntity { }

    private class TestClass
    {
        public void Method1(
            [InjectSpecificSource(typeof(IBaseEntity))] IBaseEntity entity,
            int param)
        { }

        public void Method2(
            [InjectSpecificSource(typeof(ISpecificEntity))] ISpecificEntity entity,
            int param)
        { }

        public void GroupMethod(
            [InjectGroup] object groupContext,
            int param)
        { }

        public void StatsMethod(
            [InjectQueryStats] object stats,
            int param)
        { }

        public void MultipleInjection(
            [InjectSpecificSource(typeof(IBaseEntity))] IBaseEntity entity,
            [InjectGroup] object groupContext,
            int param)
        { }

        public void OptionalWithInjection(
            [InjectSpecificSource(typeof(IBaseEntity))] IBaseEntity entity,
            int param = 42)
        { }
    }

    private MethodsMetadata _methodsMetadata;

    [TestInitialize]
    public void Initialize()
    {
        _methodsMetadata = new TestMethodsMetadata();
    }

    [TestMethod]
    public void TryGetMethod_BasicInjection_MatchingEntityType()
    {
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("Method1", [typeof(int)], typeof(BaseEntity), out _),
            "Should resolve with BaseEntity"
        );

        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("Method1", [typeof(int)], typeof(SpecificEntity), out _),
            "Should resolve with SpecificEntity"
        );
    }

    [TestMethod]
    public void TryGetMethod_BasicInjection_NonMatchingEntityType()
    {
        Assert.IsFalse(
            _methodsMetadata.TryGetMethod("Method1", [typeof(int)], typeof(string), out _),
            "Should not resolve with String type"
        );
    }

    [TestMethod]
    public void TryGetMethod_SpecificInjection_MatchingEntityType()
    {
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("Method2", [typeof(int)], typeof(SpecificEntity), out _),
            "Should resolve with SpecificEntity"
        );
    }

    [TestMethod]
    public void TryGetMethod_SpecificInjection_NonMatchingEntityType()
    {
        Assert.IsFalse(
            _methodsMetadata.TryGetMethod("Method2", [typeof(int)], typeof(BaseEntity), out _),
            "Should not resolve with BaseEntity"
        );
        
        Assert.IsFalse(
            _methodsMetadata.TryGetMethod("Method2", [typeof(int)], typeof(OtherEntity), out _),
            "Should not resolve with OtherEntity"
        );
    }

    [TestMethod]
    public void TryGetMethod_GroupInjection()
    {
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("GroupMethod", [typeof(int)], typeof(BaseEntity), out _),
            "Should resolve with any entity type"
        );
        
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("GroupMethod", [typeof(int)], typeof(string), out _),
            "Should resolve with string type"
        );
    }

    [TestMethod]
    public void TryGetMethod_StatsInjection()
    {
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("StatsMethod", [typeof(int)], typeof(BaseEntity), out _),
            "Should resolve with any entity type"
        );
        
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("StatsMethod", [typeof(int)], typeof(string), out _),
            "Should resolve with string type"
        );
    }

    [TestMethod]
    public void TryGetMethod_MultipleInjection()
    {
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("MultipleInjection", [typeof(int)], typeof(BaseEntity), out _),
            "Should resolve with base entity"
        );
        
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("MultipleInjection", [typeof(int)], typeof(SpecificEntity), out _),
            "Should resolve with specific entity"
        );
        
        Assert.IsFalse(
            _methodsMetadata.TryGetMethod("MultipleInjection", [typeof(int)], typeof(string), out _),
            "Should not resolve with non-matching type"
        );
    }

    [TestMethod]
    public void TryGetMethod_OptionalWithInjection()
    {
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("OptionalWithInjection", [], typeof(BaseEntity), out _),
            "Should resolve with no parameters"
        );
        
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("OptionalWithInjection", [typeof(int)], typeof(BaseEntity), out _),
            "Should resolve with parameter"
        );
        
        Assert.IsFalse(
            _methodsMetadata.TryGetMethod("OptionalWithInjection", [], typeof(string), out _),
            "Should not resolve with non-matching type"
        );
    }

    private class TestMethodsMetadata : MethodsMetadata
    {
        public TestMethodsMetadata()
        {
            var testClass = typeof(TestClass);
            foreach (var method in testClass.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                RegisterMethod(method);
            }
        }

        private new void RegisterMethod(MethodInfo methodInfo)
        {
            base.RegisterMethod(methodInfo);
        }
    }
}