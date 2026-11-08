using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Runtime;
using Musoq.Evaluator.Visitors;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests.Runtime;

[TestClass]
public sealed class TypeHintRuntimeCacheTests
{
    [TestMethod]
    [DoNotParallelize]
    public void TypeHintAttributeCache_ShouldUseWeakBoundedOwnershipAndSupportClear()
    {
        WeakTypeRuntimeCache<DynamicObjectPropertyTypeHintAttribute[]> cache =
            BuildMetadataAndInferTypesVisitorUtilities.TypeHintAttributeCache;

        var first = BuildMetadataAndInferTypesVisitorUtilities.GetCachedTypeHintAttributes(typeof(HintedEntity));
        var second = BuildMetadataAndInferTypesVisitorUtilities.GetCachedTypeHintAttributes(typeof(HintedEntity));

        Assert.AreSame(first, second);
        Assert.HasCount(1, first);

        cache.Clear();
        Assert.AreEqual(0, cache.Count);
    }

    [DynamicObjectPropertyTypeHint("Value", typeof(int))]
    private sealed class HintedEntity;
}
