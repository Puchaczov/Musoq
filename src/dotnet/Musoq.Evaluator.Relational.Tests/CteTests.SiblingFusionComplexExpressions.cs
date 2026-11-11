using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class CteTests
{
    [TestMethod]
    public void SiblingFusion_WithComplexExpressionsAndSidecarConsumers_ShouldFuseAndExecute()
    {
        const string query = (
            "\n"
            + "with raw as (\n"
            + "    select a.Id, a.Name, a.City, a.Country, a.Population, a.Array[1] as SecondValue\n"
            + "    from #A.entities() a\n"
            + "), names as (\n"
            + "    select\n"
            + "        Id,\n"
            + "        case when Name is null then Country else Name end as Label,\n"
            + "        SecondValue,\n"
            + "        Population::int as PopulationValue\n"
            + "    from raw\n"
            + "    where Id > 0\n"
            + "), eligible as (\n"
            + "    select Id\n"
            + "    from raw\n"
            + "    where Population > 0\n"
            + ")\n"
            + "select b.Name, n.Label, n.SecondValue, n.PopulationValue\n"
            + "from #B.entities() b\n"
            + "inner join names n on b.Id = n.Id\n"
            + "semi join eligible e on b.Id = e.Id"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity("42")
                {
                    Id = 1,
                    City = "Warsaw",
                    Country = "Poland",
                    Population = 100m,
                },
            ],
            ["#B"] = [new BasicEntity("42") { Id = 1 }],
        };
        var options = new CompilationOptions(
            useHashJoin: true,
            useSortMergeJoin: false,
            useCteParallelization: false,
            useCteSidecarIndexes: true
        );
        var provider = new BasicSchemaProvider<BasicEntity>(sources);

        var inspection = InstanceCreator.CompileForInspection(
            query,
            Guid.NewGuid().ToString(),
            provider,
            new TestsLoggerResolver(),
            options
        );
        Assert.Contains("FusedCteProducer [cte1 -> sidecar-only, cte2 -> sidecar-only]", inspection.ExecutionPlanText);
        Assert.Contains("HashAdd", inspection.ExecutionPlanText);
        Assert.Contains("KeySetAdd", inspection.ExecutionPlanText);

        var compiled = CreateAndRunVirtualMachine(query, sources, options);
        var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Label", typeof(string)),
            ("SecondValue", typeof(int)),
            ("PopulationValue", typeof(int?))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["42", "42", 1, 100]);
    }
}
