using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Runtime;
using Musoq.Evaluator.Visitors;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests.Runtime;

[TestClass]
public sealed class TypeHintRuntimeCacheTests
{
    [TestMethod]
    public void TypeHintAttributeCache_ShouldUseWeakBoundedOwnershipAndSupportClear()
    {
        var field = typeof(BuildMetadataAndInferTypesVisitorUtilities).GetField(
                        "TypeHintAttributeCache",
                        BindingFlags.NonPublic | BindingFlags.Static) ??
                    throw new AssertFailedException("The type-hint cache field was not found.");
        var cache = field.GetValue(null) ??
                    throw new AssertFailedException("The type-hint cache was not initialized.");

        StringAssert.Contains(cache.GetType().FullName, nameof(WeakTypeRuntimeCache<object>));

        var first = BuildMetadataAndInferTypesVisitorUtilities.GetCachedTypeHintAttributes(typeof(HintedEntity));
        var second = BuildMetadataAndInferTypesVisitorUtilities.GetCachedTypeHintAttributes(typeof(HintedEntity));

        Assert.AreSame(first, second);
        Assert.HasCount(1, first);

        var clear = cache.GetType().GetMethod(nameof(BoundedRuntimeCache<object, object>.Clear), BindingFlags.Public | BindingFlags.Instance)
                    ?? throw new AssertFailedException("The type-hint cache does not expose Clear.");
        clear.Invoke(cache, []);

        var count = cache.GetType().GetProperty(nameof(BoundedRuntimeCache<object, object>.Count), BindingFlags.Public | BindingFlags.Instance)
                    ?? throw new AssertFailedException("The type-hint cache does not expose Count.");
        Assert.AreEqual(0, count.GetValue(cache));
    }

    [DynamicObjectPropertyTypeHint("Value", typeof(int))]
    private sealed class HintedEntity;
}
