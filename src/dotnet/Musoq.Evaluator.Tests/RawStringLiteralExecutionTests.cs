using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class RawStringLiteralExecutionTests : BasicEntityTestBase
{

    [TestMethod]
    public void ValuesSource_WhenRawLiteralIsUsed_ShouldPreserveBackslashes()
    {
        const string query = @"
from values {
    { Path: r'C:\new\test' },
    { Path: r'\\server\share' }
} paths
select paths.Path
order by paths.Path";

        var vm = CreateAndRunVirtualMachine(
            query,
            new Dictionary<string, IEnumerable<BasicEntity>>());
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            [@"C:\new\test"],
            [@"\\server\share"]);
    }

    [TestMethod]
    public void ValuesSource_WhenRawLiteralIsAssignedToScriptVariable_ShouldPreserveValue()
    {
        const string query = @"
let path: string = r'C:\A';
from values {
    { Path: $path }
} paths
select paths.Path";

        var vm = CreateAndRunVirtualMachine(
            query,
            new Dictionary<string, IEnumerable<BasicEntity>>());
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [@"C:\A"]);
    }
}
