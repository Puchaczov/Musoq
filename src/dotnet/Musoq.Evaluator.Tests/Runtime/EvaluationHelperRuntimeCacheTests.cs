using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Runtime;
using Musoq.Evaluator.Visitors;

namespace Musoq.Evaluator.Tests.Runtime;

[TestClass]
[DoNotParallelize]
public sealed class EvaluationHelperRuntimeCacheTests
{
    [TestMethod]
    public void DynamicNestedValueAccessorCache_ShouldStayBounded()
    {
        var cache = PrivateStaticCache("NestedValueAccessors");
        Clear(cache);

        for (var i = 0; i < RuntimeCacheOptions.DynamicAccessorCacheSize + 10; i++)
            EvaluationHelper.GetNestedValueAccessor(typeof(DynamicAccessorEntity), $"Missing{i}");

        Assert.AreEqual(1, Count(cache));
        Assert.AreEqual(
            RuntimeCacheOptions.DynamicAccessorCacheSize,
            EvaluationHelper.GetNestedValueAccessorCacheCount(typeof(DynamicAccessorEntity)));
    }

    [TestMethod]
    public void XmlDocumentationCache_ShouldUseBoundedRuntimeCache()
    {
        var cache = PrivateStaticCache("XmlDocCache");

        StringAssert.Contains(cache.GetType().FullName, nameof(BoundedRuntimeCache<object, object>));
    }

    [TestMethod]
    public void RemainingTypeKeyedCaches_ShouldUseWeakBoundedRuntimeCache()
    {
        foreach (var fieldName in new[]
                 {
                     "CastableTypeCache",
                     "ObjectChunkAdapters"
                 })
        {
            var cache = PrivateStaticCache(fieldName);
            Assert.AreEqual(
                typeof(WeakTypeRuntimeCache<>),
                cache.GetType().GetGenericTypeDefinition(),
                $"EvaluationHelper.{fieldName} must use weak bounded ownership.");
        }

        foreach (var fieldName in new[] { "HasIndexerCache", "IsIndexableCache" })
        {
            var field = typeof(BuildMetadataAndInferTypesVisitorUtilities).GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(field, $"{fieldName} private static cache field was not found.");
            Assert.AreEqual(typeof(WeakTypeRuntimeCache<>), field!.FieldType.GetGenericTypeDefinition());
        }
    }

    [TestMethod]
    public void TypeInspectionCaches_ShouldSupportExplicitClear()
    {
        BuildMetadataAndInferTypesVisitorUtilities.ClearTypeInspectionCaches();

        Assert.IsTrue(BuildMetadataAndInferTypesVisitorUtilities.HasIndexer(typeof(CacheEntity)));
        Assert.IsTrue(BuildMetadataAndInferTypesVisitorUtilities.IsIndexableType(typeof(CacheEntity)));

        var hasIndexer = PrivateStaticCache(typeof(BuildMetadataAndInferTypesVisitorUtilities), "HasIndexerCache");
        var isIndexable = PrivateStaticCache(typeof(BuildMetadataAndInferTypesVisitorUtilities), "IsIndexableCache");
        Assert.AreEqual(1, Count(hasIndexer));
        Assert.AreEqual(1, Count(isIndexable));

        BuildMetadataAndInferTypesVisitorUtilities.ClearTypeInspectionCaches();

        Assert.AreEqual(0, Count(hasIndexer));
        Assert.AreEqual(0, Count(isIndexable));
    }

    private static object PrivateStaticCache(string fieldName)
    {
        return PrivateStaticCache(typeof(EvaluationHelper), fieldName);
    }

    private static object PrivateStaticCache(Type owner, string fieldName)
    {
        var field = owner.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)
                    ?? throw new AssertFailedException($"EvaluationHelper.{fieldName} private static cache field was not found.");
        return field.GetValue(null)
               ?? throw new AssertFailedException($"EvaluationHelper.{fieldName} private static cache field is null.");
    }

    private static void Clear(object cache)
    {
        var clearMethod = cache.GetType().GetMethod(nameof(BoundedRuntimeCache<object, object>.Clear), BindingFlags.Public | BindingFlags.Instance)
                          ?? throw new AssertFailedException($"{cache.GetType().FullName} does not expose Clear.");
        clearMethod.Invoke(cache, []);
    }

    private static int Count(object cache)
    {
        var countProperty = cache.GetType().GetProperty(nameof(BoundedRuntimeCache<object, object>.Count), BindingFlags.Public | BindingFlags.Instance)
                            ?? throw new AssertFailedException($"{cache.GetType().FullName} does not expose Count.");
        return countProperty.GetValue(cache) is int count
            ? count
            : throw new AssertFailedException($"{cache.GetType().FullName}.Count did not return an int.");
    }

    private sealed class DynamicAccessorEntity;

    private sealed class CacheEntity
    {
        public int this[int index] => index;
    }
}
