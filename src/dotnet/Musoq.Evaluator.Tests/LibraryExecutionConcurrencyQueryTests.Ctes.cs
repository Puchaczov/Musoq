using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionConcurrencyQueryTests
{
    [TestMethod]
    [DataRow(SourceExecutionPermissions.None)]
    [DataRow(SourceExecutionPermissions.ConcurrentAccess)]
    [DataRow(SourceExecutionPermissions.ConcurrentRowProcessing)]
    [DataRow(SourceExecutionPermissions.ConcurrentAccess | SourceExecutionPermissions.ConcurrentRowProcessing)]
    public void IndependentCteQuery_WhenOnlyOneSourcePermissionIsGranted_ShouldRemainSerial(SourceExecutionPermissions permissions)
    {
        var concurrent = permissions == (SourceExecutionPermissions.ConcurrentAccess | SourceExecutionPermissions.ConcurrentRowProcessing);
        using var probe = new ConcurrencyProbe(concurrent);
        var provider = CreateProvider(probe, true, permissions);
        const string query = "with a as (select PermittedWork(Probe, Position) as Value from #schema.first() where Position = 0), " +
                             "b as (select PermittedWork(Probe, Position) as Value from #schema.first() where Position = 0) " +
                             "select a.Value as FirstValue, b.Value as SecondValue from a inner join b on a.Value = b.Value";
        var options = new CompilationOptions(ParallelizationMode.Full, usePrimitiveTypeValidation: false,
            useCteParallelization: true, maxDegreeOfParallelismOverride: 2, forceTableResultMaterialization: true);
        var inspection = InstanceCreator.CompileForInspection(query, Guid.NewGuid().ToString("N"), provider, _logger, options);
        Assert.AreEqual(concurrent, inspection.ExecutionPlanText.Contains("ParallelBlock", StringComparison.Ordinal), inspection.PlanningText);
        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString("N"), provider, _logger, options);
        using var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();
        TableMaterializationTestHelper.AssertColumns(result, ("FirstValue", typeof(int)), ("SecondValue", typeof(int)));
        Assert.HasCount(1, rows);
        Assert.AreEqual(1, rows[0].GetValue<int>(0));
        Assert.AreEqual(1, rows[0].GetValue<int>(1));
        Assert.AreEqual(2, probe.Calls);
        Assert.AreEqual(concurrent ? 2 : 1, probe.MaxConcurrent);
    }

    [TestMethod]
    public void WindowCteQuery_WhenOnlyAggregateDeclarationPermitsConcurrency_ShouldRemainSerial()
    {
        using var probe = new ConcurrencyProbe(false);
        var provider = CreateProvider(probe, true);
        const string query = "with a as (select PermittedCount(Position) over () as Total from #schema.first() where Position = 0), " +
                             "b as (select PermittedCount(Position) over () as Total from #schema.first() where Position = 0) " +
                             "select a.Total as FirstCount, b.Total as SecondCount from a inner join b on a.Total = b.Total";
        var options = new CompilationOptions(ParallelizationMode.Full, usePrimitiveTypeValidation: false,
            useCteParallelization: true, maxDegreeOfParallelismOverride: 2, forceTableResultMaterialization: true);
        var inspection = InstanceCreator.CompileForInspection(query, Guid.NewGuid().ToString("N"), provider, _logger, options);
        Assert.DoesNotContain("ParallelBlock", inspection.ExecutionPlanText);
        Assert.Contains("does not permit concurrent independent partitions", inspection.PlanningText);
        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString("N"), provider, _logger, options);
        using var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();
        TableMaterializationTestHelper.AssertColumns(result, ("FirstCount", typeof(long)), ("SecondCount", typeof(long)));
        Assert.HasCount(1, rows);
        Assert.AreEqual(1L, rows[0].GetValue<long>(0));
        Assert.AreEqual(1L, rows[0].GetValue<long>(1));
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public void IndependentCteQuery_WhenAccessPermissionsIntersect_ShouldScheduleOnlyAuthorizedBranches(bool sourceAllowed, bool functionAllowed)
    {
        var concurrent = sourceAllowed && functionAllowed;
        using var probe = new ConcurrencyProbe(concurrent);
        var provider = CreateProvider(probe, sourceAllowed,
            sourceAllowed ? SourceExecutionPermissions.ConcurrentAccess | SourceExecutionPermissions.ConcurrentRowProcessing : SourceExecutionPermissions.None);
        var function = functionAllowed ? "PermittedWork" : "SerialWork";
        var query = $"with a as (select {function}(Probe, Position) as Value from #schema.first() where Position = 0), " +
                    $"b as (select {function}(Probe, Position) as Value from #schema.first() where Position = 0) " +
                    "select a.Value as FirstValue, b.Value as SecondValue from a inner join b on a.Value = b.Value";
        var options = new CompilationOptions(ParallelizationMode.Full, usePrimitiveTypeValidation: false,
            useCteParallelization: true, maxDegreeOfParallelismOverride: 2, forceTableResultMaterialization: true);
        var inspection = InstanceCreator.CompileForInspection(query, Guid.NewGuid().ToString("N"), provider, _logger, options);
        Assert.AreEqual(concurrent, inspection.ExecutionPlanText.Contains("ParallelBlock", StringComparison.Ordinal), inspection.PlanningText);
        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString("N"), provider, _logger, options);
        using var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();
        TableMaterializationTestHelper.AssertColumns(result, ("FirstValue", typeof(int)), ("SecondValue", typeof(int)));
        Assert.HasCount(1, rows);
        Assert.AreEqual(1, rows[0].GetValue<int>(0));
        Assert.AreEqual(1, rows[0].GetValue<int>(1));
        Assert.AreEqual(2, probe.Calls);
        Assert.AreEqual(concurrent ? 2 : 1, probe.MaxConcurrent);
    }
}
