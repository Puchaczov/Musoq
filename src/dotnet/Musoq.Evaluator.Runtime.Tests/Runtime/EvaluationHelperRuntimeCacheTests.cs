using System;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Runtime;
using Musoq.Evaluator.Tests.Architecture;
using Musoq.Evaluator.Visitors;

namespace Musoq.Evaluator.Tests.Runtime;

[TestClass]
[DoNotParallelize]
public sealed class EvaluationHelperRuntimeCacheTests
{
    [TestMethod]
    public void DynamicNestedValueAccessors_ShouldNotExist()
    {
        Assert.IsNull(CompilerContractSymbols.For(typeof(EvaluationHelper)).GetMethod("GetNestedValueAccessor"));
        Assert.IsNull(CompilerContractSymbols.For(typeof(EvaluationHelper)).GetMethod("GetNestedValue"));
        Assert.IsNull(
            CompilerContractSymbols
                .For(typeof(EvaluationHelper))
                .GetField("NestedValueAccessors", BindingFlags.NonPublic | BindingFlags.Static)
        );
    }

    [TestMethod]
    public void XmlDocumentationCache_ShouldUseBoundedRuntimeCache()
    {
        var cache = CompilerContractSymbols
            .For(typeof(EvaluationHelper))
            .GetField("XmlDocCache", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(cache);
        Assert.IsTrue(
            SymbolEqualityComparer.Default.Equals(
                CompilerContractSymbols.For(typeof(BoundedRuntimeCache<,>)),
                cache.Type.OriginalDefinition
            )
        );
    }

    [TestMethod]
    public void RemainingTypeKeyedCaches_ShouldUseWeakBoundedRuntimeCache()
    {
        var castable = CompilerContractSymbols
            .For(typeof(EvaluationHelper))
            .GetField("CastableTypeCache", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(castable);
        Assert.IsTrue(
            SymbolEqualityComparer.Default.Equals(
                CompilerContractSymbols.For(typeof(WeakTypeRuntimeCache<>)),
                castable.Type.OriginalDefinition
            )
        );
        foreach (var name in new[] { "HasIndexerCache", "IsIndexableCache" })
        {
            var cache = CompilerContractSymbols
                .For(typeof(BuildMetadataAndInferTypesVisitorUtilities))
                .GetField(name, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(cache);
            Assert.IsTrue(
                SymbolEqualityComparer.Default.Equals(
                    CompilerContractSymbols.For(typeof(WeakTypeRuntimeCache<>)),
                    cache.Type.OriginalDefinition
                )
            );
        }
    }

    [TestMethod]
    public void TypeInspectionCaches_ShouldSupportExplicitClear()
    {
        var indexer = new CountingType();
        var indexable = new CountingType();
        BuildMetadataAndInferTypesVisitorUtilities.ClearTypeInspectionCaches();
        try
        {
            Assert.IsTrue(BuildMetadataAndInferTypesVisitorUtilities.HasIndexer(indexer));
            Assert.IsTrue(BuildMetadataAndInferTypesVisitorUtilities.HasIndexer(indexer));
            Assert.IsTrue(BuildMetadataAndInferTypesVisitorUtilities.IsIndexableType(indexable));
            Assert.IsTrue(BuildMetadataAndInferTypesVisitorUtilities.IsIndexableType(indexable));
            Assert.AreEqual((1, 1), (indexer.PropertyReads, indexable.PropertyReads));
            BuildMetadataAndInferTypesVisitorUtilities.ClearTypeInspectionCaches();
            Assert.IsTrue(BuildMetadataAndInferTypesVisitorUtilities.HasIndexer(indexer));
            Assert.IsTrue(BuildMetadataAndInferTypesVisitorUtilities.IsIndexableType(indexable));
            Assert.AreEqual((2, 2), (indexer.PropertyReads, indexable.PropertyReads));
        }
        finally
        {
            BuildMetadataAndInferTypesVisitorUtilities.ClearTypeInspectionCaches();
        }
    }

    // Type is the API input under test. This typed delegator observes public metadata requests;
    // it does not inspect production fields or invoke private members.
    private sealed class CountingType() : TypeDelegator(typeof(CacheEntity))
    {
        internal int PropertyReads { get; private set; }

        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            PropertyReads++;
            return base.GetProperties(bindingAttr);
        }
    }

    private sealed class CacheEntity
    {
        public int this[int index] => index;
    }
}
