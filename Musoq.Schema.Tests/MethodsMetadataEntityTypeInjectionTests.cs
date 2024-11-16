using System;
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
        // Basic injection tests
        public void Method1(
            [InjectSpecificSourceAttribute(typeof(IBaseEntity))] IBaseEntity entity,
            int param)
        { }

        public void Method2(
            [InjectSpecificSourceAttribute(typeof(ISpecificEntity))] ISpecificEntity entity,
            int param)
        { }

        // Testing InjectGroupAttribute
        public void GroupMethod(
            [InjectGroupAttribute] object groupContext,
            int param)
        { }

        // Testing InjectQueryStatsAttribute
        public void StatsMethod(
            [InjectQueryStatsAttribute] object stats,
            int param)
        { }

        // Multiple injected parameters
        public void MultipleInjection(
            [InjectSpecificSourceAttribute(typeof(IBaseEntity))] IBaseEntity entity,
            [InjectGroupAttribute] object groupContext,
            int param)
        { }

        // Optional parameters with injection
        public void OptionalWithInjection(
            [InjectSpecificSourceAttribute(typeof(IBaseEntity))] IBaseEntity entity,
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
        // Base entity should match IBaseEntity injection
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("Method1", new[] { typeof(int) }, typeof(BaseEntity), out var method),
            "Should resolve with BaseEntity"
        );

        // Specific entity should match IBaseEntity injection
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("Method1", new[] { typeof(int) }, typeof(SpecificEntity), out method),
            "Should resolve with SpecificEntity"
        );
    }

    [TestMethod]
    public void TryGetMethod_BasicInjection_NonMatchingEntityType()
    {
        // String type should not match IBaseEntity injection
        Assert.IsFalse(
            _methodsMetadata.TryGetMethod("Method1", new[] { typeof(int) }, typeof(string), out _),
            "Should not resolve with String type"
        );
    }

    [TestMethod]
    public void TryGetMethod_SpecificInjection_MatchingEntityType()
    {
        // Specific entity should match ISpecificEntity injection
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("Method2", new[] { typeof(int) }, typeof(SpecificEntity), out _),
            "Should resolve with SpecificEntity"
        );
    }

    [TestMethod]
    public void TryGetMethod_SpecificInjection_NonMatchingEntityType()
    {
        // Base entity should not match ISpecificEntity injection
        Assert.IsFalse(
            _methodsMetadata.TryGetMethod("Method2", new[] { typeof(int) }, typeof(BaseEntity), out _),
            "Should not resolve with BaseEntity"
        );
        
        // Other entity should not match ISpecificEntity injection
        Assert.IsFalse(
            _methodsMetadata.TryGetMethod("Method2", new[] { typeof(int) }, typeof(OtherEntity), out _),
            "Should not resolve with OtherEntity"
        );
    }

    [TestMethod]
    public void TryGetMethod_GroupInjection()
    {
        // Group injection should work with any entity type
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("GroupMethod", new[] { typeof(int) }, typeof(BaseEntity), out _),
            "Should resolve with any entity type"
        );
        
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("GroupMethod", new[] { typeof(int) }, typeof(string), out _),
            "Should resolve with string type"
        );
    }

    [TestMethod]
    public void TryGetMethod_StatsInjection()
    {
        // Stats injection should work with any entity type
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("StatsMethod", new[] { typeof(int) }, typeof(BaseEntity), out _),
            "Should resolve with any entity type"
        );
        
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("StatsMethod", new[] { typeof(int) }, typeof(string), out _),
            "Should resolve with string type"
        );
    }

    [TestMethod]
    public void TryGetMethod_MultipleInjection()
    {
        // Should work with matching entity type for multiple injections
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("MultipleInjection", new[] { typeof(int) }, typeof(BaseEntity), out _),
            "Should resolve with base entity"
        );
        
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("MultipleInjection", new[] { typeof(int) }, typeof(SpecificEntity), out _),
            "Should resolve with specific entity"
        );
        
        Assert.IsFalse(
            _methodsMetadata.TryGetMethod("MultipleInjection", new[] { typeof(int) }, typeof(string), out _),
            "Should not resolve with non-matching type"
        );
    }

    [TestMethod]
    public void TryGetMethod_OptionalWithInjection()
    {
        // Should work with optional parameters
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("OptionalWithInjection", Array.Empty<Type>(), typeof(BaseEntity), out _),
            "Should resolve with no parameters"
        );
        
        Assert.IsTrue(
            _methodsMetadata.TryGetMethod("OptionalWithInjection", new[] { typeof(int) }, typeof(BaseEntity), out _),
            "Should resolve with parameter"
        );
        
        Assert.IsFalse(
            _methodsMetadata.TryGetMethod("OptionalWithInjection", Array.Empty<Type>(), typeof(string), out _),
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