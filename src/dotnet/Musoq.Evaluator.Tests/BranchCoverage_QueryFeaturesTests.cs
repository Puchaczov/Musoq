using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.TemporarySchemas;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Tokens;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.Api;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;

namespace Musoq.Evaluator.Tests;

public partial class BranchCoverageImprovementTests
{
    #region BaseOperations Set Operations

    [TestMethod]
    public void WhenUnion_WithDuplicates_ShouldRemoveDuplicates()
    {
        var source = new SimpleEntity[] { new() { Name = "a" }, new() { Name = "b" } };

        var vm = CreateAndRunVirtualMachine(
            "select Name from #schema.first() union (Name) select Name from #schema.first()",
            source);
        var result = vm.Run();

        Assert.AreEqual(2, result.Count);
    }

    [TestMethod]
    public void WhenUnionAll_ShouldKeepAllRows()
    {
        var source = new SimpleEntity[] { new() { Name = "a" }, new() { Name = "b" } };

        var vm = CreateAndRunVirtualMachine(
            "select Name from #schema.first() union all (Name) select Name from #schema.first()",
            source);
        var result = vm.Run();

        Assert.AreEqual(4, result.Count);
    }

    [TestMethod]
    public void WhenExcept_ShouldRemoveMatchingRows()
    {
        var source1 = new SimpleEntity[] { new() { Name = "a" }, new() { Name = "b" }, new() { Name = "c" } };
        var source2 = new SimpleEntity[] { new() { Name = "b" } };

        var vm = CreateAndRunVirtualMachine(
            "select Name from #schema.first() except (Name) select Name from #schema.second()",
            source1, source2);
        var result = vm.Run();

        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result.Any(r => (string)r[0] == "a"));
        Assert.IsTrue(result.Any(r => (string)r[0] == "c"));
    }

    [TestMethod]
    public void WhenIntersect_ShouldKeepOnlyCommonRows()
    {
        var source1 = new SimpleEntity[] { new() { Name = "a" }, new() { Name = "b" }, new() { Name = "c" } };
        var source2 = new SimpleEntity[] { new() { Name = "b" }, new() { Name = "c" }, new() { Name = "d" } };

        var vm = CreateAndRunVirtualMachine(
            "select Name from #schema.first() intersect (Name) select Name from #schema.second()",
            source1, source2);
        var result = vm.Run();

        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result.Any(r => (string)r[0] == "b"));
        Assert.IsTrue(result.Any(r => (string)r[0] == "c"));
    }

    #endregion

    #region Constant Folding Additional Branches

    [TestMethod]
    public void WhenFoldingStringConcatenation_ShouldFold()
    {
        var source = new SimpleEntity[] { new() { Name = "x" } };
        var vm = CreateAndRunVirtualMachine("select 'hello' + ' ' + 'world' from #schema.first()", source);
        var result = vm.Run();

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("hello world", result[0][0]);
    }

    [TestMethod]
    public void WhenNegatingConstant_ShouldFold()
    {
        var source = new SimpleEntity[] { new() { Name = "x" } };
        var vm = CreateAndRunVirtualMachine("select -42 from #schema.first()", source);
        var result = vm.Run();

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(-42L, Convert.ToInt64(result[0][0]));
    }

    [TestMethod]
    public void WhenBooleanAndWithConstants_ShouldFold()
    {
        var source = new SimpleEntity[] { new() { Name = "x" } };
        var vm = CreateAndRunVirtualMachine("select 1 from #schema.first() where true and true", source);
        var result = vm.Run();

        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public void WhenBooleanOrWithConstants_ShouldFold()
    {
        var source = new SimpleEntity[] { new() { Name = "x" } };
        var vm = CreateAndRunVirtualMachine("select 1 from #schema.first() where false or true", source);
        var result = vm.Run();

        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public void WhenBitwiseAndWithConstants_ShouldFold()
    {
        var source = new SimpleEntity[] { new() { Name = "x" } };
        var vm = CreateAndRunVirtualMachine("select 0xFF & 0x0F from #schema.first()", source);
        var result = vm.Run();

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(15L, Convert.ToInt64(result[0][0]));
    }

    [TestMethod]
    public void WhenBitwiseOrWithConstants_ShouldFold()
    {
        var source = new SimpleEntity[] { new() { Name = "x" } };
        var vm = CreateAndRunVirtualMachine("select 0xF0 | 0x0F from #schema.first()", source);
        var result = vm.Run();

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(255L, Convert.ToInt64(result[0][0]));
    }

    [TestMethod]
    public void WhenBitwiseXorWithConstants_ShouldFold()
    {
        var source = new SimpleEntity[] { new() { Name = "x" } };
        var vm = CreateAndRunVirtualMachine("select 0xFF ^ 0x0F from #schema.first()", source);
        var result = vm.Run();

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(240L, Convert.ToInt64(result[0][0]));
    }

    [TestMethod]
    public void WhenLeftShiftWithConstants_ShouldFold()
    {
        var source = new SimpleEntity[] { new() { Name = "x" } };
        var vm = CreateAndRunVirtualMachine("select 1 << 3 from #schema.first()", source);
        var result = vm.Run();

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(8L, Convert.ToInt64(result[0][0]));
    }

    [TestMethod]
    public void WhenRightShiftWithConstants_ShouldFold()
    {
        var source = new SimpleEntity[] { new() { Name = "x" } };
        var vm = CreateAndRunVirtualMachine("select 16 >> 2 from #schema.first()", source);
        var result = vm.Run();

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(4L, Convert.ToInt64(result[0][0]));
    }

    #endregion

    #region OrderBY / ThenBy Branch Coverage

    [TestMethod]
    public void WhenOrderBy_WithStringValues_ShouldSortOrdinal()
    {
        var source = new SimpleEntity[]
        {
            new() { Name = "Charlie" },
            new() { Name = "Alice" },
            new() { Name = "Bob" }
        };

        var vm = CreateAndRunVirtualMachine("select Name from #schema.first() order by Name asc", source);
        var result = vm.Run();

        Assert.AreEqual("Alice", result[0][0]);
        Assert.AreEqual("Bob", result[1][0]);
        Assert.AreEqual("Charlie", result[2][0]);
    }

    [TestMethod]
    public void WhenOrderByDescending_WithStringValues_ShouldSortDescending()
    {
        var source = new SimpleEntity[]
        {
            new() { Name = "Alice" },
            new() { Name = "Charlie" },
            new() { Name = "Bob" }
        };

        var vm = CreateAndRunVirtualMachine("select Name from #schema.first() order by Name desc", source);
        var result = vm.Run();

        Assert.AreEqual("Charlie", result[0][0]);
        Assert.AreEqual("Bob", result[1][0]);
        Assert.AreEqual("Alice", result[2][0]);
    }

    [TestMethod]
    public void WhenOrderByThenBy_ShouldSortByMultipleColumns()
    {
        var source = new SimpleEntity[]
        {
            new() { Name = "B", City = "Y" },
            new() { Name = "A", City = "Z" },
            new() { Name = "A", City = "X" }
        };

        var vm = CreateAndRunVirtualMachine(
            "select Name, City from #schema.first() order by Name asc, City asc", source);
        var result = vm.Run();

        Assert.AreEqual("A", result[0][0]);
        Assert.AreEqual("X", result[0][1]);
        Assert.AreEqual("A", result[1][0]);
        Assert.AreEqual("Z", result[1][1]);
        Assert.AreEqual("B", result[2][0]);
    }

    [TestMethod]
    public void WhenOrderByThenByDescending_ShouldSortCorrectly()
    {
        var source = new SimpleEntity[]
        {
            new() { Name = "A", City = "X" },
            new() { Name = "A", City = "Z" },
            new() { Name = "B", City = "Y" }
        };

        var vm = CreateAndRunVirtualMachine(
            "select Name, City from #schema.first() order by Name asc, City desc", source);
        var result = vm.Run();

        Assert.AreEqual("A", result[0][0]);
        Assert.AreEqual("Z", result[0][1]);
        Assert.AreEqual("A", result[1][0]);
        Assert.AreEqual("X", result[1][1]);
    }

    #endregion
}
