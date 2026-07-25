using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Runtime;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class InMemorySourceShapeCacheTests
{
    [TestMethod]
    public void SourceShapeCache_ShouldUseWeakBoundedTypeOwnership()
    {
        var field = typeof(InMemorySourceShape).GetField("Shapes", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.IsNotNull(field);
        Assert.AreEqual(typeof(WeakTypeRuntimeCache<>), field!.FieldType.GetGenericTypeDefinition());
    }

    [TestMethod]
    public void SourceShapeCache_ShouldReuseShapeForTheSameType()
    {
        var first = InMemorySourceShape.For(typeof(CacheRow));
        var second = InMemorySourceShape.For(typeof(CacheRow));

        Assert.AreSame(first, second);
    }

    private sealed class CacheRow
    {
        public int Value { get; set; }
    }
}
