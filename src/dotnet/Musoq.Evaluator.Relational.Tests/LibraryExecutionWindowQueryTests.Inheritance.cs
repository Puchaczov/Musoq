using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using static Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTestsFixtures;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionWindowQueryTests
{
    [TestMethod]
    public void StandardSqlName_WhenLibraryAddsMultipleInputs_ShouldBindItsDeclaredSignature()
    {
        const string query =
            "select Position, Sum(Amount, 2) over (partition by Partition order by Position rows between 1 preceding and current row) as Total from #schema.first() order by Position";
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionEntity, InvoiceWindowLibrary>(
            query,
            LibraryExecutionFixture.CreateRows()
        );
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
        const string query =
            "select Position, Sum(Amount) over (partition by Partition order by Position) as DecimalTotal, Sum(Integral) over (partition by Partition order by Position) as IntegerTotal from #schema.first() order by Position";
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionEntity, InvoiceWindowLibrary>(
            query,
            LibraryExecutionFixture.CreateRows()
        );
        using var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(
            result,
            ("Position", typeof(int)),
            ("DecimalTotal", typeof(decimal?)),
            ("IntegerTotal", typeof(int?))
        );
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
        const string query =
            "select Position, InvoiceTotal(Amount) over () as Total from #schema.first() order by Position";
        using var compiled = CreateAndRunVirtualMachine<LibraryExecutionEntity, InvoiceWindowLibrary>(
            query,
            LibraryExecutionFixture.CreateRows()
        );
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
}
