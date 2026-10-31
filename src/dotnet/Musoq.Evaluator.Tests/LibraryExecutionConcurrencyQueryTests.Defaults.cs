using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionConcurrencyQueryTests
{
    [TestMethod]
    [DataRow("DefaultCount", true)]
    [DataRow("ExplicitSerialCount", false)]
    [DataRow("DescendantCount", false)]
    public void CustomCountQuery_WhenLibraryDefaultsAreDeclared_ShouldRespectMethodOwnershipAndExplicitNone(string function, bool parallel)
    {
        var provider = LibraryExecutionFixture.CreateProvider<LibraryExecutionEntity, DescendantDefaultsLibrary>(
            LibraryExecutionFixture.CreateRows(), SourceExecutionPermissions.ConcurrentRowProcessing);
        var query = $"select Partition, {function}(Integral) as Total from #schema.first() group by Partition";
        var inspection = InstanceCreator.CompileForInspection(query, Guid.NewGuid().ToString("N"), provider, _logger, ParallelOptions);
        Assert.AreEqual(parallel, inspection.GeneratedCSharpCode.Contains("ParallelSingleKeyAggregateChunkWorker_", StringComparison.Ordinal), inspection.PlanningText);
        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString("N"), provider, _logger, ParallelOptions);
        using var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();
        TableMaterializationTestHelper.AssertColumns(result, ("Partition", typeof(string)), ("Total", typeof(long)));
        Assert.HasCount(2, rows);
        var totals = rows.ToDictionary(static row => row.GetValue<string>(0), static row => row.GetValue<long>(1));
        Assert.AreEqual(2L, totals["A"]);
        Assert.AreEqual(2L, totals["B"]);
    }

    [LibraryExecutionDefaults(AggregatePermissions = FunctionExecutionPermissions.ConcurrentInvocations |
        FunctionExecutionPermissions.IndependentGroups | FunctionExecutionPermissions.EquivalentPartialMerge)]
    public class PermittedDefaultsLibrary : LibraryBase
    {
        [AggregateFunction(typeof(CountNullableAggregateKernel<int>))]
        public long DefaultCount(int? value) => AggregateFunction.NotInvoked<long>();

        [AggregateFunction(typeof(CountNullableAggregateKernel<int>), ExecutionPermissions = FunctionExecutionPermissions.None)]
        public long ExplicitSerialCount(int? value) => AggregateFunction.NotInvoked<long>();
    }

    public sealed class DescendantDefaultsLibrary : PermittedDefaultsLibrary
    {
        [AggregateFunction(typeof(CountNullableAggregateKernel<int>))]
        public long DescendantCount(int? value) => AggregateFunction.NotInvoked<long>();
    }
}
