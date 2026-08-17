using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class RawStringLiteralPipelineTests
{
    private readonly TestsLoggerResolver _loggerResolver = new();

    [TestMethod]
    public void CompileForInspection_WhenRawSourceArgumentIsUsed_ShouldPreserveItInExecutionIr()
    {
        var result = InstanceCreator.CompileForInspection(
            @"select f.Path from #raw.files(r'C:\new\test', true) f",
            Guid.NewGuid().ToString(),
            new RawStringLiteralSchemaProvider(),
            _loggerResolver);

        var plan = result.ExecutionPlan;
        Assert.IsNotNull(plan);

        var sourceScan = plan.Body.Nodes.OfType<ExecutionSourceScan>().Single();
        Assert.HasCount(2, sourceScan.Binding.Arguments);

        var path = sourceScan.Binding.Arguments[0];
        Assert.IsInstanceOfType<ExecutionLiteral>(path);
        Assert.AreEqual(
            @"C:\new\test",
            ((ExecutionLiteral)path).Value.ToClrValue());

        var recursive = sourceScan.Binding.Arguments[1];
        Assert.IsInstanceOfType<ExecutionLiteral>(recursive);
        Assert.AreEqual(true, ((ExecutionLiteral)recursive).Value.ToClrValue());
        Assert.Contains("C:\\\\new\\\\test", result.GeneratedCSharpCode);
    }

    [TestMethod]
    [DataRow(@"C:\Some\Path\To\Directory")]
    [DataRow(@"C:\new\test")]
    [DataRow(@"\\server\share")]
    [DataRow(@"\\?\C:\Directory")]
    [DataRow(@"\\.\pipe\name")]
    [DataRow(@"C:\Temp\")]
    public void CompileForExecution_WhenRawSourceArgumentContainsWindowsPath_ShouldPreserveIt(
        string expectedPath)
    {
        var query = $"select f.Path, f.Recursive from #raw.files(r'{expectedPath}', true) f";

        var compiled = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            new RawStringLiteralSchemaProvider(),
            _loggerResolver);

        var table = compiled.Run();

        Assert.HasCount(1, table);
        Assert.AreEqual(expectedPath, table[0][0]);
        Assert.AreEqual(true, table[0][1]);
    }

    [TestMethod]
    public void CompileForExecution_WhenRawLiteralIsUsedAcrossExpressions_ShouldPreserveValue()
    {
        var compiled = InstanceCreator.CompileForExecution(
            @"
select
    f.Path as Path,
    r'C:\A' as LiteralPath,
    case when f.Path = r'C:\A' then r'C:\new\test' else r'not matched' end as CasePath,
    f.Path in (r'C:\A', r'C:\B') as InMatch,
    f.Path like r'%A' as LikeMatch,
    f.Path rlike r'.*A' as RlikeMatch,
    Length(r'C:\A') as PathLength
from #raw.files(r'C:\A', true) f
where f.Path = r'C:\A'
order by r'C:\A'",
            Guid.NewGuid().ToString(),
            new RawStringLiteralSchemaProvider(),
            _loggerResolver);

        var table = compiled.Run();

        Assert.HasCount(1, table);
        Assert.AreEqual(@"C:\A", table[0][0]);
        Assert.AreEqual(@"C:\A", table[0][1]);
        Assert.AreEqual(@"C:\new\test", table[0][2]);
        Assert.AreEqual(true, table[0][3]);
        Assert.AreEqual(true, table[0][4]);
        Assert.AreEqual(true, table[0][5]);
        Assert.AreEqual(4, table[0][6]);
    }

    [TestMethod]
    public void CompileForExecution_WhenRawLiteralIsUsedInHaving_ShouldPreserveValue()
    {
        var compiled = InstanceCreator.CompileForExecution(
            @"
select f.Path, Count(f.Path)
from #raw.files(r'C:\A', true) f
group by f.Path
having Count(f.Path) > Length(r'')
order by f.Path",
            Guid.NewGuid().ToString(),
            new RawStringLiteralSchemaProvider(),
            _loggerResolver);

        var table = compiled.Run();

        Assert.HasCount(1, table);
        Assert.AreEqual(@"C:\A", table[0][0]);
        Assert.AreEqual(1L, table[0][1]);
    }

    [TestMethod]
    public void CompileForExecution_WhenRawLiteralIsAssignedToScriptVariable_ShouldPreserveValue()
    {
        var compiled = InstanceCreator.CompileForExecution(
            @"
let path: string = r'C:\A';
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

    [TestMethod]
    public void CompileForExecution_WhenRawAndOrdinaryLiteralsCoexist_ShouldKeepOrdinaryEscapes()
    {
        var compiled = InstanceCreator.CompileForExecution(
            @"select r'\n', '\n', '\'', r'\''' from #raw.files(r'row', true)",
            Guid.NewGuid().ToString(),
            new RawStringLiteralSchemaProvider(),
            _loggerResolver);

        var table = compiled.Run();

        Assert.HasCount(1, table);
        Assert.AreEqual(@"\n", table[0][0]);
        Assert.AreEqual("\n", table[0][1]);
        Assert.AreEqual("'", table[0][2]);
        Assert.AreEqual(@"\'", table[0][3]);
    }
}
