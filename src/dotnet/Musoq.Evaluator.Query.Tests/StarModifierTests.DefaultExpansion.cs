using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class StarModifierTests
{
    [TestMethod]
    public void WhenStarNoModifiers_ShouldExpandNormally()
    {
        const string query = "select * from #A.entities() a";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>> { { "#A", [new BasicEntity("january", 50m)] } };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(9, table.Columns.Count());
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("City", typeof(string)),
            ("Country", typeof(string)),
            ("Population", typeof(decimal)),
            ("Money", typeof(decimal)),
            ("Month", typeof(string)),
            ("Time", typeof(System.DateTime)),
            ("Id", typeof(int)),
            ("NullableValue", typeof(int?))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["", "", null, 0m, 50m, "january", default(System.DateTime), 0, null]
        );
    }
}
