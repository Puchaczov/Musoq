using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class ProfiledQueryPipelineIntegrationTests : BasicEntityTestBase
{
    [TestMethod]
    public void RunWithProfile_WhenSimpleSelectStreamsFinalShapes_ShouldMaterializeRowsAndRecordProfile()
    {
        const string query = "select Name, Population from #A.Entities() where Population > 0";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = "A", Population = 10 },
                    new BasicEntity { Name = "B", Population = 0 },
                    new BasicEntity { Name = "C", Population = 20 }
                ]
            }
        };
        var vm = CreateAndRunVirtualMachine(
            query,
            sources,
            TestCompilationOptions.WithInstrumentationMode(QueryInstrumentationMode.Full));

        var profileResult = vm.RunWithProfile(CancellationToken.None);

        Assert.AreEqual(2, profileResult.Result.Count);
        Assert.AreEqual("A", profileResult.Result[0][0]);
        Assert.AreEqual(10m, profileResult.Result[0][1]);
        Assert.AreEqual("C", profileResult.Result[1][0]);
        Assert.AreEqual(20m, profileResult.Result[1][1]);

        var source = profileResult.Profile.Sources.Single();
        Assert.AreEqual(3, source.RowsRead);

        var appendShape = profileResult.Profile.Operators.Single(operation => operation.Name == "AppendShape");
        Assert.AreEqual(2, appendShape.OutputRows);
        Assert.AreEqual(0, appendShape.ExceptionCount);
    }

    [TestMethod]
    public void RunWithProfile_WithPerRunQueryProgressOptions_ShouldPublishFinalSnapshot()
    {
        const string query = "select Name, Population from #A.Entities() where Population > 0";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = "A", Population = 10 },
                    new BasicEntity { Name = "B", Population = 20 }
                ]
            }
        };
        var vm = CreateAndRunVirtualMachine(
            query,
            sources,
            TestCompilationOptions.WithInstrumentationMode(QueryInstrumentationMode.Full));
        var snapshots = new List<QueryProgressEventArgs>();
        vm.QueryProgress += (_, args) => snapshots.Add(args);

        var profileResult = vm.RunWithProfile(new QueryProgressOptions
        {
            RowsPerUpdate = 1,
            MinimumInterval = TimeSpan.FromDays(1)
        });

        Assert.AreEqual(2, profileResult.Result.Count);
        Assert.IsTrue(snapshots.Count > 0);
        Assert.IsTrue(snapshots[^1].IsFinal);
        Assert.AreEqual(2, snapshots[^1].QueryRowsProcessed);
    }
}
