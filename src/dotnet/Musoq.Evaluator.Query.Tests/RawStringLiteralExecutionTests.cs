using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class RawStringLiteralExecutionTests : BasicEntityTestBase
{
    [TestMethod]
    public void ValuesSource_WhenRawLiteralIsUsed_ShouldPreserveBackslashes()
    {
        const string query = (
            "\n"
            + "from values {\n"
            + "    ( Path: r'C:\\new\\test' ),\n"
            + "    ( Path: r'\\\\server\\share' )\n"
            + "} paths\n"
            + "select paths.Path\n"
            + "order by paths.Path"
        );

        var vm = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [@"C:\new\test"], [@"\\server\share"]);
    }

    [TestMethod]
    public void ValuesSource_WhenRawLiteralIsAssignedToScriptVariable_ShouldPreserveValue()
    {
        const string query = (
            "\n"
            + "let path: string = r'C:\\A';\n"
            + "from values {\n"
            + "    ( Path: $path )\n"
            + "} paths\n"
            + "select paths.Path"
        );

        var vm = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [@"C:\A"]);
    }
}
