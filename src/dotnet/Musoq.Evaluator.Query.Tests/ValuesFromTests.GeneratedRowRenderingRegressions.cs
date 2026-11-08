using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class ValuesFromTests
{
    [TestMethod]
    public void ValuesSource_InnerJoinWithSourceLocalOnFilter_ShouldMaterializeFilteredRows()
    {
        const string query = """
                             select leftRows.Id, rightRows.Value
                             from values {
                                 ( Id: 1 ),
                                 ( Id: 2 )
                             } leftRows
                             inner join values {
                                 ( Id: 1, Value: 'keep', Enabled: true ),
                                 ( Id: 2, Value: 'skip', Enabled: false )
                             } rightRows on leftRows.Id = rightRows.Id and rightRows.Enabled = true
                             order by leftRows.Id
                             """;
        var vm = CreateAndRunVirtualMachine(query, EmptySources());
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Id", typeof(int)),
            ("Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, "keep"]);
    }

    [TestMethod]
    public void ValuesSource_WindowOverInlineRows_ShouldKeepExpandoAdapterPathWorking()
    {
        const string query = """
                             select rows.Name, RowNumber() over (order by rows.Score) as Position
                             from values {
                                 ( Name: 'second', Score: 20 ),
                                 ( Name: 'first', Score: 10 )
                             } rows
                             order by rows.Score
                             """;
        var vm = CreateAndRunVirtualMachine(query, EmptySources());
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Position", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["first", 1L],
            ["second", 2L]);
    }
}
