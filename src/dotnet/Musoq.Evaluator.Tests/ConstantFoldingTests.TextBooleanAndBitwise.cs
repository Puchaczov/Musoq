using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class ConstantFoldingTests
{
    #region String concatenation folding

    [TestMethod]
    public void WhenConcatenatingStringLiterals_ShouldFoldToString()
    {
        var vm = CreateAndRunVirtualMachine("select 'hello' + ' ' + 'world' from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("hello world", table[0][0]);
    }

    [TestMethod]
    public void WhenConcatenatingMultipleStrings_ShouldFoldAll()
    {
        var vm = CreateAndRunVirtualMachine("select 'a' + 'b' + 'c' + 'd' from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("abcd", table[0][0]);
    }

    #endregion

    #region Adjacent string constant folding ('text' + variable + 'text' + 'othertext')

    [TestMethod]
    public void WhenAdjacentStringConstantsAfterVariable_ShouldMerge()
    {
        var source = new[] { new FoldEntity { Name = "hello", Value = 1 } };

        var vm = CreateAndRunVirtualMachine(
            "select 'prefix:' + Name + ' - suffix' + 'More' from #schema.first()", source);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("prefix:hello - suffixMore", table[0][0]);
    }

    [TestMethod]
    public void WhenAdjacentConstantsOnBothSides_ShouldMergeOnRight()
    {
        var source = new[] { new FoldEntity { Name = "x", Value = 1 } };

        var vm = CreateAndRunVirtualMachine(
            "select 'aaa' + Name + 'bbb' + 'ccc' from #schema.first()", source);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("aaaxbbbccc", table[0][0]);
    }

    [TestMethod]
    public void WhenThreeAdjacentConstants_ShouldMergeAllOnRight()
    {
        var source = new[] { new FoldEntity { Name = "X", Value = 1 } };

        var vm = CreateAndRunVirtualMachine(
            "select Name + 'a' + 'b' + 'c' from #schema.first()", source);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Xabc", table[0][0]);
    }

    [TestMethod]
    public void WhenVariableSurroundedByConstants_ShouldPreserveCorrectly()
    {
        var source = new[] { new FoldEntity { Name = "World", Value = 1 } };

        var vm = CreateAndRunVirtualMachine(
            "select 'Hello ' + Name + '!' from #schema.first()", source);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Hello World!", table[0][0]);
    }

    [TestMethod]
    public void WhenMultipleVariablesWithConstants_ShouldOnlyFoldAdjacentConstants()
    {
        var source = new[] { new FoldEntity { Name = "A", Value = 42 } };

        var vm = CreateAndRunVirtualMachine(
            "select Name + ':' + ':' + Name from #schema.first()", source);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("A::A", table[0][0]);
    }

    #endregion

    #region Boolean folding

    [TestMethod]
    public void WhenAndWithFalse_ShouldFoldToFalse()
    {
        var vm = CreateAndRunVirtualMachine(
            "select Name from #schema.first() where true and false", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void WhenOrWithTrue_ShouldFoldToTrue()
    {
        var vm = CreateAndRunVirtualMachine(
            "select Name from #schema.first() where false or true", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void WhenAndWithTrue_ShouldFoldToTrue()
    {
        var vm = CreateAndRunVirtualMachine(
            "select Name from #schema.first() where true and true", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void WhenOrWithFalse_ShouldFoldToFalse()
    {
        var vm = CreateAndRunVirtualMachine(
            "select Name from #schema.first() where false or false", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(0, table.Count);
    }

    #endregion

    #region Null propagation

    [TestMethod]
    public void WhenAddingNullToConstant_ShouldFoldToNull()
    {
        var vm = CreateAndRunVirtualMachine("select 10 + null from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.IsNull(table[0][0]);
    }

    [TestMethod]
    public void WhenMultiplyingNullByConstant_ShouldFoldToNull()
    {
        var vm = CreateAndRunVirtualMachine("select null * 5 from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.IsNull(table[0][0]);
    }

    [TestMethod]
    public void WhenDividingNullByConstant_ShouldFoldToNull()
    {
        var vm = CreateAndRunVirtualMachine("select null / 5 from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.IsNull(table[0][0]);
    }

    #endregion

    #region Bitwise folding

    [TestMethod]
    public void WhenBitwiseAndOnConstants_ShouldFold()
    {
        var vm = CreateAndRunVirtualMachine("select 0xFF & 0x0F from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(15L, Convert.ToInt64(table[0][0]));
    }

    [TestMethod]
    public void WhenBitwiseOrOnConstants_ShouldFold()
    {
        var vm = CreateAndRunVirtualMachine("select 0xF0 | 0x0F from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(255L, Convert.ToInt64(table[0][0]));
    }

    [TestMethod]
    public void WhenLeftShiftOnConstants_ShouldFold()
    {
        var vm = CreateAndRunVirtualMachine("select 1 << 3 from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(8L, Convert.ToInt64(table[0][0]));
    }

    [TestMethod]
    public void WhenRightShiftOnConstants_ShouldFold()
    {
        var vm = CreateAndRunVirtualMachine("select 16 >> 2 from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(4L, Convert.ToInt64(table[0][0]));
    }

    #endregion
}
