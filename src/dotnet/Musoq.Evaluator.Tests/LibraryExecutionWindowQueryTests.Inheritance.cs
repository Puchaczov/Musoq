using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionWindowQueryTests
{
    [TestMethod]
    public void StandardSqlName_WhenLibraryAddsMultipleInputs_ShouldBindItsDeclaredSignature()
    {
        const string query = "select Position, Sum(Amount, 2) over (partition by Partition order by Position rows between 1 preceding and current row) as Total from #schema.first() order by Position";
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionEntity, InvoiceWindowLibrary>(query,
            LibraryExecutionFixture.CreateRows());
        using var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("Position", typeof(int)), ("Total", typeof(decimal?)));
        Assert.HasCount(5, rows);
        decimal?[] expected = [2.5m, 8m, 5.5m, -6m, 4m];
        for (var index = 0; index < rows.Length; index++)
        {
            Assert.AreEqual(index + 1, rows[index].GetValue<int>(0));
            Assert.AreEqual(expected[index], rows[index].GetValue<decimal?>(1));
        }
    }

    [TestMethod]
    public void WindowDeclarations_WhenDescendantOverridesOneSignature_ShouldRetainOtherInheritedTypes()
    {
        const string query = "select Position, Sum(Amount) over (partition by Partition order by Position) as DecimalTotal, Sum(Integral) over (partition by Partition order by Position) as IntegerTotal from #schema.first() order by Position";
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionEntity, InvoiceWindowLibrary>(query,
            LibraryExecutionFixture.CreateRows());
        using var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("Position", typeof(int)),
            ("DecimalTotal", typeof(decimal?)), ("IntegerTotal", typeof(int?)));
        Assert.HasCount(5, rows);
        decimal?[] decimals = [101.25m, 104m, 104m, 97m, 102m];
        int?[] integers = [1, 3, 3, -3, 2];
        for (var index = 0; index < rows.Length; index++)
        {
            Assert.AreEqual(index + 1, rows[index].GetValue<int>(0));
            Assert.AreEqual(decimals[index], rows[index].GetValue<decimal?>(1));
            Assert.AreEqual(integers[index], rows[index].GetValue<int?>(2));
        }
    }

    [TestMethod]
    public void WindowDeclaration_WhenSqlAliasDiffersFromClrName_ShouldInvokeRegisteredOperations()
    {
        const string query = "select Position, InvoiceTotal(Amount) over () as Total from #schema.first() order by Position";
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionEntity, InvoiceWindowLibrary>(query,
            LibraryExecutionFixture.CreateRows());
        using var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("Position", typeof(int)), ("Total", typeof(decimal?)));
        Assert.HasCount(5, rows);
        for (var index = 0; index < rows.Length; index++)
        {
            Assert.AreEqual(index + 1, rows[index].GetValue<int>(0));
            Assert.AreEqual(106m, rows[index].GetValue<decimal?>(1));
        }
    }

    public sealed class InvoiceWindowLibrary : LibraryBase
    {
        [WindowFunction(typeof(LibraryExecutionLibrary.ScaledTotalKernel), WindowEvaluationModes.WholePartition |
            WindowEvaluationModes.Running | WindowEvaluationModes.RecomputeFrames, AggregateEmptyResultBehavior.Null,
            Name = "Sum")]
        public decimal? SumScaled(decimal? value, int factor) => AggregateFunction.NotInvoked<decimal?>();

        [WindowFunction(typeof(InvoiceTotalKernel), WindowEvaluationModes.WholePartition |
            WindowEvaluationModes.Running | WindowEvaluationModes.RecomputeFrames, AggregateEmptyResultBehavior.Null)]
        public new decimal? Sum(decimal? value, [AggregateParent] int parent = 0)
            => AggregateFunction.NotInvoked<decimal?>();

        [WindowFunction(typeof(InvoiceTotalKernel), WindowEvaluationModes.WholePartition,
            AggregateEmptyResultBehavior.Null, Name = "InvoiceTotal")]
        public decimal? TotalAmount(decimal? value) => AggregateFunction.NotInvoked<decimal?>();
    }

    public static class InvoiceTotalKernel
    {
        public struct State { public bool HasValue; public decimal Total; }
        public static void Reset(ref State state) => state = new State { Total = 100m };
        public static void Set(ref State state, decimal? value)
        {
            if (!value.HasValue)
                return;
            state.Total = checked(state.Total + value.Value);
            state.HasValue = true;
        }
        public static decimal? Get(in State state) => state.HasValue ? state.Total : null;
    }
}
