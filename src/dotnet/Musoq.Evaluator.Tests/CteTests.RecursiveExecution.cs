using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class CteTests
{
    public static IEnumerable<object[]> RecursiveUnionAllCases =>
        RecursiveCteSupportedCaseCatalog.Cases.Select(static item => new object[] { item });

    [TestMethod]
    [DynamicData(nameof(RecursiveUnionAllCases))]
    public void RecursiveUnionAllSupportedCase_ShouldReturnDeclaredColumnsAndRows(
        RecursiveCteSupportedCase testCase)
    {
        var vm = testCase.CreateSchemaProvider == null
            ? CreateAndRunVirtualMachine(
                testCase.Query,
                CreateSingleSource(),
                testCase.CompilationOptions)
            : InstanceCreator.CompileForExecution(
                testCase.Query,
                Guid.NewGuid().ToString(),
                testCase.CreateSchemaProvider(),
                LoggerResolver,
                testCase.CompilationOptions);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            testCase.ExpectedColumns
                .Select(static column => (column.Name, column.ClrType))
                .ToArray());

        if (testCase.Ordered)
            TableMaterializationTestHelper.AssertRowsInOrder(table, testCase.ExpectedRows.ToArray());
        else
            TableMaterializationTestHelper.AssertRowsUnordered(table, testCase.ExpectedRows.ToArray());
    }

    [TestMethod]
    public void RecursiveUnionAll_WhenIterationLimitIsReached_ShouldReportMq7007()
    {
        var testCase = RecursiveCteSupportedCaseCatalog.GetBySampleName("Q188_RecursiveUnionAllCounter");
        var options = testCase.CompilationOptions.WithRecursiveCteLimits(new(2, 100));
        var vm = CreateAndRunVirtualMachine(testCase.Query, CreateSingleSource(), options);

        var exception = Assert.ThrowsExactly<RecursiveCteLimitExceededException>(
            () => TableMaterializationTestHelper.Materialize(vm.Run(TestContext.CancellationToken)));

        Assert.AreEqual(DiagnosticCode.MQ7007_RecursiveCteIterationLimitExceeded, exception.Code);
        Assert.AreEqual(2, exception.ConfiguredLimit);
        Assert.AreEqual("counter", exception.CteName);
    }

    [TestMethod]
    public void RecursiveUnionAll_WhenRowLimitIsReached_ShouldReportMq7008()
    {
        var testCase = RecursiveCteSupportedCaseCatalog.GetBySampleName("Q188_RecursiveUnionAllCounter");
        var options = testCase.CompilationOptions.WithRecursiveCteLimits(new(100, 3));
        var vm = CreateAndRunVirtualMachine(testCase.Query, CreateSingleSource(), options);

        var exception = Assert.ThrowsExactly<RecursiveCteLimitExceededException>(
            () => TableMaterializationTestHelper.Materialize(vm.Run(TestContext.CancellationToken)));

        Assert.AreEqual(DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, exception.Code);
        Assert.AreEqual(3, exception.ConfiguredLimit);
        Assert.AreEqual("counter", exception.CteName);
    }

    [TestMethod]
    public void RecursiveUnionAll_WhenCanceledBeforeExecution_ShouldStop()
    {
        var testCase = RecursiveCteSupportedCaseCatalog.GetBySampleName("Q188_RecursiveUnionAllCounter");
        var vm = CreateAndRunVirtualMachine(
            testCase.Query,
            CreateSingleSource(),
            testCase.CompilationOptions);
        using var cancellation = new System.Threading.CancellationTokenSource();
        cancellation.Cancel();

        Assert.ThrowsExactly<OperationCanceledException>(() => vm.Run(cancellation.Token));
    }

    [TestMethod]
    public void RecursiveUnionAllInspection_ShouldExposeDedicatedFrontiersAndFixedPointLoop()
    {
        var testCase = RecursiveCteSupportedCaseCatalog.GetBySampleName("Q188_RecursiveUnionAllCounter");
        var inspection = InstanceCreator.CompileForInspection(
            testCase.Query,
            Guid.NewGuid().ToString(),
            new Schema.Basic.BasicSchemaProvider<BasicEntity>(CreateSingleSource()),
            LoggerResolver,
            testCase.CompilationOptions);

        Assert.Contains("RecursiveCte [counter; result cte0; frontiers cte0CurrentFrontier, cte0NextFrontier", inspection.ExecutionPlanText);
        Assert.Contains("Anchor", inspection.ExecutionPlanText);
        Assert.Contains("RecursiveMember", inspection.ExecutionPlanText);
        Assert.Contains("RecursiveAppend", inspection.ExecutionPlanText);
        Assert.IsFalse(inspection.ExecutionPlanText.Contains("CreateTable [cte0CurrentFrontier", StringComparison.Ordinal));
        Assert.IsFalse(inspection.ExecutionPlanText.Contains("CreateTable [cte0NextFrontier", StringComparison.Ordinal));

        Assert.AreEqual(3, CountOccurrences(inspection.GeneratedCSharpCode, "new List<Cte0Row0>"));
        Assert.AreEqual(1, CountOccurrences(inspection.GeneratedCSharpCode, "while (cte0CurrentFrontier.Count > 0)"));
        Assert.Contains("cte0NextFrontier.Clear();", inspection.GeneratedCSharpCode);
        Assert.Contains("token.ThrowIfCancellationRequested();", inspection.GeneratedCSharpCode);
        Assert.Contains("MQ7007_RecursiveCteIterationLimitExceeded", inspection.GeneratedCSharpCode);
        Assert.Contains("MQ7008_RecursiveCteRowLimitExceeded", inspection.GeneratedCSharpCode);
        Assert.IsFalse(inspection.GeneratedCSharpCode.Contains("new HashSet<", StringComparison.Ordinal));
        Assert.IsFalse(inspection.GeneratedCSharpCode.Contains(".Select(", StringComparison.Ordinal));
        Assert.IsFalse(inspection.GeneratedCSharpCode.Contains(".Where(", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RecursiveKeyedUnion_WhenSameGenerationHasDifferentPayloads_ShouldKeepOneRepresentative()
    {
        const string prefix =
            "with recursive paths (Id, Path) as (" +
            "select Id, Path from values {{ Id: 1, Path: 'left' }, { Id: 2, Path: 'right' }} seed " +
            "union (Id) select 3, p.Path + '->3' from paths p where p.Id < 3) ";
        var ascending = CreateAndRunVirtualMachine(
                prefix + "select Id, Path from paths order by Id",
                CreateSingleSource())
            .Run(TestContext.CancellationToken);
        var descending = CreateAndRunVirtualMachine(
                prefix + "select Id, Path from paths order by Id desc",
                CreateSingleSource())
            .Run(TestContext.CancellationToken);

        var ascendingRepresentative = ascending.Single(row => (int)row.Values[0] == 3).Values[1];
        var descendingRepresentative = descending.Single(row => (int)row.Values[0] == 3).Values[1];

        Assert.Contains(ascendingRepresentative, new object[] { "left->3", "right->3" });
        Assert.AreEqual(ascendingRepresentative, descendingRepresentative);
    }

    [TestMethod]
    public void RecursiveUnion_WhenDuplicatesAreRejected_ShouldNotConsumeRowLimit()
    {
        foreach (var sampleName in new[]
                 {
                     "Q196_RecursiveAnchorDuplicates",
                     "Q197_RecursiveDuplicateEdges"
                 })
        {
            var testCase = RecursiveCteSupportedCaseCatalog.GetBySampleName(sampleName);
            var options = testCase.CompilationOptions.WithRecursiveCteLimits(
                new(100, testCase.ExpectedRows.Count));
            var table = CreateAndRunVirtualMachine(testCase.Query, CreateSingleSource(), options)
                .Run(TestContext.CancellationToken);

            Assert.HasCount(testCase.ExpectedRows.Count, table, sampleName);
        }
    }

    [TestMethod]
    public void RecursiveKeyedUnion_WhenColumnsBeforeIdentityArePruned_ShouldUseRemappedOrdinal()
    {
        const string query =
            "with recursive counter (Unused, Id) as (" +
            "select 'anchor', Value from values {{ Value: 1 }} seed union (Id) " +
            "select 'member', c.Id + 1 from counter c where c.Id < 3) " +
            "select Id from counter order by Id";

        var table = CreateAndRunVirtualMachine(query, CreateSingleSource())
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Id", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1], [2], [3]);
    }

    private static int CountOccurrences(string text, string value)
    {
        return text.Split(value, StringSplitOptions.None).Length - 1;
    }
}
