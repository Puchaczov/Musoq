using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class ValuesFromTests
{
    [TestMethod]
    public void ValuesSource_InnerJoinWithSourceLocalOnFilter_ShouldMaterializeFilteredRows()
    {
        const string query = (
            "select leftRows.Id, rightRows.Value\n"
            + "from values {\n"
            + "    ( Id: 1 ),\n"
            + "    ( Id: 2 )\n"
            + "} leftRows\n"
            + "inner join values {\n"
            + "    ( Id: 1, Value: 'keep', Enabled: true ),\n"
            + "    ( Id: 2, Value: 'skip', Enabled: false )\n"
            + "} rightRows on leftRows.Id = rightRows.Id and rightRows.Enabled = true\n"
            + "order by leftRows.Id"
        );
        var vm = CreateAndRunVirtualMachine(query, EmptySources());
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Id", typeof(int)), ("Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, "keep"]);
    }

    [TestMethod]
    public void ValuesSource_WindowOverInlineRows_ShouldKeepExpandoAdapterPathWorking()
    {
        const string query = (
            "select rows.Name, RowNumber() over (order by rows.Score) as Position\n"
            + "from values {\n"
            + "    ( Name: 'second', Score: 20 ),\n"
            + "    ( Name: 'first', Score: 10 )\n"
            + "} rows\n"
            + "order by rows.Score"
        );
        var vm = CreateAndRunVirtualMachine(query, EmptySources());
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Position", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["first", 1L], ["second", 2L]);
    }
}
