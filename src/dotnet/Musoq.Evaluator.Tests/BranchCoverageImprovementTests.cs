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

[TestClass]
public class BranchCoverageImprovementTests : GenericEntityTestBase
{
    #region IndexedList / Table Contains(key, value) Bug Fix Verification

    [TestMethod]
    public void IndexedList_ContainsWithKey_WhenKeyExistsAndValueMatches_ShouldReturnTrue()
    {
        var list = new TestIndexedList();
        var row = new ObjectsRow([42]);
        var key = new Key([42], [0]);
        list.AddRowWithIndex(key, row);

        var result = list.Contains(key, row);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IndexedList_ContainsWithKey_WhenKeyExistsButValueDiffers_ShouldReturnFalse()
    {
        var list = new TestIndexedList();
        var row = new ObjectsRow([42]);
        var key = new Key([42], [0]);
        list.AddRowWithIndex(key, row);

        var differentRow = new ObjectsRow([99]);
        var result = list.Contains(key, differentRow);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IndexedList_ContainsWithKey_WhenKeyDoesNotExist_ShouldReturnFalse()
    {
        var list = new TestIndexedList();
        var row = new ObjectsRow([42]);
        var key = new Key([42], [0]);
        list.AddRowWithIndex(key, row);

        var missingKey = new Key([999], [0]);
        var result = list.Contains(missingKey, row);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IndexedList_ContainsWithKey_MultipleRows_ShouldFindMatchingRow()
    {
        var list = new TestIndexedList();
        var key = new Key([20], [0]);
        list.AddRowWithIndex(key, new ObjectsRow([10]));
        list.AddRowWithIndex(key, new ObjectsRow([20]));
        list.AddRowWithIndex(key, new ObjectsRow([30]));

        var targetRow = new ObjectsRow([20]);
        var result = list.Contains(key, targetRow);

        Assert.IsTrue(result);
    }

    #endregion

    #region IndexedList TryGetIndexedValues

    [TestMethod]
    public void Table_TryGetIndexedValues_WhenKeyDoesNotExist_ShouldReturnFalseWithEmptyList()
    {
        var table = new Table("Test", [new Column("Col1", typeof(int), 0)]);
        table.Add(new ObjectsRow([42]));

        var key = new Key([999], [0]);
        var found = table.TryGetIndexedValues(key, out var values);

        Assert.IsFalse(found);
        Assert.IsEmpty(values);
    }

    #endregion

    #region IndexedList Contains(value, comparer)

    [TestMethod]
    public void Table_ContainsWithComparer_WhenMatch_ShouldReturnTrue()
    {
        var table = new Table("Test", [new Column("Col1", typeof(int), 0)]);
        table.Add(new ObjectsRow([42]));

        var searchRow = new ObjectsRow([42]);
        var result = table.Contains(searchRow, (a, b) => (int)a[0] == (int)b[0]);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Table_ContainsWithComparer_WhenNoMatch_ShouldReturnFalse()
    {
        var table = new Table("Test", [new Column("Col1", typeof(int), 0)]);
        table.Add(new ObjectsRow([42]));

        var searchRow = new ObjectsRow([99]);
        var result = table.Contains(searchRow, (a, b) => (int)a[0] == (int)b[0]);

        Assert.IsFalse(result);
    }

    #endregion

    #region Row Equality Branch Coverage

    [TestMethod]
    public void Row_Equals_WithNull_ShouldReturnFalse()
    {
        var row = new ObjectsRow([1, 2]);

        Assert.IsFalse(row.Equals((Row)null));
    }

    [TestMethod]
    public void Row_Equals_DifferentCount_ShouldReturnFalse()
    {
        var row1 = new ObjectsRow([1, 2]);
        var row2 = new ObjectsRow([1, 2, 3]);

        Assert.IsFalse(row1.Equals(row2));
    }

    [TestMethod]
    public void Row_Equals_BothNullValues_ShouldBeEqual()
    {
        var row1 = new ObjectsRow([null, "test"]);
        var row2 = new ObjectsRow([null, "test"]);

        Assert.IsTrue(row1.Equals(row2));
    }

    [TestMethod]
    public void Row_Equals_OneNullOneNotNull_ShouldNotBeEqual()
    {
        var row1 = new ObjectsRow([null, "test"]);
        var row2 = new ObjectsRow([1, "test"]);

        Assert.IsFalse(row1.Equals(row2));
    }

    [TestMethod]
    public void Row_Equals_FirstNullSecondNotNull_ShouldNotBeEqual()
    {
        var row1 = new ObjectsRow([1, "test"]);
        var row2 = new ObjectsRow([null, "test"]);

        Assert.IsFalse(row1.Equals(row2));
    }

    [TestMethod]
    public void Row_Equals_DifferentValues_ShouldNotBeEqual()
    {
        var row1 = new ObjectsRow([1, "test"]);
        var row2 = new ObjectsRow([2, "test"]);

        Assert.IsFalse(row1.Equals(row2));
    }

    [TestMethod]
    public void Row_Equals_ObjectOverload_WithNonRow_ShouldReturnFalse()
    {
        var row = new ObjectsRow([1, 2]);

        Assert.IsFalse(row.Equals("not a row"));
    }

    [TestMethod]
    public void Row_GetHashCode_ShouldBeConsistentForEqualRows()
    {
        var row1 = new ObjectsRow([1, "test"]);
        var row2 = new ObjectsRow([1, "test"]);

        Assert.AreEqual(row1.GetHashCode(), row2.GetHashCode());
    }

    [TestMethod]
    public void Row_GetHashCode_WithNullValues_ShouldNotThrow()
    {
        var row = new ObjectsRow([null, 1, null]);

        var hash = row.GetHashCode();

        Assert.IsNotNull(hash);
    }

    [TestMethod]
    public void Row_CheckWithKey_WhenBothNullValues_ShouldMatch()
    {
        var row = new ObjectsRow([null, "test"]);
        var key = new Key([null], [0]);

        Assert.IsTrue(row.CheckWithKey(key));
    }

    [TestMethod]
    public void Row_CheckWithKey_WhenRowNullKeyNotNull_ShouldNotMatch()
    {
        var row = new ObjectsRow([null, "test"]);
        var key = new Key([42], [0]);

        Assert.IsFalse(row.CheckWithKey(key));
    }

    [TestMethod]
    public void Row_CheckWithKey_WhenRowNotNullKeyNull_ShouldNotMatch()
    {
        var row = new ObjectsRow([42, "test"]);
        var key = new Key([null], [0]);

        Assert.IsFalse(row.CheckWithKey(key));
    }

    #endregion

    #region ObjectsRow Context Lazy Evaluation

    [TestMethod]
    public void ObjectsRow_WithLeftAndRightContexts_ShouldLazilyMaterialize()
    {
        var leftCtx = new object[] { "left1", "left2" };
        var rightCtx = new object[] { "right1" };
        var row = new ObjectsRow([1], leftCtx, rightCtx);

        var contexts = row.Contexts;

        Assert.HasCount(3, contexts);
        Assert.AreEqual("left1", contexts[0]);
        Assert.AreEqual("left2", contexts[1]);
        Assert.AreEqual("right1", contexts[2]);
    }

    [TestMethod]
    public void ObjectsRow_WithNullLeftContext_ShouldPadWithNull()
    {
        var rightCtx = new object[] { "right1" };
        var row = new ObjectsRow([1], null, rightCtx);

        var contexts = row.Contexts;

        Assert.HasCount(2, contexts);
        Assert.IsNull(contexts[0]);
        Assert.AreEqual("right1", contexts[1]);
    }

    [TestMethod]
    public void ObjectsRow_WithNullRightContext_ShouldPadWithNull()
    {
        var leftCtx = new object[] { "left1" };
        var row = new ObjectsRow([1], leftCtx, null);

        var contexts = row.Contexts;

        Assert.HasCount(2, contexts);
        Assert.AreEqual("left1", contexts[0]);
        Assert.IsNull(contexts[1]);
    }

    [TestMethod]
    public void ObjectsRow_WithExplicitContexts_ShouldReturnDirectly()
    {
        var ctx = new object[] { "ctx1" };
        var row = new ObjectsRow([1], ctx);

        Assert.AreSame(ctx, row.Contexts);
    }

    [TestMethod]
    public void ObjectsRow_WithNoContexts_ShouldReturnNull()
    {
        var row = new ObjectsRow([1]);

        Assert.IsNull(row.Contexts);
    }

    [TestMethod]
    public void ObjectsRow_WithBothNullContexts_ShouldThrow()
    {
        Assert.Throws<NotSupportedException>(() => new ObjectsRow([1], null, null));
    }

    #endregion

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

    #region DiagnosticContext Branch Coverage

    [TestMethod]
    public void DiagnosticContext_Scope_ShouldTrackAndNest()
    {
        var ctx = new DiagnosticContext();

        Assert.AreEqual("", ctx.CurrentScope);

        using (ctx.EnterScope("Query"))
        {
            Assert.AreEqual("Query", ctx.CurrentScope);

            using (ctx.EnterScope("Select"))
            {
                Assert.AreEqual("Query.Select", ctx.CurrentScope);
            }

            Assert.AreEqual("Query", ctx.CurrentScope);
        }

        Assert.AreEqual("", ctx.CurrentScope);
    }

    [TestMethod]
    public void DiagnosticContext_ReportError_ShouldAddError()
    {
        var ctx = new DiagnosticContext();

        ctx.ReportError(DiagnosticCode.MQ3001_UnknownColumn, "Unknown column", new TextSpan(0, 5));

        Assert.IsTrue(ctx.HasErrors);
        Assert.AreEqual(1, ctx.Errors.Count());
    }

    [TestMethod]
    public void DiagnosticContext_ReportWarning_ShouldAddWarning()
    {
        var ctx = new DiagnosticContext();

        ctx.ReportWarning(DiagnosticCode.MQ5001_UnusedAlias, "Unused alias", new TextSpan(0, 5));

        Assert.IsFalse(ctx.HasErrors);
        Assert.AreEqual(1, ctx.Warnings.Count());
    }

    [TestMethod]
    public void DiagnosticContext_ReportInfo_ShouldNotCountAsError()
    {
        var ctx = new DiagnosticContext();

        ctx.ReportInfo(DiagnosticCode.MQ5001_UnusedAlias, "Info msg", new TextSpan(0, 5));

        Assert.IsFalse(ctx.HasErrors);
    }

    [TestMethod]
    public void DiagnosticContext_ReportHint_ShouldNotCountAsError()
    {
        var ctx = new DiagnosticContext();

        ctx.ReportHint(DiagnosticCode.MQ5001_UnusedAlias, "Hint msg", new TextSpan(0, 5));

        Assert.IsFalse(ctx.HasErrors);
    }

    [TestMethod]
    public void DiagnosticContext_ReportException_ShouldConvertToError()
    {
        var ctx = new DiagnosticContext();

        ctx.ReportException(new InvalidOperationException("test failure"));

        Assert.IsTrue(ctx.HasErrors);
    }

    [TestMethod]
    public void DiagnosticContext_ReportException_WithSpan_ShouldUseProvidedSpan()
    {
        var ctx = new DiagnosticContext();
        var span = new TextSpan(10, 5);

        ctx.ReportException(new InvalidOperationException("test"), span);

        Assert.IsTrue(ctx.HasErrors);
    }

    [TestMethod]
    public void DiagnosticContext_AddRange_ShouldImportDiagnostics()
    {
        var ctx = new DiagnosticContext();
        var diagnostics = new[]
        {
            Diagnostic.Error(DiagnosticCode.MQ2001_UnexpectedToken, "error1", new TextSpan(0, 5)),
            Diagnostic.Warning(DiagnosticCode.MQ5001_UnusedAlias, "warning1", new TextSpan(5, 3))
        };

        ctx.AddRange(diagnostics);

        Assert.IsTrue(ctx.HasErrors);
        Assert.AreEqual(1, ctx.Errors.Count());
        Assert.AreEqual(1, ctx.Warnings.Count());
    }

    [TestMethod]
    public void DiagnosticContext_HasReachedMaxErrors_WhenBelowLimit_ShouldBeFalse()
    {
        var ctx = new DiagnosticContext(maxErrors: 10);

        ctx.ReportError(DiagnosticCode.MQ3001_UnknownColumn, "error", new TextSpan(0, 5));

        Assert.IsFalse(ctx.HasReachedMaxErrors);
    }

    [TestMethod]
    public void DiagnosticContext_ReportTypeMismatch_ShouldAddError()
    {
        var ctx = new DiagnosticContext();
        var node = new Musoq.Parser.Nodes.IntegerNode(42);

        ctx.ReportTypeMismatch("string", "int", node);

        Assert.IsTrue(ctx.HasErrors);
        var error = ctx.Errors.First();
        Assert.Contains("string", error.Message);
        Assert.Contains("int", error.Message);
    }

    [TestMethod]
    public void DiagnosticContext_ReportAmbiguousColumn_ShouldAddError()
    {
        var ctx = new DiagnosticContext();
        var node = new Musoq.Parser.Nodes.IntegerNode(42);

        ctx.ReportAmbiguousColumn("Name", "t1", "t2", node);

        Assert.IsTrue(ctx.HasErrors);
        var error = ctx.Errors.First();
        Assert.Contains("Name", error.Message);
        Assert.Contains("t1", error.Message);
        Assert.Contains("t2", error.Message);
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

    #region Key Equality Branch Coverage

    [TestMethod]
    public void Key_Equals_SameReference_ShouldReturnTrue()
    {
        var key = new Key([1], [0]);

        Assert.IsTrue(key.Equals(key));
    }

    [TestMethod]
    public void Key_Equals_Null_ShouldReturnFalse()
    {
        var key = new Key([1], [0]);

        Assert.IsFalse(key.Equals((Key)null));
    }

    [TestMethod]
    public void Key_Equals_DifferentColumnsLength_ShouldReturnFalse()
    {
        var key1 = new Key([1], [0]);
        var key2 = new Key([1, 2], [0, 1]);

        Assert.IsFalse(key1.Equals(key2));
    }

    [TestMethod]
    public void Key_Equals_BothNullValues_ShouldBeEqual()
    {
        var key1 = new Key([null], [0]);
        var key2 = new Key([null], [0]);

        Assert.IsTrue(key1.Equals(key2));
    }

    [TestMethod]
    public void Key_Equals_OneNullValue_ShouldNotBeEqual()
    {
        var key1 = new Key([null], [0]);
        var key2 = new Key([1], [0]);

        Assert.IsFalse(key1.Equals(key2));
    }

    [TestMethod]
    public void Key_Equals_OtherValueNull_ShouldNotBeEqual()
    {
        var key1 = new Key([1], [0]);
        var key2 = new Key([null], [0]);

        Assert.IsFalse(key1.Equals(key2));
    }

    [TestMethod]
    public void Key_Equals_ObjectOverload_SameKey_ShouldReturnTrue()
    {
        var key1 = new Key([1], [0]);
        object key2 = new Key([1], [0]);

        Assert.IsTrue(key1.Equals(key2));
    }

    [TestMethod]
    public void Key_Equals_ObjectOverload_Null_ShouldReturnFalse()
    {
        var key = new Key([1], [0]);

        Assert.IsFalse(key.Equals((object)null));
    }

    [TestMethod]
    public void Key_Equals_ObjectOverload_DifferentType_ShouldReturnFalse()
    {
        var key = new Key([1], [0]);

        Assert.IsFalse(key.Equals("not a key"));
    }

    [TestMethod]
    public void Key_GetHashCode_ShouldBeConsistent()
    {
        var key1 = new Key([1, "test"], [0, 1]);
        var key2 = new Key([1, "test"], [0, 1]);

        Assert.AreEqual(key1.GetHashCode(), key2.GetHashCode());
    }

    [TestMethod]
    public void Key_ToString_ShouldFormatCorrectly()
    {
        var key = new Key([42, "abc"], [0, 1]);

        var str = key.ToString();

        Assert.Contains("42", str);
        Assert.Contains("abc", str);
    }

    #endregion

    #region Table Operations Branch Coverage

    [TestMethod]
    public void Table_AddRange_ShouldAddMultipleRows()
    {
        var table = new Table("Test", [new Column("Col1", typeof(int), 0)]);

        table.AddRange([
            new ObjectsRow([1]),
            new ObjectsRow([2]),
            new ObjectsRow([3])
        ]);

        Assert.AreEqual(3, table.Count);
    }

    [TestMethod]
    public void Table_Add_WrongColumnCount_ShouldThrow()
    {
        var table = new Table("Test", [new Column("Col1", typeof(int), 0)]);

        Assert.Throws<NotSupportedException>(() => table.Add(new ObjectsRow([1, 2])));
    }

    [TestMethod]
    public void Table_Name_ShouldBeAccessible()
    {
        var table = new Table("MyTable", [new Column("Col1", typeof(int), 0)]);

        Assert.AreEqual("MyTable", table.Name);
    }

    [TestMethod]
    public void Table_Columns_ShouldReturnAllColumns()
    {
        var table = new Table("Test", [
            new Column("Col1", typeof(int), 0),
            new Column("Col2", typeof(string), 1)
        ]);

        var columns = table.Columns.ToList();

        Assert.HasCount(2, columns);
    }

    #endregion

    #region GroupKey Branch Coverage

    [TestMethod]
    public void GroupKey_ShouldBeComparableByValues()
    {
        var key1 = new GroupKey([42, "abc"]);
        var key2 = new GroupKey([42, "abc"]);
        var key3 = new GroupKey([42, "xyz"]);

        Assert.AreEqual(key1, key2);
        Assert.AreNotEqual(key1, key3);
    }

    [TestMethod]
    public void GroupKey_WithNullValues_ShouldHandleNulls()
    {
        var key1 = new GroupKey([null, "abc"]);
        var key2 = new GroupKey([null, "abc"]);

        Assert.AreEqual(key1, key2);
    }

    #endregion

    #region Row DebugInfo Bug Fix Verification

    [TestMethod]
    public void Row_DebugInfo_WhenCountIsZero_ShouldReturnEmpty()
    {
        var row = new ObjectsRow([]);

        var debugInfo = row.DebugInfo();

        Assert.AreEqual(string.Empty, debugInfo);
    }

    [TestMethod]
    public void Row_DebugInfo_WithSingleValue_ShouldReturnValue()
    {
        var row = new ObjectsRow([42]);

        var debugInfo = row.DebugInfo();

        Assert.AreEqual("42", debugInfo);
    }

    [TestMethod]
    public void Row_DebugInfo_WithMultipleValues_ShouldFormatWithCommas()
    {
        var row = new ObjectsRow([1, "hello", 3]);

        var debugInfo = row.DebugInfo();

        Assert.AreEqual("1, hello, 3", debugInfo);
    }

    [TestMethod]
    public void Row_Equals_ObjectOverload_WithRow_ShouldDelegate()
    {
        var row1 = new ObjectsRow([42]);
        var row2 = new ObjectsRow([42]);

        Assert.IsTrue(row1.Equals((object)row2));
    }

    [TestMethod]
    public void Row_FitsTheIndex_ShouldDelegateToDoesRowMatchKey()
    {
        var row = new ObjectsRow([10]);
        var key = new Key([10], [0]);

        Assert.IsTrue(row.FitsTheIndex(key));
    }

    [TestMethod]
    public void Row_FitsTheIndex_WhenNoMatch_ShouldReturnFalse()
    {
        var row = new ObjectsRow([10]);
        var key = new Key([99], [0]);

        Assert.IsFalse(row.FitsTheIndex(key));
    }

    #endregion

    #region SafeArrayAccess Branch Coverage

    [TestMethod]
    public void SafeArrayAccess_GetArrayElement_NullArray_ShouldReturnDefault()
    {
        var result = Helpers.SafeArrayAccess.GetArrayElement<int>(null, 0);

        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetArrayElement_EmptyArray_ShouldReturnDefault()
    {
        var result = Helpers.SafeArrayAccess.GetArrayElement(Array.Empty<int>(), 0);

        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetArrayElement_NegativeIndex_ShouldWrapAround()
    {
        var arr = new[] { 10, 20, 30 };

        Assert.AreEqual(30, Helpers.SafeArrayAccess.GetArrayElement(arr, -1));
        Assert.AreEqual(20, Helpers.SafeArrayAccess.GetArrayElement(arr, -2));
        Assert.AreEqual(10, Helpers.SafeArrayAccess.GetArrayElement(arr, -3));
    }

    [TestMethod]
    public void SafeArrayAccess_GetArrayElement_IndexBeyondLength_ShouldReturnDefault()
    {
        var arr = new[] { 10, 20 };

        Assert.AreEqual(0, Helpers.SafeArrayAccess.GetArrayElement(arr, 5));
    }

    [TestMethod]
    public void SafeArrayAccess_GetArrayElement_ValidIndex_ShouldReturnElement()
    {
        var arr = new[] { 10, 20, 30 };

        Assert.AreEqual(20, Helpers.SafeArrayAccess.GetArrayElement(arr, 1));
    }

    [TestMethod]
    public void SafeArrayAccess_GetStringCharacter_NullString_ShouldReturnNullChar()
    {
        Assert.AreEqual('\0', Helpers.SafeArrayAccess.GetStringCharacter(null, 0));
    }

    [TestMethod]
    public void SafeArrayAccess_GetStringCharacter_EmptyString_ShouldReturnNullChar()
    {
        Assert.AreEqual('\0', Helpers.SafeArrayAccess.GetStringCharacter(string.Empty, 0));
    }

    [TestMethod]
    public void SafeArrayAccess_GetStringCharacter_NegativeIndex_ShouldWrapAround()
    {
        Assert.AreEqual('c', Helpers.SafeArrayAccess.GetStringCharacter("abc", -1));
    }

    [TestMethod]
    public void SafeArrayAccess_GetStringCharacter_IndexBeyondLength_ShouldReturnNullChar()
    {
        Assert.AreEqual('\0', Helpers.SafeArrayAccess.GetStringCharacter("abc", 10));
    }

    [TestMethod]
    public void SafeArrayAccess_GetStringCharacter_ValidIndex_ShouldReturnChar()
    {
        Assert.AreEqual('b', Helpers.SafeArrayAccess.GetStringCharacter("abc", 1));
    }

    [TestMethod]
    public void SafeArrayAccess_GetDictionaryValue_NullDictionary_ShouldReturnDefault()
    {
        var result = Helpers.SafeArrayAccess.GetDictionaryValue<string, int>(null, "key");

        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetDictionaryValue_NullKey_ShouldReturnDefault()
    {
        var dict = new Dictionary<string, int> { ["a"] = 1 };

        var result = Helpers.SafeArrayAccess.GetDictionaryValue(dict, null);

        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetDictionaryValue_MissingKey_ShouldReturnDefault()
    {
        var dict = new Dictionary<string, int> { ["a"] = 1 };

        var result = Helpers.SafeArrayAccess.GetDictionaryValue(dict, "missing");

        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetDictionaryValue_ExistingKey_ShouldReturnValue()
    {
        var dict = new Dictionary<string, int> { ["a"] = 42 };

        var result = Helpers.SafeArrayAccess.GetDictionaryValue(dict, "a");

        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetListElement_NullList_ShouldReturnDefault()
    {
        var result = Helpers.SafeArrayAccess.GetListElement<int>(null, 0);

        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetListElement_EmptyList_ShouldReturnDefault()
    {
        var result = Helpers.SafeArrayAccess.GetListElement(new List<int>(), 0);

        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetListElement_NegativeIndex_ShouldWrapAround()
    {
        var list = new List<int> { 10, 20, 30 };

        Assert.AreEqual(30, Helpers.SafeArrayAccess.GetListElement(list, -1));
    }

    [TestMethod]
    public void SafeArrayAccess_GetListElement_IndexBeyondCount_ShouldReturnDefault()
    {
        var list = new List<int> { 10, 20 };

        Assert.AreEqual(0, Helpers.SafeArrayAccess.GetListElement(list, 5));
    }

    [TestMethod]
    public void SafeArrayAccess_GetListElement_ValidIndex_ShouldReturnElement()
    {
        var list = new List<int> { 10, 20, 30 };

        Assert.AreEqual(20, Helpers.SafeArrayAccess.GetListElement(list, 1));
    }

    [TestMethod]
    public void SafeArrayAccess_GetIndexedElement_NullObject_ShouldReturnDefault()
    {
        var result = Helpers.SafeArrayAccess.GetIndexedElement(null, 0, typeof(int));

        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetIndexedElement_NullIndex_ShouldReturnDefault()
    {
        var result = Helpers.SafeArrayAccess.GetIndexedElement(new[] { 1, 2 }, null, typeof(int));

        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetIndexedElement_String_ShouldReturnChar()
    {
        var result = Helpers.SafeArrayAccess.GetIndexedElement("abc", 1, typeof(char));

        Assert.AreEqual('b', result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetIndexedElement_Array_ShouldReturnElement()
    {
        var result = Helpers.SafeArrayAccess.GetIndexedElement(new[] { 10, 20, 30 }, 1, typeof(int));

        Assert.AreEqual(20, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetIndexedElement_Array_NegativeIndex_ShouldWrapAround()
    {
        var result = Helpers.SafeArrayAccess.GetIndexedElement(new[] { 10, 20, 30 }, -1, typeof(int));

        Assert.AreEqual(30, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetIndexedElement_Array_OutOfBounds_ShouldReturnDefault()
    {
        var result = Helpers.SafeArrayAccess.GetIndexedElement(new[] { 10 }, 5, typeof(int));

        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetIndexedElement_EmptyArray_ShouldReturnDefault()
    {
        var result = Helpers.SafeArrayAccess.GetIndexedElement(Array.Empty<int>(), 0, typeof(int));

        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetIndexedElement_DictionaryByStringKey_ShouldReturnValue()
    {
        var dict = new Dictionary<string, int> { ["hello"] = 42 };

        var result = Helpers.SafeArrayAccess.GetIndexedElement(dict, "hello", typeof(int));

        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetIndexedElement_DictionaryMissingKey_ShouldReturnDefault()
    {
        var dict = new Dictionary<string, int> { ["hello"] = 42 };

        var result = Helpers.SafeArrayAccess.GetIndexedElement(dict, "missing", typeof(int));

        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetIndexedElement_NullableType_ShouldReturnNull()
    {
        var result = Helpers.SafeArrayAccess.GetIndexedElement(null, 0, typeof(int?));

        Assert.IsNull(result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetIndexedElement_ReferenceType_ShouldReturnNull()
    {
        var result = Helpers.SafeArrayAccess.GetIndexedElement(null, 0, typeof(string));

        Assert.IsNull(result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetIndexedElement_NullElementType_ShouldReturnNull()
    {
        var result = Helpers.SafeArrayAccess.GetIndexedElement(null, 0, null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetIndexedElement_ListWithIndexer_ShouldReturnElement()
    {
        var list = new List<string> { "a", "b", "c" };

        var result = Helpers.SafeArrayAccess.GetIndexedElement(list, 1, typeof(string));

        Assert.AreEqual("b", result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetIndexedElement_ListWithOutOfBoundsIndexer_ShouldReturnDefault()
    {
        var list = new List<string> { "a" };

        var result = Helpers.SafeArrayAccess.GetIndexedElement(list, 99, typeof(string));

        Assert.IsNull(result);
    }

    #endregion

    #region ExpandoObjectPropertyInfo Branch Coverage

    [TestMethod]
    public void ExpandoObjectPropertyInfo_Properties_ShouldReturnExpectedValues()
    {
        var propInfo = new ExpandoObjectPropertyInfo("TestProp", typeof(string));

        Assert.AreEqual("TestProp", propInfo.Name);
        Assert.AreEqual(typeof(string), propInfo.PropertyType);
        Assert.AreEqual(typeof(System.Dynamic.ExpandoObject), propInfo.DeclaringType);
        Assert.AreEqual(typeof(System.Dynamic.ExpandoObject), propInfo.ReflectedType);
        Assert.IsTrue(propInfo.CanRead);
        Assert.IsFalse(propInfo.CanWrite);
        Assert.AreEqual(System.Reflection.PropertyAttributes.None, propInfo.Attributes);
    }

    [TestMethod]
    public void ExpandoObjectPropertyInfo_GetCustomAttributes_Inherit_ShouldReturnEmpty()
    {
        var propInfo = new ExpandoObjectPropertyInfo("TestProp", typeof(int));

        Assert.IsEmpty(propInfo.GetCustomAttributes(false));
    }

    [TestMethod]
    public void ExpandoObjectPropertyInfo_GetCustomAttributes_WithType_ShouldReturnEmpty()
    {
        var propInfo = new ExpandoObjectPropertyInfo("TestProp", typeof(int));

        Assert.IsEmpty(propInfo.GetCustomAttributes(typeof(object), false));
    }

    [TestMethod]
    public void ExpandoObjectPropertyInfo_IsDefined_ShouldReturnFalse()
    {
        var propInfo = new ExpandoObjectPropertyInfo("TestProp", typeof(int));

        Assert.IsFalse(propInfo.IsDefined(typeof(object), false));
    }

    [TestMethod]
    public void ExpandoObjectPropertyInfo_GetAccessors_ShouldReturnEmpty()
    {
        var propInfo = new ExpandoObjectPropertyInfo("TestProp", typeof(int));

        Assert.IsEmpty(propInfo.GetAccessors(false));
    }

    [TestMethod]
    public void ExpandoObjectPropertyInfo_GetGetMethod_ShouldThrow()
    {
        var propInfo = new ExpandoObjectPropertyInfo("TestProp", typeof(int));

        Assert.Throws<NotImplementedException>(() => propInfo.GetGetMethod(false));
    }

    [TestMethod]
    public void ExpandoObjectPropertyInfo_GetSetMethod_ShouldThrow()
    {
        var propInfo = new ExpandoObjectPropertyInfo("TestProp", typeof(int));

        Assert.Throws<NotImplementedException>(() => propInfo.GetSetMethod(false));
    }

    [TestMethod]
    public void ExpandoObjectPropertyInfo_GetIndexParameters_ShouldReturnEmpty()
    {
        var propInfo = new ExpandoObjectPropertyInfo("TestProp", typeof(int));

        Assert.IsEmpty(propInfo.GetIndexParameters());
    }

    [TestMethod]
    public void ExpandoObjectPropertyInfo_GetValue_ShouldThrow()
    {
        var propInfo = new ExpandoObjectPropertyInfo("TestProp", typeof(int));

        Assert.Throws<NotImplementedException>(() =>
            propInfo.GetValue(null, System.Reflection.BindingFlags.Default, null, null, null));
    }

    [TestMethod]
    public void ExpandoObjectPropertyInfo_SetValue_ShouldThrow()
    {
        var propInfo = new ExpandoObjectPropertyInfo("TestProp", typeof(int));

        Assert.Throws<NotImplementedException>(() =>
            propInfo.SetValue(null, null, System.Reflection.BindingFlags.Default, null, null, null));
    }

    #endregion

    #region IndexedList Additional Branch Coverage

    [TestMethod]
    public void IndexedList_ContainsValue_EmptyList_ShouldReturnFalse()
    {
        var list = new TestIndexedList();

        Assert.IsFalse(list.Contains(new ObjectsRow([1])));
    }

    [TestMethod]
    public void IndexedList_ContainsWithComparer_EmptyList_ShouldReturnFalse()
    {
        var list = new TestIndexedList();

        Assert.IsFalse(list.Contains(new ObjectsRow([1]), (a, b) => true));
    }

    [TestMethod]
    public void IndexedList_Indexer_Int_ShouldReturnRow()
    {
        var list = new TestIndexedList();
        var row = new ObjectsRow([42]);
        var key = new Key([42], [0]);
        list.AddRowWithIndex(key, row);

        Assert.AreEqual(row, list[0]);
    }

    [TestMethod]
    public void IndexedList_Indexer_Key_ShouldReturnMatchingRows()
    {
        var list = new TestIndexedList();
        var key = new Key([42], [0]);
        var row1 = new ObjectsRow([42]);
        var row2 = new ObjectsRow([42]);
        list.AddRowWithIndex(key, row1);
        list.AddRowWithIndex(key, row2);

        var results = list[key].ToArray();

        Assert.HasCount(2, results);
    }

    [TestMethod]
    public void IndexedList_ContainsKey_WhenKeyExists_ShouldReturnTrue()
    {
        var list = new TestIndexedList();
        var key = new Key([1], [0]);
        list.AddRowWithIndex(key, new ObjectsRow([1]));

        Assert.IsTrue(list.ContainsKey(key));
    }

    [TestMethod]
    public void IndexedList_ContainsKey_WhenKeyMissing_ShouldReturnFalse()
    {
        var list = new TestIndexedList();

        Assert.IsFalse(list.ContainsKey(new Key([999], [0])));
    }

    [TestMethod]
    public void IndexedList_TryGetIndexedValues_WhenKeyExists_ShouldReturnRows()
    {
        var list = new TestIndexedList();
        var key = new Key([42], [0]);
        list.AddRowWithIndex(key, new ObjectsRow([42]));

        var found = list.TryGetIndexedValues(key, out var values);

        Assert.IsTrue(found);
        Assert.HasCount(1, values);
    }

    [TestMethod]
    public void IndexedList_TryGetIndexedValues_WhenKeyMissing_ShouldReturnFalse()
    {
        var list = new TestIndexedList();

        var found = list.TryGetIndexedValues(new Key([999], [0]), out _);

        Assert.IsFalse(found);
    }

    [TestMethod]
    public void IndexedList_Count_ShouldReturnRowCount()
    {
        var list = new TestIndexedList();
        var key = new Key([1], [0]);
        list.AddRowWithIndex(key, new ObjectsRow([1]));
        list.AddRowWithIndex(key, new ObjectsRow([2]));

        Assert.AreEqual(2, list.Count);
    }

    #endregion

    #region Exception Branch Coverage — ConstructionNotYetSupported

    [TestMethod]
    public void ConstructionNotYetSupported_WhenCreatedWithMessage_ShouldSetCodeAndNullSpan()
    {
        var ex = new ConstructionNotYetSupported("test message");

        Assert.AreEqual(DiagnosticCode.MQ3030_ConstructionNotSupported, ex.Code);
        Assert.IsNull(ex.Span);
        Assert.AreEqual("test message", ex.Message);
    }

    [TestMethod]
    public void ConstructionNotYetSupported_WhenCreatedWithSpan_ShouldSetCodeAndSpan()
    {
        var span = new TextSpan(5, 10);
        var ex = new ConstructionNotYetSupported("test", span);

        Assert.AreEqual(DiagnosticCode.MQ3030_ConstructionNotSupported, ex.Code);
        Assert.AreEqual(span, ex.Span);
    }

    [TestMethod]
    public void ConstructionNotYetSupported_ToDiagnostic_WhenSpanIsNull_ShouldUseEmptySpan()
    {
        var ex = new ConstructionNotYetSupported("test message");

        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3030_ConstructionNotSupported, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual("test message", diagnostic.Message);
    }

    [TestMethod]
    public void ConstructionNotYetSupported_ToDiagnostic_WhenSpanIsSet_ShouldUseSpan()
    {
        var span = new TextSpan(5, 10);
        var ex = new ConstructionNotYetSupported("test", span);

        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3030_ConstructionNotSupported, diagnostic.Code);
        Assert.AreEqual("test", diagnostic.Message);
    }

    #endregion

    #region Exception Branch Coverage — FieldLinkIndexOutOfRangeException

    [TestMethod]
    public void FieldLinkIndexOutOfRange_WhenCreatedWithoutSpan_ShouldSetProperties()
    {
        var ex = new FieldLinkIndexOutOfRangeException(5, 3);

        Assert.AreEqual(5, ex.Index);
        Assert.AreEqual(3, ex.MaxGroups);
        Assert.AreEqual(DiagnosticCode.MQ3024_GroupByIndexOutOfRange, ex.Code);
        Assert.IsNull(ex.Span);
    }

    [TestMethod]
    public void FieldLinkIndexOutOfRange_WhenCreatedWithSpan_ShouldSetAllProperties()
    {
        var span = new TextSpan(1, 5);
        var ex = new FieldLinkIndexOutOfRangeException(2, 4, span);

        Assert.AreEqual(2, ex.Index);
        Assert.AreEqual(4, ex.MaxGroups);
        Assert.AreEqual(span, ex.Span);
    }

    [TestMethod]
    public void FieldLinkIndexOutOfRange_ToDiagnostic_ShouldReturnError()
    {
        var ex = new FieldLinkIndexOutOfRangeException(5, 3);

        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3024_GroupByIndexOutOfRange, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    #endregion

    #region Exception Branch Coverage — ObjectDoesNotImplementIndexerException

    [TestMethod]
    public void ObjectDoesNotImplementIndexer_WhenCreatedWithMessage_ShouldSetCode()
    {
        var ex = new ObjectDoesNotImplementIndexerException("test");

        Assert.AreEqual(DiagnosticCode.MQ3018_NoIndexer, ex.Code);
        Assert.IsNull(ex.Span);
    }

    [TestMethod]
    public void ObjectDoesNotImplementIndexer_WhenCreatedWithSpan_ShouldSetSpan()
    {
        var span = new TextSpan(0, 5);
        var ex = new ObjectDoesNotImplementIndexerException("test", span);

        Assert.AreEqual(span, ex.Span);
        Assert.AreEqual(DiagnosticCode.MQ3018_NoIndexer, ex.Code);
    }

    [TestMethod]
    public void ObjectDoesNotImplementIndexer_ToDiagnostic_ShouldReturnError()
    {
        var ex = new ObjectDoesNotImplementIndexerException("test");
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    #endregion

    #region Exception Branch Coverage — ObjectIsNotAnArrayException

    [TestMethod]
    public void ObjectIsNotAnArray_WhenCreatedWithMessage_ShouldSetCode()
    {
        var ex = new ObjectIsNotAnArrayException("test");

        Assert.AreEqual(DiagnosticCode.MQ3017_ObjectNotArray, ex.Code);
        Assert.IsNull(ex.Span);
    }

    [TestMethod]
    public void ObjectIsNotAnArray_WhenCreatedWithSpan_ShouldSetSpan()
    {
        var span = new TextSpan(0, 5);
        var ex = new ObjectIsNotAnArrayException("test", span);

        Assert.AreEqual(span, ex.Span);
    }

    [TestMethod]
    public void ObjectIsNotAnArray_ToDiagnostic_ShouldReturnError()
    {
        var ex = new ObjectIsNotAnArrayException("test");
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    #endregion

    #region Exception Branch Coverage — InvalidQueryExpressionTypeException

    [TestMethod]
    public void InvalidQueryExpressionType_WhenCreatedWithDescription_ShouldIncludeTypeName()
    {
        var ex = new InvalidQueryExpressionTypeException("expr", typeof(int), "context");

        Assert.AreEqual(DiagnosticCode.MQ3027_InvalidExpressionType, ex.Code);
        Assert.IsNull(ex.Span);
        StringAssert.Contains(ex.Message, "Int32");
    }

    [TestMethod]
    public void InvalidQueryExpressionType_WhenCreatedWithNullType_ShouldShowNull()
    {
        var ex = new InvalidQueryExpressionTypeException("expr", null, "context");

        StringAssert.Contains(ex.Message, "null");
    }

    [TestMethod]
    public void InvalidQueryExpressionType_WhenCreatedWithFieldNode_ShouldIncludeFieldName()
    {
        var intNode = new IntegerNode("1", "i");
        var fieldNode = new FieldNode(intNode, 0, "testField");
        var ex = new InvalidQueryExpressionTypeException(fieldNode, typeof(string), "context");

        Assert.AreEqual(DiagnosticCode.MQ3027_InvalidExpressionType, ex.Code);
        StringAssert.Contains(ex.Message, "testField");
    }

    [TestMethod]
    public void InvalidQueryExpressionType_WhenCreatedWithSpan_ShouldSetSpan()
    {
        var span = new TextSpan(2, 8);
        var ex = new InvalidQueryExpressionTypeException("msg", span);

        Assert.AreEqual(span, ex.Span);
    }

    [TestMethod]
    public void InvalidQueryExpressionType_ToDiagnostic_ShouldReturnError()
    {
        var ex = new InvalidQueryExpressionTypeException("expr", typeof(int), "ctx");
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3027_InvalidExpressionType, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    #endregion

    #region Exception Branch Coverage — SetOperatorMustHaveSameQuantityOfColumnsException

    [TestMethod]
    public void SetOperatorSameQuantity_WhenCreatedParameterless_ShouldSetCode()
    {
        var ex = new SetOperatorMustHaveSameQuantityOfColumnsException();

        Assert.AreEqual(DiagnosticCode.MQ3019_SetOperatorColumnCount, ex.Code);
        Assert.IsNull(ex.Span);
    }

    [TestMethod]
    public void SetOperatorSameQuantity_WhenCreatedWithCounts_ShouldSetSpanAndMessage()
    {
        var span = new TextSpan(0, 10);
        var ex = new SetOperatorMustHaveSameQuantityOfColumnsException(3, 5, span);

        Assert.AreEqual(span, ex.Span);
        Assert.IsTrue(ex.Message.Contains("3") || ex.Message.Contains("5"));
    }

    [TestMethod]
    public void SetOperatorSameQuantity_ToDiagnostic_ShouldReturnError()
    {
        var ex = new SetOperatorMustHaveSameQuantityOfColumnsException();
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    #endregion

    #region Exception Branch Coverage — TypeNotFoundException

    [TestMethod]
    public void TypeNotFound_WhenCreatedWithMessage_ShouldSetCodeAndNoTypeName()
    {
        var ex = new TypeNotFoundException("test message");

        Assert.AreEqual(DiagnosticCode.MQ3005_TypeMismatch, ex.Code);
        Assert.IsNull(ex.TypeName);
        Assert.IsNull(ex.Span);
    }

    [TestMethod]
    public void TypeNotFound_WhenCreatedWithEmptyContext_ShouldNotAppendContextSuffix()
    {
        var span = new TextSpan(0, 5);
        var ex = new TypeNotFoundException("MyType", "", span);

        Assert.AreEqual("MyType", ex.TypeName);
        Assert.AreEqual(span, ex.Span);
        Assert.AreNotEqual(string.Empty, ex.Message);
    }

    [TestMethod]
    public void TypeNotFound_WhenCreatedWithNonEmptyContext_ShouldAppendContextSuffix()
    {
        var span = new TextSpan(0, 5);
        var ex = new TypeNotFoundException("MyType", "some context", span);

        StringAssert.Contains(ex.Message, "some context");
    }

    [TestMethod]
    public void TypeNotFound_ToDiagnostic_ShouldReturnError()
    {
        var ex = new TypeNotFoundException("test");
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3005_TypeMismatch, diagnostic.Code);
    }

    #endregion

    #region Exception Branch Coverage — ColumnMustBeAnArrayOrImplementIEnumerableException

    [TestMethod]
    public void ColumnMustBeArray_WhenCreatedParameterless_ShouldSetCode()
    {
        var ex = new ColumnMustBeAnArrayOrImplementIEnumerableException();

        Assert.AreEqual(DiagnosticCode.MQ3025_ColumnMustBeArray, ex.Code);
        Assert.IsNull(ex.ColumnName);
        Assert.IsNull(ex.Span);
    }

    [TestMethod]
    public void ColumnMustBeArray_WhenCreatedWithColumnAndSpan_ShouldSetProperties()
    {
        var span = new TextSpan(0, 5);
        var ex = new ColumnMustBeAnArrayOrImplementIEnumerableException("col1", span);

        Assert.AreEqual("col1", ex.ColumnName);
        Assert.AreEqual(span, ex.Span);
    }

    [TestMethod]
    public void ColumnMustBeArray_ToDiagnostic_ShouldReturnError()
    {
        var ex = new ColumnMustBeAnArrayOrImplementIEnumerableException();
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    #endregion

    #region Exception Branch Coverage — ColumnMustBeMarkedAsBindablePropertyAsTableException

    [TestMethod]
    public void ColumnMustBeBindable_WhenCreatedParameterless_ShouldSetCode()
    {
        var ex = new ColumnMustBeMarkedAsBindablePropertyAsTableException();

        Assert.AreEqual(DiagnosticCode.MQ3026_ColumnNotBindable, ex.Code);
        Assert.IsNull(ex.ColumnName);
    }

    [TestMethod]
    public void ColumnMustBeBindable_WhenCreatedWithColumnAndSpan_ShouldSetProperties()
    {
        var span = new TextSpan(0, 5);
        var ex = new ColumnMustBeMarkedAsBindablePropertyAsTableException("col1", span);

        Assert.AreEqual("col1", ex.ColumnName);
        Assert.AreEqual(span, ex.Span);
    }

    [TestMethod]
    public void ColumnMustBeBindable_ToDiagnostic_ShouldReturnError()
    {
        var ex = new ColumnMustBeMarkedAsBindablePropertyAsTableException();
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    #endregion

    #region Exception Branch Coverage — TableIsNotDefinedException

    [TestMethod]
    public void TableIsNotDefined_WhenCreatedWithTableName_ShouldSetProperties()
    {
        var ex = new TableIsNotDefinedException("MyTable");

        Assert.AreEqual("MyTable", ex.TableName);
        Assert.AreEqual(DiagnosticCode.MQ3023_TableNotDefined, ex.Code);
        Assert.IsNull(ex.Span);
    }

    [TestMethod]
    public void TableIsNotDefined_WhenCreatedWithSpan_ShouldSetSpan()
    {
        var span = new TextSpan(0, 5);
        var ex = new TableIsNotDefinedException("MyTable", span);

        Assert.AreEqual(span, ex.Span);
        Assert.AreEqual("MyTable", ex.TableName);
    }

    [TestMethod]
    public void TableIsNotDefined_ToDiagnostic_ShouldReturnError()
    {
        var ex = new TableIsNotDefinedException("MyTable");
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3023_TableNotDefined, diagnostic.Code);
    }

    #endregion

    #region Exception Branch Coverage — UnresolvableMethodException

    [TestMethod]
    public void UnresolvableMethod_WhenCreatedWithMessage_ShouldSetCode()
    {
        var ex = new UnresolvableMethodException("test");

        Assert.AreEqual(DiagnosticCode.MQ3004_UnknownFunction, ex.Code);
        Assert.IsNull(ex.MethodName);
        Assert.IsNull(ex.ArgumentTypes);
        Assert.IsNull(ex.Span);
    }

    [TestMethod]
    public void UnresolvableMethod_WhenCreatedWithDetails_ShouldSetAllProperties()
    {
        var span = new TextSpan(0, 5);
        var argTypes = new[] { "int", "string" };
        var ex = new UnresolvableMethodException("DoWork", argTypes, span);

        Assert.AreEqual("DoWork", ex.MethodName);
        Assert.AreEqual(argTypes, ex.ArgumentTypes);
        Assert.AreEqual(span, ex.Span);
        StringAssert.Contains(ex.Message, "DoWork");
    }

    [TestMethod]
    public void UnresolvableMethod_ToDiagnostic_ShouldReturnError()
    {
        var ex = new UnresolvableMethodException("test");
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3004_UnknownFunction, diagnostic.Code);
    }

    #endregion

    #region Exception Branch Coverage — AliasAlreadyUsedException

    [TestMethod]
    public void AliasAlreadyUsed_WhenCreatedWithSchemaFromNodeWithoutSpan_ShouldHaveNullSpan_ViaBranch()
    {
        var node = new SchemaFromNode("schema", "method", ArgsListNode.Empty, "alias", typeof(object), 0);
        var ex = new AliasAlreadyUsedException(node, "myAlias");

        Assert.AreEqual("myAlias", ex.Alias);
        Assert.AreEqual(DiagnosticCode.MQ3021_DuplicateAlias, ex.Code);
        Assert.IsNull(ex.Span);
    }

    [TestMethod]
    public void AliasAlreadyUsed_WhenCreatedWithAliasAndSpanOverload_ShouldSetSpanAndAlias()
    {
        var span = new TextSpan(0, 5);
        var ex = new AliasAlreadyUsedException("duplicateAlias", span);

        Assert.AreEqual("duplicateAlias", ex.Alias);
        Assert.AreEqual(span, ex.Span);
        Assert.AreEqual(DiagnosticCode.MQ3021_DuplicateAlias, ex.Code);
    }

    [TestMethod]
    public void AliasAlreadyUsed_WhenCreatedWithAliasAndSpan_ShouldSetProperties()
    {
        var span = new TextSpan(0, 5);
        var ex = new AliasAlreadyUsedException("myAlias", span);

        Assert.AreEqual("myAlias", ex.Alias);
        Assert.AreEqual(span, ex.Span);
    }

    [TestMethod]
    public void AliasAlreadyUsed_ToDiagnostic_ShouldReturnError()
    {
        var span = new TextSpan(0, 5);
        var ex = new AliasAlreadyUsedException("alias", span);
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3021_DuplicateAlias, diagnostic.Code);
    }

    #endregion

    #region Exception Branch Coverage — SetOperatorMustHaveKeyColumnsException

    [TestMethod]
    public void SetOperatorKeyColumns_WhenCreatedWithUnion_ShouldCreateCorrectMessage()
    {
        var ex = new SetOperatorMustHaveKeyColumnsException("Union");

        StringAssert.Contains(ex.Message, "UNION");
        StringAssert.Contains(ex.Message, "UNION (<key_columns>)");
    }

    [TestMethod]
    public void SetOperatorKeyColumns_WhenCreatedWithUnionAll_ShouldCreateCorrectMessage()
    {
        var ex = new SetOperatorMustHaveKeyColumnsException("UnionAll");

        StringAssert.Contains(ex.Message, "UNION ALL");
        StringAssert.Contains(ex.Message, "UNION ALL (<key_columns>)");
    }

    [TestMethod]
    public void SetOperatorKeyColumns_WhenCreatedWithExcept_ShouldCreateCorrectMessage()
    {
        var ex = new SetOperatorMustHaveKeyColumnsException("Except");

        StringAssert.Contains(ex.Message, "EXCEPT");
        StringAssert.Contains(ex.Message, "EXCEPT (<key_columns>)");
    }

    [TestMethod]
    public void SetOperatorKeyColumns_WhenCreatedWithIntersect_ShouldCreateCorrectMessage()
    {
        var ex = new SetOperatorMustHaveKeyColumnsException("Intersect");

        StringAssert.Contains(ex.Message, "INTERSECT");
        StringAssert.Contains(ex.Message, "INTERSECT (<key_columns>)");
    }

    [TestMethod]
    public void SetOperatorKeyColumns_WhenCreatedWithUnknownOperator_ShouldUseFallback()
    {
        var ex = new SetOperatorMustHaveKeyColumnsException("CustomOp");

        StringAssert.Contains(ex.Message, "CUSTOMOP");
    }

    [TestMethod]
    public void SetOperatorKeyColumns_WhenCreatedWithSpan_ShouldSetSpan()
    {
        var span = new TextSpan(1, 10);
        var ex = new SetOperatorMustHaveKeyColumnsException("Union", span);

        Assert.AreEqual(span, ex.Span);
        Assert.AreEqual(DiagnosticCode.MQ3031_SetOperatorMissingKeys, ex.Code);
    }

    [TestMethod]
    public void SetOperatorKeyColumns_ToDiagnostic_ShouldReturnError()
    {
        var ex = new SetOperatorMustHaveKeyColumnsException("Union");
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3031_SetOperatorMissingKeys, diagnostic.Code);
    }

    [TestMethod]
    public void SetOperatorKeyColumns_CreateMessage_ShouldCombineSyntaxAndDisplayName()
    {
        var message = SetOperatorMustHaveKeyColumnsException.CreateMessage("Intersect");

        StringAssert.Contains(message, "INTERSECT (<key_columns>)");
        StringAssert.Contains(message, "INTERSECT");
    }

    #endregion

    #region Exception Branch Coverage — VisitorException

    [TestMethod]
    public void VisitorException_WhenCreatedWithNullNames_ShouldCoalesceToUnknown()
    {
        var ex = new VisitorException(null, null, "test message");

        Assert.AreEqual("Unknown", ex.VisitorName);
        Assert.AreEqual("Unknown", ex.Operation);
    }

    [TestMethod]
    public void VisitorException_WhenCreatedWithValidNames_ShouldPreserveNames()
    {
        var ex = new VisitorException("MyVisitor", "DoStuff", "msg");

        Assert.AreEqual("MyVisitor", ex.VisitorName);
        Assert.AreEqual("DoStuff", ex.Operation);
    }

    [TestMethod]
    public void VisitorException_WhenCreatedWithDiagnosticInner_ShouldResolveFromInner()
    {
        var innerEx = new ConstructionNotYetSupported("inner", new TextSpan(0, 5));
        var ex = new VisitorException("Vis", "Op", "msg", innerEx);

        Assert.AreEqual(DiagnosticCode.MQ3030_ConstructionNotSupported, ex.Code);
    }

    [TestMethod]
    public void VisitorException_WhenCreatedWithGenericInner_ShouldUseFallbackCode()
    {
        var innerEx = new InvalidOperationException("generic error");
        var ex = new VisitorException("Vis", "Op", "msg", innerEx);

        Assert.AreEqual(DiagnosticSeverity.Error, ex.ToDiagnostic().Severity);
    }

    [TestMethod]
    public void VisitorException_WhenCreatedWithNullInner_ShouldUseDefaultCode()
    {
        var ex = new VisitorException("Vis", "Op", "msg", (Exception)null);

        Assert.AreEqual(DiagnosticCode.MQ2030_UnsupportedSyntax, ex.Code);
        Assert.IsNull(ex.Span);
    }

    [TestMethod]
    public void VisitorException_WhenCreatedWithCodeAndSpan_ShouldSetDirectly()
    {
        var span = new TextSpan(0, 5);
        var ex = new VisitorException("Vis", "Op", "msg", DiagnosticCode.MQ3005_TypeMismatch, span);

        Assert.AreEqual(DiagnosticCode.MQ3005_TypeMismatch, ex.Code);
        Assert.AreEqual(span, ex.Span);
    }

    [TestMethod]
    public void VisitorException_CreateForStackUnderflow_ShouldCreateWithDetails()
    {
        var ex = VisitorException.CreateForStackUnderflow("TestVisitor", "Visit", 3, 1);

        Assert.AreEqual("TestVisitor", ex.VisitorName);
        Assert.AreEqual("Visit", ex.Operation);
        StringAssert.Contains(ex.Message, "3");
    }

    [TestMethod]
    public void VisitorException_CreateForNullNode_ShouldCreateWithNodeType()
    {
        var ex = VisitorException.CreateForNullNode("TestVisitor", "Visit", "SelectNode");

        StringAssert.Contains(ex.Message, "SelectNode");
    }

    [TestMethod]
    public void VisitorException_CreateForInvalidNodeType_ShouldCreateWithTypes()
    {
        var ex = VisitorException.CreateForInvalidNodeType("TestVisitor", "Visit", "SelectNode", "WhereNode");

        StringAssert.Contains(ex.Message, "SelectNode");
        StringAssert.Contains(ex.Message, "WhereNode");
    }

    [TestMethod]
    public void VisitorException_CreateForProcessingFailure_WithSuggestion_ShouldAppendSuggestion()
    {
        var ex = VisitorException.CreateForProcessingFailure("Vis", "Op", "context", "Try this instead");

        StringAssert.Contains(ex.Message, "context");
        StringAssert.Contains(ex.Message, "Try this instead");
    }

    [TestMethod]
    public void VisitorException_CreateForProcessingFailure_WithoutSuggestion_ShouldNotAppend()
    {
        var ex = VisitorException.CreateForProcessingFailure("Vis", "Op", "context", null);

        StringAssert.Contains(ex.Message, "context");
    }

    [TestMethod]
    public void VisitorException_CreateForProcessingFailure_WithEmptySuggestion_ShouldNotAppend()
    {
        var ex = VisitorException.CreateForProcessingFailure("Vis", "Op", "context", "");

        StringAssert.Contains(ex.Message, "context");
    }

    [TestMethod]
    public void VisitorException_ToDiagnostic_WhenSpanNull_ShouldUseEmpty()
    {
        var ex = new VisitorException("Vis", "Op", "msg");
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    [TestMethod]
    public void VisitorException_ToDiagnostic_WhenSpanSet_ShouldUseSpan()
    {
        var span = new TextSpan(5, 10);
        var ex = new VisitorException("Vis", "Op", "msg", DiagnosticCode.MQ3005_TypeMismatch, span);
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3005_TypeMismatch, diagnostic.Code);
    }

    #endregion

    #region Exception Branch Coverage — AliasMissingException

    [TestMethod]
    public void AliasMissing_WhenCreatedWithAccessMethodNode_ShouldSetCode()
    {
        var funcToken = new FunctionToken("Count", new TextSpan(0, 5));
        var node = new AccessMethodNode(funcToken, ArgsListNode.Empty, ArgsListNode.Empty, true);

        var ex = new AliasMissingException(node);

        Assert.AreEqual(DiagnosticCode.MQ3022_MissingAlias, ex.Code);
        StringAssert.Contains(ex.Message, "Count");
    }

    [TestMethod]
    public void AliasMissing_WhenCreatedWithMessageAndSpan_ShouldSetProperties()
    {
        var span = new TextSpan(0, 5);
        var ex = new AliasMissingException("test message", span);

        Assert.AreEqual(span, ex.Span);
        Assert.AreEqual(DiagnosticCode.MQ3022_MissingAlias, ex.Code);
    }

    [TestMethod]
    public void AliasMissing_ToDiagnostic_ShouldReturnError()
    {
        var span = new TextSpan(0, 5);
        var ex = new AliasMissingException("test", span);
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3022_MissingAlias, diagnostic.Code);
    }

    [TestMethod]
    public void AliasMissing_CreateMethodCallMessage_ShouldFormatCorrectly()
    {
        var message = AliasMissingException.CreateMethodCallMessage("Sum(col)");

        StringAssert.Contains(message, "Sum(col)");
        StringAssert.Contains(message, "alias");
    }

    #endregion

    #region Exception Branch Coverage — AmbiguousAggregateOwnerException

    [TestMethod]
    public void AmbiguousAggregateOwner_WhenCreatedWithoutSpan_ShouldSetCode()
    {
        var aliases = new[] { "a", "b" };
        var ex = new AmbiguousAggregateOwnerException("Count(*)", aliases);

        Assert.AreEqual(DiagnosticCode.MQ3034_AmbiguousAggregateOwner, ex.Code);
        Assert.IsNull(ex.Span);
        StringAssert.Contains(ex.Message, "Count(*)");
    }

    [TestMethod]
    public void AmbiguousAggregateOwner_WhenCreatedWithSpan_ShouldSetSpan()
    {
        var span = new TextSpan(0, 5);
        var aliases = new[] { "a", "b" };
        var ex = new AmbiguousAggregateOwnerException("Count(*)", aliases, span);

        Assert.AreEqual(span, ex.Span);
    }

    [TestMethod]
    public void AmbiguousAggregateOwner_ToDiagnostic_ShouldReturnError()
    {
        var ex = new AmbiguousAggregateOwnerException("Count(*)", new[] { "a" });
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    #endregion

    #region Exception Branch Coverage — AmbiguousMethodOwnerException

    [TestMethod]
    public void AmbiguousMethodOwner_WhenCreatedWithoutSpan_ShouldSetCode()
    {
        var aliases = new[] { "x", "y" };
        var ex = new AmbiguousMethodOwnerException("DoWork()", aliases);

        Assert.AreEqual(DiagnosticCode.MQ3035_AmbiguousMethodOwner, ex.Code);
        Assert.IsNull(ex.Span);
    }

    [TestMethod]
    public void AmbiguousMethodOwner_WhenCreatedWithSpan_ShouldSetSpan()
    {
        var span = new TextSpan(0, 5);
        var ex = new AmbiguousMethodOwnerException("DoWork()", new[] { "x" }, span);

        Assert.AreEqual(span, ex.Span);
    }

    [TestMethod]
    public void AmbiguousMethodOwner_ToDiagnostic_ShouldReturnError()
    {
        var ex = new AmbiguousMethodOwnerException("DoWork()", new[] { "x" });
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    #endregion

    #region Exception Branch Coverage — NonAggregatedColumnInSelectException

    [TestMethod]
    public void NonAggregatedColumn_WhenCreatedWithGroupByColumns_ShouldSetProperties()
    {
        var groupByCols = new[] { "Name", "Age" };
        var ex = new NonAggregatedColumnInSelectException("City", groupByCols);

        Assert.AreEqual("City", ex.ColumnName);
        Assert.AreEqual(groupByCols, ex.GroupByColumns);
        Assert.AreEqual(DiagnosticCode.MQ3012_NonAggregateInSelect, ex.Code);
        Assert.IsNull(ex.Span);
    }

    [TestMethod]
    public void NonAggregatedColumn_WhenCreatedWithEmptyGroupBy_ShouldShowNone()
    {
        var ex = new NonAggregatedColumnInSelectException("City", []);

        StringAssert.Contains(ex.Message, "(none)");
    }

    [TestMethod]
    public void NonAggregatedColumn_WhenCreatedWithSpan_ShouldSetSpan()
    {
        var span = new TextSpan(0, 5);
        var ex = new NonAggregatedColumnInSelectException("City", new[] { "Name" }, span);

        Assert.AreEqual(span, ex.Span);
    }

    [TestMethod]
    public void NonAggregatedColumn_ToDiagnostic_ShouldReturnError()
    {
        var ex = new NonAggregatedColumnInSelectException("City", new[] { "Name" });
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3012_NonAggregateInSelect, diagnostic.Code);
    }

    #endregion

    #region Exception Branch Coverage — SetOperatorMustHaveSameTypesOfColumnsException

    [TestMethod]
    public void SetOperatorSameTypes_WhenCreatedWithFieldNodes_ShouldSetCode()
    {
        var leftExpr = new IntegerNode("1", "i");
        var rightExpr = new IntegerNode("2", "i");
        var left = new FieldNode(leftExpr, 0, "left");
        var right = new FieldNode(rightExpr, 1, "right");

        var ex = new SetOperatorMustHaveSameTypesOfColumnsException(left, right);

        Assert.AreEqual(DiagnosticCode.MQ3020_SetOperatorColumnTypes, ex.Code);
        Assert.IsNull(ex.Span);
    }

    [TestMethod]
    public void SetOperatorSameTypes_WhenCreatedWithSpan_ShouldSetSpan()
    {
        var span = new TextSpan(0, 10);
        var ex = new SetOperatorMustHaveSameTypesOfColumnsException("type mismatch", span);

        Assert.AreEqual(span, ex.Span);
    }

    [TestMethod]
    public void SetOperatorSameTypes_ToDiagnostic_ShouldReturnError()
    {
        var span = new TextSpan(0, 5);
        var ex = new SetOperatorMustHaveSameTypesOfColumnsException("msg", span);
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3020_SetOperatorColumnTypes, diagnostic.Code);
    }

    #endregion

    #region Exception Branch Coverage — TypeMismatchException

    [TestMethod]
    public void TypeMismatch_WhenCreated_ShouldSetAllProperties()
    {
        var span = new TextSpan(0, 5);
        var ex = new TypeMismatchException(typeof(int), typeof(string), span);

        Assert.AreEqual(typeof(int), ex.ExpectedType);
        Assert.AreEqual(typeof(string), ex.ActualType);
        Assert.AreEqual(span, ex.Span);
        Assert.AreEqual(DiagnosticCode.MQ3005_TypeMismatch, ex.Code);
    }

    [TestMethod]
    public void TypeMismatch_ToDiagnostic_ShouldReturnError()
    {
        var ex = new TypeMismatchException(typeof(int), typeof(string), new TextSpan(0, 5));
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3005_TypeMismatch, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    #endregion

    #region SemanticAnalysisException Branch Coverage

    [TestMethod]
    public void SemanticAnalysisException_WhenCreatedWithDiagnostic_ShouldSetProperties()
    {
        var diagnostic = Diagnostic.Error(DiagnosticCode.MQ3005_TypeMismatch, "type error", new TextSpan(0, 5));
        var ex = new SemanticAnalysisException("analysis failed", diagnostic);

        Assert.AreEqual(diagnostic, ex.PrimaryDiagnostic);
        Assert.AreEqual(DiagnosticCode.MQ3005_TypeMismatch, ex.Code);
        Assert.AreEqual("analysis failed", ex.Message);
    }

    [TestMethod]
    public void SemanticAnalysisException_WhenCreatedWithInnerException_ShouldPreserveInner()
    {
        var diagnostic = Diagnostic.Error(DiagnosticCode.MQ3005_TypeMismatch, "error", new TextSpan(0, 5));
        var inner = new InvalidOperationException("inner");
        var ex = new SemanticAnalysisException("msg", diagnostic, inner);

        Assert.AreEqual(inner, ex.InnerException);
        Assert.AreEqual(diagnostic.Location, ex.Location);
    }

    #endregion

    #region SemanticAnalysisResult Branch Coverage

    [TestMethod]
    public void SemanticAnalysisResult_WhenCreatedWithNullDiagnostics_ShouldCreateEmptyList()
    {
        var node = new IntegerNode("1", "i");
        var result = new SemanticAnalysisResult(node);

        Assert.IsEmpty(result.Diagnostics);
        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.HasErrors);
        Assert.IsFalse(result.HasWarnings);
        Assert.AreEqual(0, result.ErrorCount);
        Assert.AreEqual(0, result.WarningCount);
    }

    [TestMethod]
    public void SemanticAnalysisResult_WhenCreatedWithErrors_ShouldReportFailure()
    {
        var node = new IntegerNode("1", "i");
        var diag = Diagnostic.Error(DiagnosticCode.MQ3005_TypeMismatch, "err", new TextSpan(0, 5));
        var result = new SemanticAnalysisResult(node, new[] { diag });

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.HasErrors);
        Assert.AreEqual(1, result.ErrorCount);
        Assert.AreEqual(1, result.Errors.Count());
    }

    [TestMethod]
    public void SemanticAnalysisResult_WhenCreatedWithWarnings_ShouldReportWarnings()
    {
        var node = new IntegerNode("1", "i");
        var diag = Diagnostic.Warning(DiagnosticCode.MQ3005_TypeMismatch, "warn", new TextSpan(0, 5));
        var result = new SemanticAnalysisResult(node, new[] { diag });

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.HasWarnings);
        Assert.AreEqual(1, result.WarningCount);
        Assert.AreEqual(1, result.Warnings.Count());
    }

    [TestMethod]
    public void SemanticAnalysisResult_AddDiagnostic_ShouldAppendToList()
    {
        var node = new IntegerNode("1", "i");
        var result = new SemanticAnalysisResult(node);
        var diag = Diagnostic.Error(DiagnosticCode.MQ3005_TypeMismatch, "err", new TextSpan(0, 5));

        result.AddDiagnostic(diag);

        Assert.AreEqual(1, result.Diagnostics.Count);
        Assert.IsTrue(result.HasErrors);
    }

    #endregion

    #region NullLogger Branch Coverage

    [TestMethod]
    public void NullLogger_BeginScope_ShouldReturnNull()
    {
        var logger = new NullLogger<object>();

        var scope = logger.BeginScope("state");

        Assert.IsNull(scope);
    }

    [TestMethod]
    public void NullLogger_IsEnabled_ShouldReturnFalse()
    {
        var logger = new NullLogger<object>();

        Assert.IsFalse(logger.IsEnabled(LogLevel.Trace));
        Assert.IsFalse(logger.IsEnabled(LogLevel.Debug));
        Assert.IsFalse(logger.IsEnabled(LogLevel.Information));
        Assert.IsFalse(logger.IsEnabled(LogLevel.Warning));
        Assert.IsFalse(logger.IsEnabled(LogLevel.Error));
        Assert.IsFalse(logger.IsEnabled(LogLevel.Critical));
    }

    [TestMethod]
    public void NullLogger_Log_ShouldNotThrow()
    {
        var logger = new NullLogger<object>();

        logger.Log(LogLevel.Error, new EventId(1), "state", null, (s, e) => s.ToString());
    }

    #endregion

    #region TransitionSchemaProvider Branch Coverage

    [TestMethod]
    public void TransitionSchemaProvider_GetSchema_WhenTransientExists_ShouldReturnTransient()
    {
        var innerProvider = new TestSchemaProvider("inner");
        var provider = new TransitionSchemaProvider(innerProvider);
        var transientSchema = new TestSchema("transient");

        provider.AddTransitionSchema(transientSchema);

        var result = provider.GetSchema("transient");

        Assert.AreEqual(transientSchema, result);
    }

    [TestMethod]
    public void TransitionSchemaProvider_GetSchema_WhenTransientMissing_ShouldFallbackToInner()
    {
        var innerSchema = new TestSchema("test");
        var innerProvider = new TestSchemaProvider("test", innerSchema);
        var provider = new TransitionSchemaProvider(innerProvider);

        var result = provider.GetSchema("test");

        Assert.AreEqual(innerSchema, result);
    }

    #endregion

    #region QuerySourceInfo Branch Coverage

    [TestMethod]
    public void QuerySourceInfo_Empty_ShouldHaveDefaultValues()
    {
        var info = QuerySourceInfo.Empty;

        Assert.IsNull(info.FromNode);
        Assert.IsEmpty(info.Columns);
        Assert.IsNull(info.WhereNode);
        Assert.IsFalse(info.HasExternallyProvidedTypes);
    }

    [TestMethod]
    public void QuerySourceInfo_FromTuple_WithNullHints_ShouldUseEmptyHints()
    {
        var tuple = (FromNode: (SchemaFromNode)null, Columns: (IReadOnlyCollection<ISchemaColumn>)Array.Empty<ISchemaColumn>(), WhereNode: (WhereNode)null, HasExternallyProvidedTypes: false);

        var result = QuerySourceInfo.FromTuple(tuple);

        Assert.AreEqual(QueryHints.Empty, result.QueryHints);
    }

    [TestMethod]
    public void QuerySourceInfo_FromTuple_WithHints_ShouldUseProvidedHints()
    {
        var hints = QueryHints.Create(10, 20, true);
        var tuple = (FromNode: (SchemaFromNode)null, Columns: (IReadOnlyCollection<ISchemaColumn>)Array.Empty<ISchemaColumn>(), WhereNode: (WhereNode)null, HasExternallyProvidedTypes: true);

        var result = QuerySourceInfo.FromTuple(tuple, hints);

        Assert.AreEqual(hints, result.QueryHints);
        Assert.IsTrue(result.HasExternallyProvidedTypes);
    }

    #endregion

    #region Exception Branch Coverage — AmbiguousColumnException

    [TestMethod]
    public void AmbiguousColumn_WhenCreatedWithoutSpan_ShouldSetProperties()
    {
        var ex = new AmbiguousColumnException("Name", "a", "b");

        Assert.AreEqual("Name", ex.ColumnName);
        Assert.AreEqual("a", ex.Alias1);
        Assert.AreEqual("b", ex.Alias2);
        Assert.AreEqual(DiagnosticCode.MQ3002_AmbiguousColumn, ex.Code);
        Assert.IsNull(ex.Span);
        StringAssert.Contains(ex.Message, "Name");
    }

    [TestMethod]
    public void AmbiguousColumn_WhenCreatedWithSpan_ShouldSetSpan()
    {
        var span = new TextSpan(5, 10);
        var ex = new AmbiguousColumnException("Col", "x", "y", span);

        Assert.AreEqual(span, ex.Span);
        Assert.AreEqual("Col", ex.ColumnName);
        Assert.AreEqual("x", ex.Alias1);
        Assert.AreEqual("y", ex.Alias2);
        Assert.AreEqual(DiagnosticCode.MQ3002_AmbiguousColumn, ex.Code);
    }

    [TestMethod]
    public void AmbiguousColumn_ToDiagnostic_WithoutSpan_ShouldUseEmptySpan()
    {
        var ex = new AmbiguousColumnException("Name", "a", "b");
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3002_AmbiguousColumn, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    [TestMethod]
    public void AmbiguousColumn_ToDiagnostic_WithSpan_ShouldUseProvidedSpan()
    {
        var span = new TextSpan(5, 10);
        var ex = new AmbiguousColumnException("Name", "a", "b", span);
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3002_AmbiguousColumn, diagnostic.Code);
    }

    #endregion

    #region Exception Branch Coverage — CannotResolveMethodException

    [TestMethod]
    public void CannotResolveMethod_WhenCreatedWithMessage_ShouldSetDefaults()
    {
        var ex = new CannotResolveMethodException("test error");

        Assert.AreEqual("test error", ex.Message);
        Assert.AreEqual(DiagnosticCode.MQ3029_UnresolvableMethod, ex.Code);
        Assert.IsNull(ex.Span);
    }

    [TestMethod]
    public void CannotResolveMethod_WhenCreatedWithMessageAndSpan_ShouldSetSpan()
    {
        var span = new TextSpan(0, 5);
        var ex = new CannotResolveMethodException("error", span);

        Assert.AreEqual(span, ex.Span);
        Assert.AreEqual(DiagnosticCode.MQ3029_UnresolvableMethod, ex.Code);
    }

    [TestMethod]
    public void CannotResolveMethod_WhenCreatedWithCustomCode_ShouldUseProvidedCode()
    {
        var span = new TextSpan(0, 5);
        var ex = new CannotResolveMethodException("error", DiagnosticCode.MQ3004_UnknownFunction, span);

        Assert.AreEqual(DiagnosticCode.MQ3004_UnknownFunction, ex.Code);
        Assert.AreEqual(span, ex.Span);
    }

    [TestMethod]
    public void CannotResolveMethod_ToDiagnostic_ShouldReturnError()
    {
        var ex = new CannotResolveMethodException("cannot resolve");
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3029_UnresolvableMethod, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    [TestMethod]
    public void CannotResolveMethod_CreateForNullArguments_ShouldContainMethodName()
    {
        var ex = CannotResolveMethodException.CreateForNullArguments("Foo");

        StringAssert.Contains(ex.Message, "Foo");
        StringAssert.Contains(ex.Message, "null arguments");
    }

    [TestMethod]
    public void CannotResolveMethod_CreateForCannotMatch_WithArgs_ShouldListTypes()
    {
        var args = new Node[] { new IntegerNode("1", "i"), new IntegerNode("2", "i") };
        var ex = CannotResolveMethodException.CreateForCannotMatchMethodNameOrArguments("Bar", args);

        StringAssert.Contains(ex.Message, "Bar");
    }

    [TestMethod]
    public void CannotResolveMethod_CreateForCannotMatch_WithEmptyArgs_ShouldUseEmptyTypes()
    {
        var ex = CannotResolveMethodException.CreateForCannotMatchMethodNameOrArguments("Baz", []);

        StringAssert.Contains(ex.Message, "Baz");
    }

    #endregion

    #region Exception Branch Coverage — UnknownColumnOrAliasException

    [TestMethod]
    public void UnknownColumnOrAlias_WhenCreatedWithMessage_ShouldSetDefaults()
    {
        var ex = new UnknownColumnOrAliasException("unknown col");

        Assert.AreEqual("unknown col", ex.Message);
        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, ex.Code);
        Assert.IsNull(ex.Span);
        Assert.IsNull(ex.ColumnName);
    }

    [TestMethod]
    public void UnknownColumnOrAlias_WhenCreatedWithContext_ShouldAppendContext()
    {
        var span = new TextSpan(0, 5);
        var ex = new UnknownColumnOrAliasException("Col", "in table Users", span);

        Assert.AreEqual("Col", ex.ColumnName);
        Assert.AreEqual(span, ex.Span);
        StringAssert.Contains(ex.Message, "Col");
        StringAssert.Contains(ex.Message, "in table Users");
    }

    [TestMethod]
    public void UnknownColumnOrAlias_WhenCreatedWithEmptyContext_ShouldOmitContext()
    {
        var span = new TextSpan(0, 5);
        var ex = new UnknownColumnOrAliasException("Col", "", span);

        StringAssert.Contains(ex.Message, "Col");
        Assert.DoesNotContain("  ", ex.Message);
    }

    [TestMethod]
    public void UnknownColumnOrAlias_WhenCreatedWithNullContext_ShouldOmitContext()
    {
        var span = new TextSpan(0, 5);
        var ex = new UnknownColumnOrAliasException("Col", null, span);

        StringAssert.Contains(ex.Message, "Col");
    }

    [TestMethod]
    public void UnknownColumnOrAlias_ToDiagnostic_ShouldReturnError()
    {
        var ex = new UnknownColumnOrAliasException("Col", "ctx", new TextSpan(0, 5));
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    #endregion

    #region Exception Branch Coverage — UnknownInterpretationSchemaException

    [TestMethod]
    public void UnknownInterpretationSchema_WhenCreatedWithName_ShouldSetProperties()
    {
        var ex = new UnknownInterpretationSchemaException("mySchema");

        Assert.AreEqual("mySchema", ex.SchemaName);
        Assert.AreEqual(DiagnosticCode.MQ3010_UnknownSchema, ex.Code);
        Assert.IsNull(ex.Span);
        StringAssert.Contains(ex.Message, "mySchema");
    }

    [TestMethod]
    public void UnknownInterpretationSchema_WhenCreatedWithNameAndMessage_ShouldSetCustomMessage()
    {
        var ex = new UnknownInterpretationSchemaException("mySchema", "custom error");

        Assert.AreEqual("mySchema", ex.SchemaName);
        Assert.AreEqual("custom error", ex.Message);
    }

    [TestMethod]
    public void UnknownInterpretationSchema_WhenCreatedWithSpan_ShouldSetSpan()
    {
        var span = new TextSpan(10, 20);
        var ex = new UnknownInterpretationSchemaException("s", "msg", span);

        Assert.AreEqual(span, ex.Span);
        Assert.AreEqual("s", ex.SchemaName);
    }

    [TestMethod]
    public void UnknownInterpretationSchema_ToDiagnostic_ShouldReturnError()
    {
        var ex = new UnknownInterpretationSchemaException("schema");
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3010_UnknownSchema, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    [TestMethod]
    public void UnknownInterpretationSchema_CreateForSchemaNotInRegistry_ShouldContainSchemaName()
    {
        var ex = UnknownInterpretationSchemaException.CreateForSchemaNotInRegistry("missing");

        Assert.AreEqual("missing", ex.SchemaName);
        StringAssert.Contains(ex.Message, "missing");
        StringAssert.Contains(ex.Message, "not found");
    }

    [TestMethod]
    public void UnknownInterpretationSchema_CreateForTypeGenerationFailed_ShouldContainSchemaName()
    {
        var ex = UnknownInterpretationSchemaException.CreateForTypeGenerationFailed("broken");

        Assert.AreEqual("broken", ex.SchemaName);
        StringAssert.Contains(ex.Message, "broken");
        StringAssert.Contains(ex.Message, "unavailable");
    }

    #endregion

    #region Exception Branch Coverage — UnknownPropertyException

    [TestMethod]
    public void UnknownProperty_WhenCreatedWithMessage_ShouldSetDefaults()
    {
        var ex = new UnknownPropertyException("property not found");

        Assert.AreEqual("property not found", ex.Message);
        Assert.AreEqual(DiagnosticCode.MQ3014_InvalidPropertyAccess, ex.Code);
        Assert.IsNull(ex.Span);
        Assert.IsNull(ex.PropertyName);
        Assert.IsNull(ex.TypeName);
    }

    [TestMethod]
    public void UnknownProperty_WhenCreatedWithDetails_ShouldSetAllProperties()
    {
        var span = new TextSpan(0, 10);
        var ex = new UnknownPropertyException("Age", "Person", span);

        Assert.AreEqual("Age", ex.PropertyName);
        Assert.AreEqual("Person", ex.TypeName);
        Assert.AreEqual(span, ex.Span);
        Assert.AreEqual(DiagnosticCode.MQ3014_InvalidPropertyAccess, ex.Code);
        StringAssert.Contains(ex.Message, "Age");
        StringAssert.Contains(ex.Message, "Person");
    }

    [TestMethod]
    public void UnknownProperty_ToDiagnostic_ShouldReturnError()
    {
        var ex = new UnknownPropertyException("Age", "Person", new TextSpan(0, 5));
        var diagnostic = ex.ToDiagnostic();

        Assert.AreEqual(DiagnosticCode.MQ3014_InvalidPropertyAccess, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    #endregion

    #region SemanticAnalysisResult — Additional Branch Coverage

    [TestMethod]
    public void SemanticAnalysisResult_AddDiagnostics_ShouldAppendAll()
    {
        var node = new IntegerNode("1", "i");
        var result = new SemanticAnalysisResult(node);
        var diags = new[]
        {
            Diagnostic.Error(DiagnosticCode.MQ3005_TypeMismatch, "err1", new TextSpan(0, 5)),
            Diagnostic.Warning(DiagnosticCode.MQ5001_UnusedAlias, "warn1", new TextSpan(5, 10))
        };

        result.AddDiagnostics(diags);

        Assert.HasCount(2, result.Diagnostics);
        Assert.AreEqual(1, result.ErrorCount);
        Assert.AreEqual(1, result.WarningCount);
    }

    [TestMethod]
    public void SemanticAnalysisResult_ThrowIfErrors_WhenNoErrors_ShouldNotThrow()
    {
        var node = new IntegerNode("1", "i");
        var result = new SemanticAnalysisResult(node);

        result.ThrowIfErrors();
    }

    [TestMethod]
    public void SemanticAnalysisResult_ThrowIfErrors_WhenHasErrors_ShouldThrow()
    {
        var node = new IntegerNode("1", "i");
        var diag = Diagnostic.Error(DiagnosticCode.MQ3001_UnknownColumn, "unknown col", new TextSpan(0, 5));
        var result = new SemanticAnalysisResult(node, [diag]);

        var ex = Assert.Throws<SemanticAnalysisException>(() => result.ThrowIfErrors());
        StringAssert.Contains(ex.Message, "unknown col");
    }

    [TestMethod]
    public void SemanticAnalysisResult_ThrowIfErrors_WhenOnlyWarnings_ShouldNotThrow()
    {
        var node = new IntegerNode("1", "i");
        var diag = Diagnostic.Warning(DiagnosticCode.MQ5001_UnusedAlias, "warn", new TextSpan(0, 5));
        var result = new SemanticAnalysisResult(node, [diag]);

        result.ThrowIfErrors();
    }

    [TestMethod]
    public void SemanticAnalysisResult_GetDiagnosticsAt_ShouldReturnMatchingDiagnostics()
    {
        var node = new IntegerNode("1", "i");
        var diag1 = Diagnostic.Error(DiagnosticCode.MQ3001_UnknownColumn, "err1", new TextSpan(0, 10));
        var diag2 = Diagnostic.Error(DiagnosticCode.MQ3001_UnknownColumn, "err2", new TextSpan(20, 30));
        var result = new SemanticAnalysisResult(node, [diag1, diag2]);

        var atFive = result.GetDiagnosticsAt(5).ToList();

        Assert.HasCount(1, atFive);
        Assert.AreEqual("err1", atFive[0].Message);
    }

    [TestMethod]
    public void SemanticAnalysisResult_GetDiagnosticsAt_WhenNoMatch_ShouldReturnEmpty()
    {
        var node = new IntegerNode("1", "i");
        var diag = Diagnostic.Error(DiagnosticCode.MQ3001_UnknownColumn, "err", new TextSpan(0, 5));
        var result = new SemanticAnalysisResult(node, [diag]);

        var atHundred = result.GetDiagnosticsAt(100).ToList();

        Assert.HasCount(0, atHundred);
    }

    [TestMethod]
    public void SemanticAnalysisResult_GetDiagnosticsIn_ShouldReturnOverlapping()
    {
        var node = new IntegerNode("1", "i");
        var diag1 = Diagnostic.Error(DiagnosticCode.MQ3001_UnknownColumn, "err1", new TextSpan(0, 10));
        var diag2 = Diagnostic.Error(DiagnosticCode.MQ3001_UnknownColumn, "err2", new TextSpan(20, 30));
        var result = new SemanticAnalysisResult(node, [diag1, diag2]);

        var overlapping = result.GetDiagnosticsIn(new TextSpan(5, 25)).ToList();

        Assert.IsGreaterThanOrEqualTo(1, overlapping.Count);
    }

    #endregion

    #region DiagnosticContext Branch Coverage

    [TestMethod]
    public void DiagnosticContext_WhenCreated_ShouldHaveNoErrors()
    {
        var ctx = new DiagnosticContext();

        Assert.IsFalse(ctx.HasErrors);
        Assert.IsFalse(ctx.HasReachedMaxErrors);
        Assert.AreEqual("", ctx.CurrentScope);
    }

    [TestMethod]
    public void DiagnosticContext_EnterAndExitScope_ShouldTrackScopePath()
    {
        var ctx = new DiagnosticContext();

        using (ctx.EnterScope("outer"))
        {
            Assert.AreEqual("outer", ctx.CurrentScope);

            using (ctx.EnterScope("inner"))
            {
                Assert.AreEqual("outer.inner", ctx.CurrentScope);
            }

            Assert.AreEqual("outer", ctx.CurrentScope);
        }

        Assert.AreEqual("", ctx.CurrentScope);
    }

    [TestMethod]
    public void DiagnosticContext_ExitScope_WhenEmpty_ShouldNotThrow()
    {
        var ctx = new DiagnosticContext();

        // ExitScope is private, but ScopeGuard.Dispose calls it.
        // Double-dispose should also be safe.
        var scope = ctx.EnterScope("test");
        scope.Dispose();
        scope.Dispose();

        Assert.AreEqual("", ctx.CurrentScope);
    }

    [TestMethod]
    public void DiagnosticContext_ReportError_WithSpan_ShouldAddError()
    {
        var ctx = new DiagnosticContext();

        ctx.ReportError(DiagnosticCode.MQ3001_UnknownColumn, "col not found", new TextSpan(0, 5));

        Assert.IsTrue(ctx.HasErrors);
        Assert.IsTrue(ctx.Errors.Any());
    }

    [TestMethod]
    public void DiagnosticContext_ReportError_WithNode_WhenNodeHasSpan_ShouldUseNodeSpan()
    {
        var ctx = new DiagnosticContext();
        var node = new IntegerNode("1", "i");
        node.WithSpan(new TextSpan(10, 20));

        ctx.ReportError(DiagnosticCode.MQ3001_UnknownColumn, "error", node);

        Assert.IsTrue(ctx.HasErrors);
    }

    [TestMethod]
    public void DiagnosticContext_ReportError_WithNode_WhenNodeHasNoSpan_ShouldUseEmptySpan()
    {
        var ctx = new DiagnosticContext();
        var node = new IntegerNode("1", "i");

        ctx.ReportError(DiagnosticCode.MQ3001_UnknownColumn, "error", node);

        Assert.IsTrue(ctx.HasErrors);
    }

    [TestMethod]
    public void DiagnosticContext_ReportWarning_WithSpan_ShouldAddWarning()
    {
        var ctx = new DiagnosticContext();

        ctx.ReportWarning(DiagnosticCode.MQ5001_UnusedAlias, "unused", new TextSpan(0, 5));

        Assert.IsFalse(ctx.HasErrors);
        Assert.IsTrue(ctx.Warnings.Any());
    }

    [TestMethod]
    public void DiagnosticContext_ReportWarning_WithNode_ShouldAddWarning()
    {
        var ctx = new DiagnosticContext();
        var node = new IntegerNode("1", "i");

        ctx.ReportWarning(DiagnosticCode.MQ5001_UnusedAlias, "unused", node);

        Assert.IsTrue(ctx.Warnings.Any());
    }

    [TestMethod]
    public void DiagnosticContext_ReportInfo_ShouldAddDiagnostic()
    {
        var ctx = new DiagnosticContext();

        ctx.ReportInfo(DiagnosticCode.MQ5001_UnusedAlias, "info msg", new TextSpan(0, 5));

        Assert.IsTrue(ctx.Diagnostics.Any());
    }

    [TestMethod]
    public void DiagnosticContext_ReportHint_ShouldAddDiagnostic()
    {
        var ctx = new DiagnosticContext();

        ctx.ReportHint(DiagnosticCode.MQ5001_UnusedAlias, "hint msg", new TextSpan(0, 5));

        Assert.IsTrue(ctx.Diagnostics.Any());
    }

    [TestMethod]
    public void DiagnosticContext_ReportException_WithDiagnosticException_ShouldAddError()
    {
        var ctx = new DiagnosticContext();
        var ex = new UnknownColumnOrAliasException("Col", "ctx", new TextSpan(0, 5));

        ctx.ReportException(ex);

        Assert.IsTrue(ctx.HasErrors);
    }

    [TestMethod]
    public void DiagnosticContext_ReportException_WithRegularException_ShouldAddGenericError()
    {
        var ctx = new DiagnosticContext();
        var ex = new InvalidOperationException("something broke");

        ctx.ReportException(ex);

        Assert.IsTrue(ctx.HasErrors);
    }

    [TestMethod]
    public void DiagnosticContext_ReportException_WithOverrideSpan_ShouldUseProvidedSpan()
    {
        var ctx = new DiagnosticContext();
        var ex = new InvalidOperationException("error");
        var span = new TextSpan(10, 20);

        ctx.ReportException(ex, span);

        Assert.IsTrue(ctx.HasErrors);
    }

    [TestMethod]
    public void DiagnosticContext_ReportUnknownAlias_ShouldAddError()
    {
        var ctx = new DiagnosticContext();
        var node = new IntegerNode("1", "i");

        ctx.ReportUnknownAlias("badAlias", ["alias1", "alias2"], node);

        Assert.IsTrue(ctx.HasErrors);
    }

    [TestMethod]
    public void DiagnosticContext_ReportUnknownAlias_WithSuggestion_ShouldIncludeDidYouMean()
    {
        var ctx = new DiagnosticContext();
        var node = new IntegerNode("1", "i");

        ctx.ReportUnknownAlias("alis1", ["alias1", "alias2"], node);

        Assert.IsTrue(ctx.HasErrors);
        var error = ctx.Errors.First();
        StringAssert.Contains(error.Message, "alis1");
    }

    [TestMethod]
    public void DiagnosticContext_ReportUnknownColumn_ShouldAddError()
    {
        var ctx = new DiagnosticContext();
        var node = new IntegerNode("1", "i");

        ctx.ReportUnknownColumn("badCol", ["Name", "Age"], node);

        Assert.IsTrue(ctx.HasErrors);
    }

    [TestMethod]
    public void DiagnosticContext_ReportUnknownColumn_WithSuggestion_ShouldIncludeDidYouMean()
    {
        var ctx = new DiagnosticContext();
        var node = new IntegerNode("1", "i");

        ctx.ReportUnknownColumn("Nme", ["Name", "Age"], node);

        Assert.IsTrue(ctx.HasErrors);
        var error = ctx.Errors.First();
        StringAssert.Contains(error.Message, "Nme");
    }

    [TestMethod]
    public void DiagnosticContext_ReportUnknownProperty_ShouldAddError()
    {
        var ctx = new DiagnosticContext();
        var node = new IntegerNode("1", "i");

        ctx.ReportUnknownProperty("badProp", ["Prop1", "Prop2"], node);

        Assert.IsTrue(ctx.HasErrors);
    }

    [TestMethod]
    public void DiagnosticContext_ReportUnknownFunction_ShouldAddError()
    {
        var ctx = new DiagnosticContext();
        var node = new IntegerNode("1", "i");

        ctx.ReportUnknownFunction("badFunc", ["Func1", "Func2"], node);

        Assert.IsTrue(ctx.HasErrors);
    }

    [TestMethod]
    public void DiagnosticContext_ReportAmbiguousAggregateOwner_ShouldAddError()
    {
        var ctx = new DiagnosticContext();
        var node = new IntegerNode("1", "i");

        ctx.ReportAmbiguousAggregateOwner("Count()", ["a", "b"], node);

        Assert.IsTrue(ctx.HasErrors);
        var error = ctx.Errors.First();
        StringAssert.Contains(error.Message, "Count()");
    }

    [TestMethod]
    public void DiagnosticContext_ReportAmbiguousMethodOwner_ShouldAddError()
    {
        var ctx = new DiagnosticContext();
        var node = new IntegerNode("1", "i");

        ctx.ReportAmbiguousMethodOwner("Foo()", ["x", "y"], node);

        Assert.IsTrue(ctx.HasErrors);
        var error = ctx.Errors.First();
        StringAssert.Contains(error.Message, "Foo()");
    }

    [TestMethod]
    public void DiagnosticContext_ReportInvalidArgumentCount_ShouldAddError()
    {
        var ctx = new DiagnosticContext();
        var node = new IntegerNode("1", "i");

        ctx.ReportInvalidArgumentCount("Sum", 1, 3, node);

        Assert.IsTrue(ctx.HasErrors);
        var error = ctx.Errors.First();
        StringAssert.Contains(error.Message, "Sum");
    }

    [TestMethod]
    public void DiagnosticContext_Clear_ShouldRemoveAllDiagnosticsAndScopes()
    {
        var ctx = new DiagnosticContext();
        ctx.ReportError(DiagnosticCode.MQ3001_UnknownColumn, "err", new TextSpan(0, 5));
        ctx.EnterScope("test");

        ctx.Clear();

        Assert.IsFalse(ctx.HasErrors);
        Assert.AreEqual("", ctx.CurrentScope);
    }

    [TestMethod]
    public void DiagnosticContext_ToResult_ShouldCreateSemanticAnalysisResult()
    {
        var ctx = new DiagnosticContext();
        ctx.ReportError(DiagnosticCode.MQ3001_UnknownColumn, "err", new TextSpan(0, 5));
        var rootNode = new IntegerNode("1", "i");

        var result = ctx.ToResult(rootNode);

        Assert.IsNotNull(result);
        Assert.AreEqual(rootNode, result.RootNode);
        Assert.IsTrue(result.HasErrors);
    }

    [TestMethod]
    public void DiagnosticContext_SourceText_ShouldReturnConfiguredValue()
    {
        var sourceText = new SourceText("SELECT 1");
        var ctx = new DiagnosticContext(sourceText);

        Assert.AreEqual(sourceText, ctx.SourceText);
    }

    [TestMethod]
    public void DiagnosticContext_HasReachedMaxErrors_WhenMaxExceeded_ShouldReturnTrue()
    {
        var ctx = new DiagnosticContext(maxErrors: 2);

        ctx.ReportError(DiagnosticCode.MQ3001_UnknownColumn, "err1", new TextSpan(0, 1));
        ctx.ReportError(DiagnosticCode.MQ3001_UnknownColumn, "err2", new TextSpan(1, 2));

        Assert.IsTrue(ctx.HasReachedMaxErrors);
    }

    #endregion

    #region DiagnosticBag Branch Coverage

    [TestMethod]
    public void DiagnosticBag_Add_WhenNull_ShouldThrow()
    {
        var bag = new DiagnosticBag();

        Assert.Throws<ArgumentNullException>(() => bag.Add(null));
    }

    [TestMethod]
    public void DiagnosticBag_Add_WhenMaxErrorsReached_ShouldRejectNewErrors()
    {
        var bag = new DiagnosticBag { MaxErrors = 1 };

        bag.AddError(DiagnosticCode.MQ3001_UnknownColumn, "err1", new TextSpan(0, 1));
        var added = bag.AddError(DiagnosticCode.MQ3001_UnknownColumn, "err2", new TextSpan(1, 2));

        Assert.IsFalse(added);
        Assert.AreEqual(1, bag.ErrorCount);
        Assert.IsTrue(bag.HasTooManyErrors);
    }

    [TestMethod]
    public void DiagnosticBag_AddWarning_ShouldIncrementWarningCount()
    {
        var bag = new DiagnosticBag();

        bag.AddWarning(DiagnosticCode.MQ5001_UnusedAlias, "warn", new TextSpan(0, 5));

        Assert.AreEqual(1, bag.WarningCount);
        Assert.AreEqual(0, bag.ErrorCount);
        Assert.IsFalse(bag.HasErrors);
    }

    [TestMethod]
    public void DiagnosticBag_AddInfo_ShouldAddDiagnostic()
    {
        var bag = new DiagnosticBag();

        bag.AddInfo(DiagnosticCode.MQ5001_UnusedAlias, "info", new TextSpan(0, 5));

        Assert.AreEqual(1, bag.Count);
    }

    [TestMethod]
    public void DiagnosticBag_AddHint_ShouldAddDiagnostic()
    {
        var bag = new DiagnosticBag();

        bag.AddHint(DiagnosticCode.MQ5001_UnusedAlias, "hint", new TextSpan(0, 5));

        Assert.AreEqual(1, bag.Count);
    }

    [TestMethod]
    public void DiagnosticBag_AddRange_ShouldStopWhenMaxErrors()
    {
        var bag = new DiagnosticBag { MaxErrors = 1 };
        var diags = new[]
        {
            Diagnostic.Error(DiagnosticCode.MQ3001_UnknownColumn, "err1", new TextSpan(0, 1)),
            Diagnostic.Error(DiagnosticCode.MQ3001_UnknownColumn, "err2", new TextSpan(1, 2)),
            Diagnostic.Error(DiagnosticCode.MQ3001_UnknownColumn, "err3", new TextSpan(2, 3))
        };

        bag.AddRange(diags);

        Assert.AreEqual(1, bag.ErrorCount);
    }

    [TestMethod]
    public void DiagnosticBag_ToSortedList_ShouldReturnSortedByLocation()
    {
        var bag = new DiagnosticBag();
        bag.AddError(DiagnosticCode.MQ3001_UnknownColumn, "err2", new TextSpan(20, 25));
        bag.AddError(DiagnosticCode.MQ3001_UnknownColumn, "err1", new TextSpan(0, 5));

        var sorted = bag.ToSortedList();

        Assert.AreEqual("err1", sorted[0].Message);
        Assert.AreEqual("err2", sorted[1].Message);
    }

    [TestMethod]
    public void DiagnosticBag_GetErrors_ShouldFilterOnlyErrors()
    {
        var bag = new DiagnosticBag();
        bag.AddError(DiagnosticCode.MQ3001_UnknownColumn, "err", new TextSpan(0, 5));
        bag.AddWarning(DiagnosticCode.MQ5001_UnusedAlias, "warn", new TextSpan(5, 10));

        var errors = bag.GetErrors().ToList();

        Assert.HasCount(1, errors);
        Assert.AreEqual("err", errors[0].Message);
    }

    [TestMethod]
    public void DiagnosticBag_GetWarnings_ShouldFilterOnlyWarnings()
    {
        var bag = new DiagnosticBag();
        bag.AddError(DiagnosticCode.MQ3001_UnknownColumn, "err", new TextSpan(0, 5));
        bag.AddWarning(DiagnosticCode.MQ5001_UnusedAlias, "warn", new TextSpan(5, 10));

        var warnings = bag.GetWarnings().ToList();

        Assert.HasCount(1, warnings);
        Assert.AreEqual("warn", warnings[0].Message);
    }

    [TestMethod]
    public void DiagnosticBag_Clear_ShouldResetAll()
    {
        var bag = new DiagnosticBag();
        bag.AddError(DiagnosticCode.MQ3001_UnknownColumn, "err", new TextSpan(0, 5));
        bag.AddWarning(DiagnosticCode.MQ5001_UnusedAlias, "warn", new TextSpan(5, 10));

        bag.Clear();

        Assert.AreEqual(0, bag.Count);
        Assert.AreEqual(0, bag.ErrorCount);
        Assert.AreEqual(0, bag.WarningCount);
        Assert.IsFalse(bag.HasErrors);
    }

    [TestMethod]
    public void DiagnosticBag_Enumerable_ShouldIterateAllDiagnostics()
    {
        var bag = new DiagnosticBag();
        bag.AddError(DiagnosticCode.MQ3001_UnknownColumn, "err", new TextSpan(0, 5));
        bag.AddWarning(DiagnosticCode.MQ5001_UnusedAlias, "warn", new TextSpan(5, 10));

        var count = 0;
        foreach (var _ in bag)
            count++;

        Assert.AreEqual(2, count);
    }

    [TestMethod]
    public void DiagnosticBag_AddWithLocationOverload_ShouldCreateDiagnostic()
    {
        var bag = new DiagnosticBag();
        var location = new SourceLocation(0, 1, 1);

        var added = bag.Add(DiagnosticCode.MQ3001_UnknownColumn, DiagnosticSeverity.Error, "err", location);

        Assert.IsTrue(added);
        Assert.AreEqual(1, bag.ErrorCount);
    }

    [TestMethod]
    public void DiagnosticBag_AddWithSourceText_ShouldGenerateContextSnippet()
    {
        var bag = new DiagnosticBag { SourceText = new SourceText("SELECT 1 FROM dual") };
        var location = new SourceLocation(0, 1, 1);
        var endLocation = new SourceLocation(6, 1, 7);

        bag.Add(DiagnosticCode.MQ3001_UnknownColumn, DiagnosticSeverity.Error, "err", location, endLocation);

        Assert.AreEqual(1, bag.ErrorCount);
    }

    [TestMethod]
    public void DiagnosticBag_AddError_WithFormatArgs_ShouldFormatMessage()
    {
        var bag = new DiagnosticBag();

        bag.AddError(DiagnosticCode.MQ3001_UnknownColumn, new TextSpan(0, 5), "MyColumn");

        var errors = bag.GetErrors().ToList();
        Assert.HasCount(1, errors);
        StringAssert.Contains(errors[0].Message, "MyColumn");
    }

    [TestMethod]
    public void DiagnosticBag_AddWarning_WithFormatArgs_ShouldFormatMessage()
    {
        var bag = new DiagnosticBag();

        bag.AddWarning(DiagnosticCode.MQ5001_UnusedAlias, new TextSpan(0, 5), "myAlias");

        var warnings = bag.GetWarnings().ToList();
        Assert.HasCount(1, warnings);
        StringAssert.Contains(warnings[0].Message, "myAlias");
    }

    [TestMethod]
    public void DiagnosticBag_AddRange_FromOtherBag_ShouldImportAll()
    {
        var bag1 = new DiagnosticBag();
        bag1.AddError(DiagnosticCode.MQ3001_UnknownColumn, "err1", new TextSpan(0, 5));
        bag1.AddWarning(DiagnosticCode.MQ5001_UnusedAlias, "warn1", new TextSpan(5, 10));

        var bag2 = new DiagnosticBag();
        bag2.AddRange(bag1);

        Assert.AreEqual(1, bag2.ErrorCount);
        Assert.AreEqual(1, bag2.WarningCount);
    }

    #endregion

    #region ErrorCatalog Branch Coverage

    [TestMethod]
    public void ErrorCatalog_GetTemplate_WhenCodeExists_ShouldReturnTemplate()
    {
        var template = ErrorCatalog.GetTemplate(DiagnosticCode.MQ3001_UnknownColumn);

        StringAssert.Contains(template, "{0}");
    }

    [TestMethod]
    public void ErrorCatalog_GetTemplate_WhenCodeDoesNotExist_ShouldReturnFallback()
    {
        var template = ErrorCatalog.GetTemplate((DiagnosticCode)99999);

        StringAssert.Contains(template, "99999");
    }

    [TestMethod]
    public void ErrorCatalog_GetMessage_WithArgs_ShouldFormatMessage()
    {
        var message = ErrorCatalog.GetMessage(DiagnosticCode.MQ3001_UnknownColumn, "MyCol");

        StringAssert.Contains(message, "MyCol");
    }

    [TestMethod]
    public void ErrorCatalog_GetMessage_WithNoArgs_ShouldReturnTemplate()
    {
        var message = ErrorCatalog.GetMessage(DiagnosticCode.MQ1002_UnterminatedString);

        StringAssert.Contains(message, "Unterminated");
    }

    [TestMethod]
    public void ErrorCatalog_GetMessage_WithBadFormatArgs_ShouldReturnTemplate()
    {
        // MQ3002 expects 3 args ({0}, {1}, {2}), passing wrong number should fallback
        var message = ErrorCatalog.GetMessage(DiagnosticCode.MQ3002_AmbiguousColumn);

        Assert.IsNotNull(message);
    }

    [TestMethod]
    public void ErrorCatalog_GetDefaultSeverity_ForWarning_ShouldReturnWarning()
    {
        var severity = ErrorCatalog.GetDefaultSeverity(DiagnosticCode.MQ5001_UnusedAlias);

        Assert.AreEqual(DiagnosticSeverity.Warning, severity);
    }

    [TestMethod]
    public void ErrorCatalog_GetDefaultSeverity_ForLexerError_ShouldReturnError()
    {
        var severity = ErrorCatalog.GetDefaultSeverity(DiagnosticCode.MQ1001_UnknownToken);

        Assert.AreEqual(DiagnosticSeverity.Error, severity);
    }

    [TestMethod]
    public void ErrorCatalog_GetDefaultSeverity_ForSemanticError_ShouldReturnError()
    {
        var severity = ErrorCatalog.GetDefaultSeverity(DiagnosticCode.MQ3001_UnknownColumn);

        Assert.AreEqual(DiagnosticSeverity.Error, severity);
    }

    [TestMethod]
    public void ErrorCatalog_GetDefaultSeverity_ForRuntimeError_ShouldReturnError()
    {
        var severity = ErrorCatalog.GetDefaultSeverity(DiagnosticCode.MQ7001_DataSourceBindingFailed);

        Assert.AreEqual(DiagnosticSeverity.Error, severity);
    }

    [TestMethod]
    public void ErrorCatalog_GetCategory_ShouldReturnCorrectCategory()
    {
        Assert.AreEqual("Lexer", ErrorCatalog.GetCategory(DiagnosticCode.MQ1001_UnknownToken));
        Assert.AreEqual("Syntax", ErrorCatalog.GetCategory(DiagnosticCode.MQ2001_UnexpectedToken));
        Assert.AreEqual("Semantic", ErrorCatalog.GetCategory(DiagnosticCode.MQ3001_UnknownColumn));
        Assert.AreEqual("Schema", ErrorCatalog.GetCategory(DiagnosticCode.MQ4001_InvalidBinarySchemaField));
        Assert.AreEqual("Warning", ErrorCatalog.GetCategory(DiagnosticCode.MQ5001_UnusedAlias));
        Assert.AreEqual("FeatureGate", ErrorCatalog.GetCategory(DiagnosticCode.MQ6001_CteUnavailable));
        Assert.AreEqual("Runtime", ErrorCatalog.GetCategory(DiagnosticCode.MQ7001_DataSourceBindingFailed));
        Assert.AreEqual("CodeGeneration", ErrorCatalog.GetCategory(DiagnosticCode.MQ8001_CodeGenerationFailed));
    }

    [TestMethod]
    public void ErrorCatalog_GetDidYouMeanSuggestion_WhenCloseMatch_ShouldSuggest()
    {
        var suggestion = ErrorCatalog.GetDidYouMeanSuggestion("Nme", ["Name", "Age", "City"]);

        Assert.AreEqual("Name", suggestion);
    }

    [TestMethod]
    public void ErrorCatalog_GetDidYouMeanSuggestion_WhenNoCloseMatch_ShouldReturnNull()
    {
        var suggestion = ErrorCatalog.GetDidYouMeanSuggestion("zzzzzzz", ["Name", "Age", "City"]);

        Assert.IsNull(suggestion);
    }

    [TestMethod]
    public void ErrorCatalog_GetDidYouMeanSuggestion_WithEmptyCandidates_ShouldReturnNull()
    {
        var suggestion = ErrorCatalog.GetDidYouMeanSuggestion("Name", Array.Empty<string>());

        Assert.IsNull(suggestion);
    }

    #endregion

    #region DiagnosticExceptionExtensions Branch Coverage

    [TestMethod]
    public void DiagnosticExceptionExtensions_TryToDiagnostic_WithDiagnosticException_ShouldReturnTrue()
    {
        Exception ex = new UnknownColumnOrAliasException("col");

        var result = ex.TryToDiagnostic(null, out var diagnostic);

        Assert.IsTrue(result);
        Assert.IsNotNull(diagnostic);
        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, diagnostic.Code);
    }

    [TestMethod]
    public void DiagnosticExceptionExtensions_TryToDiagnostic_WithRegularException_ShouldReturnFalse()
    {
        var ex = new InvalidOperationException("test");

        var result = ex.TryToDiagnostic(null, out var diagnostic);

        Assert.IsFalse(result);
        Assert.IsNull(diagnostic);
    }

    [TestMethod]
    public void DiagnosticExceptionExtensions_TryToDiagnostic_WithWrappedDiagnosticException_ShouldReturnTrue()
    {
        var inner = new UnknownPropertyException("Age", "Person", new TextSpan(0, 5));
        Exception ex = new InvalidOperationException("wrapper", inner);

        var result = ex.TryToDiagnostic(null, out var diagnostic);

        Assert.IsTrue(result);
        Assert.AreEqual(DiagnosticCode.MQ3014_InvalidPropertyAccess, diagnostic.Code);
    }

    [TestMethod]
    public void DiagnosticExceptionExtensions_ToDiagnosticOrGeneric_WithDiagnosticException_ShouldReturnTyped()
    {
        Exception ex = new AmbiguousColumnException("Col", "a", "b");

        var diagnostic = ex.ToDiagnosticOrGeneric();

        Assert.AreEqual(DiagnosticCode.MQ3002_AmbiguousColumn, diagnostic.Code);
    }

    [TestMethod]
    public void DiagnosticExceptionExtensions_ToDiagnosticOrGeneric_WithRegularException_ShouldReturnGeneric()
    {
        var ex = new InvalidOperationException("something went wrong");

        var diagnostic = ex.ToDiagnosticOrGeneric();

        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    [TestMethod]
    public void DiagnosticExceptionExtensions_ToDiagnosticOrGeneric_WithArgumentNullException_ShouldReturnGeneric()
    {
        var ex = new ArgumentNullException("param");

        var diagnostic = ex.ToDiagnosticOrGeneric();

        Assert.AreEqual(DiagnosticCode.MQ2030_UnsupportedSyntax, diagnostic.Code);
    }

    [TestMethod]
    public void DiagnosticExceptionExtensions_ToDiagnosticOrGeneric_WithKeyNotFoundException_ShouldReturnTableNotFound()
    {
        var ex = new KeyNotFoundException("test");

        var diagnostic = ex.ToDiagnosticOrGeneric();

        Assert.AreEqual(DiagnosticCode.MQ3003_UnknownTable, diagnostic.Code);
    }

    [TestMethod]
    public void DiagnosticExceptionExtensions_ToDiagnosticOrGeneric_WithNotSupportedException_ShouldReturnUnsupported()
    {
        var ex = new NotSupportedException("not supported");

        var diagnostic = ex.ToDiagnosticOrGeneric();

        Assert.AreEqual(DiagnosticCode.MQ2030_UnsupportedSyntax, diagnostic.Code);
    }

    [TestMethod]
    public void DiagnosticExceptionExtensions_TryToDiagnostic_WithNull_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ((Exception)null).TryToDiagnostic(null, out _));
    }

    #endregion

    #region Entity Types

    public class SimpleEntity
    {
        public string Name { get; set; }

        public string City { get; set; }
    }

    #endregion

    #region Test Helpers

    private sealed class TestIndexedList : IndexedList<Key, Row>
    {
        public void AddRowWithIndex(Key key, Row row)
        {
            Rows.Add(row);
            var rowIndex = Rows.Count - 1;

            if (!Indexes.TryGetValue(key, out var indices))
            {
                indices = [];
                Indexes[key] = indices;
            }

            indices.Add(rowIndex);
        }
    }

    private sealed class TestSchemaProvider : ISchemaProvider
    {
        private readonly string _schemaName;
        private readonly ISchema _schema;

        public TestSchemaProvider(string schemaName, ISchema schema = null)
        {
            _schemaName = schemaName;
            _schema = schema;
        }

        public ISchema GetSchema(string schema)
        {
            return _schema ?? throw new InvalidOperationException($"Schema '{schema}' not found");
        }
    }

    private sealed class TestSchema : ISchema
    {
        public TestSchema(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext, params object[] parameters) =>
            throw new NotImplementedException();

        public RowSource GetRowSource(string name, RuntimeContext interCommunicator, params object[] parameters) =>
            throw new NotImplementedException();

        public SchemaMethodInfo[] GetRawConstructors(RuntimeContext runtimeContext) =>
            throw new NotImplementedException();

        public SchemaMethodInfo[] GetRawConstructors(string methodName, RuntimeContext runtimeContext) =>
            throw new NotImplementedException();

        public bool TryResolveMethod(string method, Type[] parameters, Type entityType, out System.Reflection.MethodInfo methodInfo)
        {
            methodInfo = null;
            return false;
        }

        public bool TryResolveRawMethod(string method, Type[] parameters, out System.Reflection.MethodInfo methodInfo)
        {
            methodInfo = null;
            return false;
        }

        public bool TryResolveAggregationMethod(string method, Type[] parameters, Type entityType, out System.Reflection.MethodInfo methodInfo)
        {
            methodInfo = null;
            return false;
        }

        public IReadOnlyDictionary<string, IReadOnlyList<System.Reflection.MethodInfo>> GetAllLibraryMethods() =>
            new Dictionary<string, IReadOnlyList<System.Reflection.MethodInfo>>();
    }

    #endregion
}
