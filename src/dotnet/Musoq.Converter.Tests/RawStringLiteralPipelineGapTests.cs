using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class RawStringLiteralPipelineGapTests
{
    private readonly TestsLoggerResolver _loggerResolver = new();

    [TestMethod]
    public void CompileForInspection_WhenUppercaseRawSourceArgumentIsUsed_ShouldPreserveIrAndGeneratedCode()
    {
        var result = InstanceCreator.CompileForInspection(
            @"select f.Path from #raw.files(R'C:\new\test', true) f",
            Guid.NewGuid().ToString(),
            new RawStringLiteralSchemaProvider(),
            _loggerResolver);

        var sourceScan = result.ExecutionPlan!.Body.Nodes.OfType<ExecutionSourceScan>().Single();
        var path = (ExecutionLiteral)sourceScan.Binding.Arguments[0];

        Assert.AreEqual(@"C:\new\test", path.Value.ToClrValue());
        Assert.Contains("C:\\\\new\\\\test", result.GeneratedCSharpCode);
    }

    [TestMethod]
    [DataRow(@"C:\new\test")]
    [DataRow(@"\\server\share")]
    [DataRow(@"\\?\C:\Directory")]
    [DataRow(@"\\.\pipe\name")]
    [DataRow(@"C:\Temp\")]
    public void CompileForExecution_WhenUppercaseRawSourceArgumentContainsWindowsPath_ShouldPreserveIt(
        string expectedPath)
    {
        var compiled = InstanceCreator.CompileForExecution(
            $"select f.Path from #raw.files(R'{expectedPath}', true) f",
            Guid.NewGuid().ToString(),
            new RawStringLiteralSchemaProvider(),
            _loggerResolver);

        var table = compiled.Run();

        Assert.HasCount(1, table);
        Assert.AreEqual(expectedPath, table[0][0]);
    }

    [TestMethod]
    public void CompileForExecution_WhenUppercaseEmptyAndQuotedRawLiteralsAreUsed_ShouldPreserveValues()
    {
        var compiled = InstanceCreator.CompileForExecution(
            @"select R'' as Empty, R'a''b' as Quoted from #raw.files(R'row', true)",
            Guid.NewGuid().ToString(),
            new RawStringLiteralSchemaProvider(),
            _loggerResolver);

        var table = compiled.Run();

        Assert.HasCount(1, table);
        Assert.AreEqual(string.Empty, table[0][0]);
        Assert.AreEqual("a'b", table[0][1]);
    }

    [TestMethod]
    public void CompileForExecution_WhenRawBackslashesReachLikeAndRlike_ShouldMatchExpectedPath()
    {
        var compiled = InstanceCreator.CompileForExecution(
            @"select
    f.Path like R'C:\logs\%.log' as LikeMatch,
    f.Path rlike R'C:\\logs\\.*\.log' as RlikeMatch
from #raw.files(R'C:\logs\app.log', true) f",
            Guid.NewGuid().ToString(),
            new RawStringLiteralSchemaProvider(),
            _loggerResolver);

        var table = compiled.Run();

        Assert.HasCount(1, table);
        Assert.AreEqual(true, table[0][0]);
        Assert.AreEqual(true, table[0][1]);
    }

    [TestMethod]
    public void CompileForExecution_WhenUppercaseRawLiteralIsAssignedToScriptVariable_ShouldPreserveValue()
    {
        var compiled = InstanceCreator.CompileForExecution(
            @"
let path: string = R'C:\A';
select f.Path
from #raw.files($path, true) f
where f.Path = $path",
            Guid.NewGuid().ToString(),
            new RawStringLiteralSchemaProvider(),
            _loggerResolver);

        var table = compiled.Run();

        Assert.HasCount(1, table);
        Assert.AreEqual(@"C:\A", table[0][0]);
    }
}
