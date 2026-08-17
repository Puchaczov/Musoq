using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Optimization.Execution;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ExecutionBlockRewriteBuilderTests
{
    [TestMethod]
    public void ToBlock_WhenNoChangesWereAdded_ShouldReturnOriginalBlock()
    {
        var block = new ExecutionBlock([Let("value", 1)]);
        var builder = new ExecutionBlockRewriteBuilder(block);

        var result = builder.ToBlock();

        Assert.AreSame(block, result);
    }

    [TestMethod]
    public void ToBlock_WhenNodeIsInsertedBeforeCurrent_ShouldPreserveOrder()
    {
        var prefix = Let("prefix", 1);
        var current = Let("current", 2);
        var inserted = Let("inserted", 3);
        var block = new ExecutionBlock([prefix, current]);
        var builder = new ExecutionBlockRewriteBuilder(block);

        builder.EnsureStartedAt(1);
        builder.Add(inserted);
        builder.Add(current);

        var result = builder.ToBlock();

        CollectionAssert.AreEqual(
            new ExecutionNode[] { prefix, inserted, current },
            result.Nodes.ToArray());
    }

    [TestMethod]
    public void ToBlock_WhenRangeIsAppended_ShouldPreserveRangeOrder()
    {
        var first = Let("first", 1);
        var second = Let("second", 2);
        var block = ExecutionBlock.Empty;
        var builder = new ExecutionBlockRewriteBuilder(block);

        builder.EnsureStartedAt(0);
        builder.AddRange([first, second]);

        var result = builder.ToBlock();

        CollectionAssert.AreEqual(
            new ExecutionNode[] { first, second },
            result.Nodes.ToArray());
    }

    [TestMethod]
    public void EnsureStartedAt_WhenCurrentIndexHasPrefix_ShouldCopyPrefixReferences()
    {
        var firstPrefix = Let("firstPrefix", 1);
        var secondPrefix = Let("secondPrefix", 2);
        var current = Let("current", 3);
        var replacement = Let("replacement", 4);
        var block = new ExecutionBlock([firstPrefix, secondPrefix, current]);
        var builder = new ExecutionBlockRewriteBuilder(block);

        builder.EnsureStartedAt(2);
        builder.Add(replacement);

        var result = builder.ToBlock();

        Assert.AreSame(firstPrefix, result.Nodes[0]);
        Assert.AreSame(secondPrefix, result.Nodes[1]);
        Assert.AreSame(replacement, result.Nodes[2]);
    }

    private static ExecutionLet Let(string name, int value)
    {
        return new ExecutionLet(
            new ExecutionVariable(name, typeof(int)),
            new ExecutionLiteral(value, typeof(int)));
    }
}
