using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class ConstantFoldingTests
{
    #region Non-foldable (passthrough) expressions

    [TestMethod]
    public void WhenColumnInArithmetic_ShouldNotFoldButStillWork()
    {
        var source = new[] { new FoldEntity { Name = "a", Value = 10 } };

        var vm = CreateAndRunVirtualMachine("select Value + 5 from #schema.first()", source);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(15L, Convert.ToInt64(table[0][0]));
    }

    [TestMethod]
    public void WhenColumnInStringConcat_ShouldNotFoldButStillWork()
    {
        var source = new[] { new FoldEntity { Name = "world", Value = 1 } };

        var vm = CreateAndRunVirtualMachine("select 'hello ' + Name from #schema.first()", source);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("hello world", table[0][0]);
    }

    [TestMethod]
    public void WhenPartiallyConstant_ShouldFoldConstantPartOnly()
    {
        var source = new[] { new FoldEntity { Name = "a", Value = 10 } };

        var vm = CreateAndRunVirtualMachine("select (2 + 3) + Value from #schema.first()", source);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(15L, Convert.ToInt64(table[0][0]));
    }

    [TestMethod]
    public void WhenBooleanMixedWithColumn_ShouldNotFoldButStillWork()
    {
        var source = new[]
        {
            new FoldEntity { Name = "a", Value = 10 },
            new FoldEntity { Name = "b", Value = 0 }
        };

        var vm = CreateAndRunVirtualMachine(
            "select Name from #schema.first() where true and Value > 5", source);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("a", table[0][0]);
    }

    #endregion

    #region Mixed expression types

    [TestMethod]
    public void WhenConstantInSelect_ShouldFoldArithmetic()
    {
        var vm = CreateAndRunVirtualMachine(
            "select 100 + 200 from #schema.first() where 1 + 1 > 0", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(300L, Convert.ToInt64(table[0][0]));
    }

    [TestMethod]
    public void WhenConstantInWhere_ShouldFilterCorrectly()
    {
        var vm = CreateAndRunVirtualMachine(
            "select Name from #schema.first() where 1 + 1 > 0", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("a", table[0][0]);
    }

    [TestMethod]
    public void WhenHexLiteralsInArithmetic_ShouldFold()
    {
        var vm = CreateAndRunVirtualMachine("select 0xFF + 0x01 from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(256L, Convert.ToInt64(table[0][0]));
    }

    [TestMethod]
    public void WhenBinaryLiteralsInArithmetic_ShouldFold()
    {
        var vm = CreateAndRunVirtualMachine("select 0b1010 + 0b0101 from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(15L, Convert.ToInt64(table[0][0]));
    }

    [TestMethod]
    public void WhenOctalLiteralsInArithmetic_ShouldFold()
    {
        var vm = CreateAndRunVirtualMachine("select 0o10 + 0o7 from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(15L, Convert.ToInt64(table[0][0]));
    }

    [TestMethod]
    public void WhenMultipleRows_ShouldFoldOnceAndApplyToAll()
    {
        var source = new[]
        {
            new FoldEntity { Name = "a", Value = 1 },
            new FoldEntity { Name = "b", Value = 2 },
            new FoldEntity { Name = "c", Value = 3 }
        };

        var vm = CreateAndRunVirtualMachine("select 100 * 3, Name from #schema.first()", source);
        var table = vm.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual(300L, Convert.ToInt64(table[0][0]));
        Assert.AreEqual(300L, Convert.ToInt64(table[1][0]));
        Assert.AreEqual(300L, Convert.ToInt64(table[2][0]));
    }

    #endregion
}
