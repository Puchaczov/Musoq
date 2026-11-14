using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SpecExplorationCoreLanguageTests
{
    [TestMethod]
    public void Spec_Select_Star_ShouldExpandAllColumns()
    {
        var query = "select * from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>> { { "#A", [new BasicEntity("Alice")] } };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.IsGreaterThan(1, table.Columns.Count(), "Star should expand to multiple columns");
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
            ["Alice", "", null, 0m, 0m, "", default(System.DateTime), 0, null]
        );
    }
}
