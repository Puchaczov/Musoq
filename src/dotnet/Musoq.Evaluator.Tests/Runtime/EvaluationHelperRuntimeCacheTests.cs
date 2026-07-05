using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Runtime;

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

        Assert.AreEqual(RuntimeCacheOptions.DynamicAccessorCacheSize, Count(cache));
    }

    [TestMethod]
    public void XmlDocumentationCache_ShouldUseBoundedRuntimeCache()
    {
        var cache = PrivateStaticCache("XmlDocCache");

        StringAssert.Contains(cache.GetType().FullName, nameof(BoundedRuntimeCache<object, object>));
    }

    private static object PrivateStaticCache(string fieldName)
    {
        var field = typeof(EvaluationHelper).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)
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
}
