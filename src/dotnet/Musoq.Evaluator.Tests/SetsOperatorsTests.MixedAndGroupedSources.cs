using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SetsOperatorsTests
{

    [TestMethod]
    public void MixedMultipleSourcesTest()
    {
        var query =
            @"
select Name from #A.Entities() union (Name)
select Name from #B.Entities() except (Name)
select Name from #C.Entities() intersect (Name)
select Name from #D.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("007"), new BasicEntity("008")
                ]
            },
            { "#B", [new BasicEntity("003"), new BasicEntity("007"), new BasicEntity("001")] },
            { "#C", [new BasicEntity("005"), new BasicEntity("007")] },
            { "#D", [new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("003")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["001"], ["002"], ["003"]);
    }

}
