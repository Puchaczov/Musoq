using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class WindowLoweringRegistryTests
{
    [TestMethod]
    public void WindowKeyArrayRegistry_WhenSignatureIsReused_ShouldReuseVariableAndDisableExtraction()
    {
        var registry = new WindowKeyArrayRegistry();
        var firstVariable = new ExecutionVariable("partitionKeys", typeof(int[]));
        var secondVariable = new ExecutionVariable("otherPartitionKeys", typeof(int[]));
        var shape = new ExecutionWindowKeyShape(ExecutionClrBindingFactory.FromClr(typeof(int)), IsTyped: true);

        var first = registry.GetOrAdd("partition:id", firstVariable, shape, shouldMaterialize: false);
        var second = registry.GetOrAdd("partition:id", secondVariable);

        Assert.AreEqual(firstVariable, first.Variable);
        Assert.IsTrue(first.ShouldExtract);
        Assert.AreEqual(shape, first.Shape);
        Assert.IsFalse(first.ShouldMaterialize);
        Assert.AreEqual(firstVariable, second.Variable);
        Assert.IsFalse(second.ShouldExtract);
        Assert.AreEqual(shape, second.Shape);
        Assert.IsFalse(second.ShouldMaterialize);
    }

    [TestMethod]
    public void WindowKeyArrayRegistry_WhenSignatureDiffers_ShouldCreateSeparateArrays()
    {
        var registry = new WindowKeyArrayRegistry();

        var first = registry.GetOrAdd("partition:id", new ExecutionVariable("idKeys", typeof(int[])));
        var second = registry.GetOrAdd("partition:name", new ExecutionVariable("nameKeys", typeof(string[])));

        Assert.AreEqual("idKeys", first.Variable.Name);
        Assert.IsTrue(first.ShouldExtract);
        Assert.AreEqual("nameKeys", second.Variable.Name);
        Assert.IsTrue(second.ShouldExtract);
    }

    [TestMethod]
    public void WindowPartitionSetRegistry_WhenSignatureIsReused_ShouldReuseVariableAndDisableCreation()
    {
        var registry = new WindowPartitionSetRegistry();
        var firstVariable = new ExecutionVariable("partitions", typeof(object));
        var secondVariable = new ExecutionVariable("otherPartitions", typeof(object));

        var first = registry.GetOrAdd("partition-list:id", firstVariable);
        var second = registry.GetOrAdd("partition-list:id", secondVariable);

        Assert.AreEqual(firstVariable, first.Variable);
        Assert.IsTrue(first.ShouldCreate);
        Assert.IsFalse(first.SortInPlace);
        Assert.AreEqual(firstVariable, second.Variable);
        Assert.IsFalse(second.ShouldCreate);
        Assert.IsFalse(second.SortInPlace);
    }

    [TestMethod]
    public void WindowPartitionSetRegistry_WhenSortedPartitionIsReusedInPlace_ShouldPreserveSortInPlaceFlag()
    {
        var registry = new WindowPartitionSetRegistry();
        var partitions = new ExecutionVariable("partitions", typeof(object));

        var first = registry.GetOrAdd("sorted:id:name", partitions);
        var reused = registry.GetOrAdd(
            "sorted:id:name",
            new ExecutionVariable("sortedPartitions", typeof(object)),
            sortInPlace: true);

        Assert.AreEqual(partitions, first.Variable);
        Assert.IsTrue(first.ShouldCreate);
        Assert.IsFalse(first.SortInPlace);
        Assert.AreEqual(partitions, reused.Variable);
        Assert.IsFalse(reused.ShouldCreate);
        Assert.IsTrue(reused.SortInPlace);
    }
}
