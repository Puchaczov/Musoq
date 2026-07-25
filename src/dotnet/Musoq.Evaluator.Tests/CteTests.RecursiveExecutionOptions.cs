using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class CteTests
{
    private static readonly RecursiveOptionProfile[] RecursiveOptionProfiles =
    [
        new("hash-only", UseHashJoin: true, UseSortMergeJoin: false),
        new("sort-merge-only", UseHashJoin: false, UseSortMergeJoin: true),
        new("nested-loop", UseHashJoin: false, UseSortMergeJoin: false),
        new("cse-disabled", UseCommonSubexpressionElimination: false),
        new("sidecars-disabled", UseCteSidecarIndexes: false),
        new("source-instrumentation", InstrumentationMode: QueryInstrumentationMode.SourceBoundaries),
        new("full-instrumentation", InstrumentationMode: QueryInstrumentationMode.Full),
        new("cte-parallelization-disabled", UseCteParallelization: false),
        new("sequential", ParallelizationMode: ParallelizationMode.None)
    ];

    public static IEnumerable<object[]> RecursiveSupportedOptionCases =>
        from testCase in RecursiveCteSupportedCaseCatalog.Cases
        from profile in RecursiveOptionProfiles
        select new object[] { testCase, profile };

    [TestMethod]
    [DynamicData(nameof(RecursiveSupportedOptionCases))]
    public void RecursiveSupportedCase_AcrossCompatibleOptimizerModes_ShouldReturnDeclaredResult(
        RecursiveCteSupportedCase testCase,
        RecursiveOptionProfile profile)
    {
        var options = ApplyProfile(testCase.CompilationOptions, profile);
        var vm = testCase.CreateSchemaProvider == null
            ? CreateAndRunVirtualMachine(testCase.Query, CreateSingleSource(), options)
            : InstanceCreator.CompileForExecution(
                testCase.Query,
                Guid.NewGuid().ToString(),
                testCase.CreateSchemaProvider(),
                LoggerResolver,
                options);
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

    private static CompilationOptions ApplyProfile(
        CompilationOptions baseline,
        RecursiveOptionProfile profile)
    {
        return new CompilationOptions(
                profile.ParallelizationMode ?? baseline.ParallelizationMode,
                profile.UseHashJoin ?? baseline.UseHashJoin,
                profile.UseSortMergeJoin ?? baseline.UseSortMergeJoin,
                profile.UseCommonSubexpressionElimination ?? baseline.UseCommonSubexpressionElimination,
                baseline.UseConstantFolding,
                baseline.UsePrimitiveTypeValidation,
                profile.UseCteParallelization ?? baseline.UseCteParallelization,
                profile.UseCteSidecarIndexes ?? baseline.UseCteSidecarIndexes,
                baseline.SourceRuntimeSettingsResolver,
                profile.InstrumentationMode ?? baseline.InstrumentationMode,
                baseline.MaxDegreeOfParallelismOverride,
                baseline.ForceTableResultMaterialization)
            .WithRecursiveCteLimits(baseline.RecursiveCteLimits);
    }

    public sealed record RecursiveOptionProfile(
        string Name,
        ParallelizationMode? ParallelizationMode = null,
        bool? UseHashJoin = null,
        bool? UseSortMergeJoin = null,
        bool? UseCommonSubexpressionElimination = null,
        bool? UseCteParallelization = null,
        bool? UseCteSidecarIndexes = null,
        QueryInstrumentationMode? InstrumentationMode = null)
    {
        public override string ToString() => Name;
    }
}
