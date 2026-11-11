using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using static Musoq.Evaluator.Tests.LibraryExecutionConcurrencyQueryTestsFixtures;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed partial class LibraryExecutionConcurrencyQueryTests : MSTestContextTestBase
{
    private readonly TestsLoggerResolver _logger = new();

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public void ScalarQuery_WhenPermissionsIntersect_ShouldUseOnlyAuthorizedConcurrency(
        bool sourceAllowed,
        bool functionAllowed
    )
    {
        var concurrent = sourceAllowed && functionAllowed;
        using var probe = new ConcurrencyProbe(concurrent);
        var provider = CreateProvider(probe, sourceAllowed);
        var function = functionAllowed ? "PermittedWork" : "SerialWork";
        var query = $"select Position, {function}(Probe, Position) as Value from #schema.first()";
        var inspection = InstanceCreator.CompileForInspection(
            query,
            Guid.NewGuid().ToString("N"),
            provider,
            _logger,
            ParallelOptions
        );
        Assert.AreEqual(
            concurrent,
            inspection.GeneratedCSharpCode.Contains("ProjectChunkedRowsParallel<", StringComparison.Ordinal),
            inspection.PlanningText
        );
        using var compiled = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString("N"),
            provider,
            _logger,
            ParallelOptions
        );
        using var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("Position", typeof(int)), ("Value", typeof(int)));
        Assert.HasCount(4096, rows);
        for (var index = 0; index < rows.Length; index++)
        {
            Assert.AreEqual(index, rows[index].GetValue<int>(0));
            Assert.AreEqual(index + 1, rows[index].GetValue<int>(1));
        }
        Assert.AreEqual(4096, probe.Calls);
        Assert.AreEqual(concurrent ? 2 : 1, probe.MaxConcurrent);
        if (!sourceAllowed)
            Assert.Contains("does not explicitly permit ConcurrentRowProcessing", inspection.PlanningText);
        else if (!functionAllowed)
            Assert.Contains("does not explicitly permit concurrent invocations", inspection.PlanningText);
    }
}
