using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class AsOfJoinTests
{
    [TestMethod]
    public void WhenAsOfJoinWithPartitionColumn_ShouldCorrelatePerService()
    {
        var query = @"
select
    errors.Name,
    errors.Time,
    deploys.Name,
    deploys.Time
from #A.entities() errors
asof join #B.entities() deploys on errors.Country = deploys.Country and errors.Time >= deploys.Time";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "Error-Auth", Country = "auth-svc", Time = new DateTime(2025, 3, 10, 14, 30, 0) },
                    new BasicEntity { Name = "Error-Pay", Country = "pay-svc", Time = new DateTime(2025, 3, 10, 15, 0, 0) },
                    new BasicEntity { Name = "Error-Auth2", Country = "auth-svc", Time = new DateTime(2025, 3, 10, 10, 0, 0) }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "Deploy-Auth-v2", Country = "auth-svc", Time = new DateTime(2025, 3, 10, 14, 0, 0) },
                    new BasicEntity { Name = "Deploy-Auth-v1", Country = "auth-svc", Time = new DateTime(2025, 3, 10, 9, 0, 0) },
                    new BasicEntity { Name = "Deploy-Pay-v1", Country = "pay-svc", Time = new DateTime(2025, 3, 10, 12, 0, 0) }
                ]
            }
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("errors.Name", typeof(string)),
            ("errors.Time", typeof(DateTime)),
            ("deploys.Name", typeof(string)),
            ("deploys.Time", typeof(DateTime)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Error-Auth", new DateTime(2025, 3, 10, 14, 30, 0), "Deploy-Auth-v2", new DateTime(2025, 3, 10, 14, 0, 0)],
            ["Error-Auth2", new DateTime(2025, 3, 10, 10, 0, 0), "Deploy-Auth-v1", new DateTime(2025, 3, 10, 9, 0, 0)],
            ["Error-Pay", new DateTime(2025, 3, 10, 15, 0, 0), "Deploy-Pay-v1", new DateTime(2025, 3, 10, 12, 0, 0)]);
    }
}
