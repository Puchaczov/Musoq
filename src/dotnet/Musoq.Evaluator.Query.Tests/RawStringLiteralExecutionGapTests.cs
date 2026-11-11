using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class RawStringLiteralExecutionGapTests : BasicEntityTestBase
{
    [TestMethod]
    public void ValuesSource_WhenUppercaseEmptyAndQuotedRawLiteralsAreUsed_ShouldPreserveValues()
    {
        const string query = (
            "\n"
            + "from values {\n"
            + "    ( Path: R'C:\\new\\test', Empty: R'', Quoted: R'a''b' )\n"
            + "} paths\n"
            + "select paths.Path, paths.Empty, paths.Quoted"
        );

        var vm = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.HasCount(1, table);
        Assert.AreEqual(@"C:\new\test", table[0].ReadValue(0));
        Assert.AreEqual(string.Empty, table[0].ReadValue(1));
        Assert.AreEqual("a'b", table[0].ReadValue(2));
    }

    [TestMethod]
    public void ValuesSource_WhenRawBackslashesReachLikeAndRlike_ShouldMatchExpectedPath()
    {
        const string query = (
            "\n"
            + "from values { ( Path: R'C:\\logs\\app.log' ) } paths\n"
            + "select\n"
            + "    paths.Path like R'C:\\logs\\%.log' as LikeMatch,\n"
            + "    paths.Path rlike R'C:\\\\logs\\\\.*\\.log' as RlikeMatch"
        );

        var vm = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.HasCount(1, table);
        Assert.AreEqual(true, table[0].ReadValue(0));
        Assert.AreEqual(true, table[0].ReadValue(1));
    }

    [TestMethod]
    public void ValuesSource_WhenUppercaseRawLiteralIsAssignedToScriptVariable_ShouldPreserveValue()
    {
        const string query = (
            "\n" + "let path: string = R'C:\\A';\n" + "from values { ( Path: $path ) } paths\n" + "select paths.Path"
        );

        var vm = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.HasCount(1, table);
        Assert.AreEqual(@"C:\A", table[0].ReadValue(0));
    }
}
