using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.Execution;
using Musoq.Parser.Diagnostics;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class SuspiciousOrdinaryStringEscapeCompilationTests
{
    private readonly TestsLoggerResolver _loggerResolver = new();

    [TestMethod]
    public void CompileWithDiagnostics_WhenRootedOrdinaryPathContainsEscapes_ShouldWarnAndKeepRuntimeSemantics()
    {
        const string query = @"select 'C:\new\test' from #system.dual()";

        var result = InstanceCreator.CompileWithDiagnostics(
            query,
            Guid.NewGuid().ToString(),
            new SystemSchemaProvider(),
            _loggerResolver);

        Assert.IsTrue(result.Succeeded, FormatDiagnostics(result.Diagnostics));
        Assert.IsNotNull(result.CompiledQuery);
        Assert.IsEmpty(result.Errors);
        Assert.IsEmpty(result.ToEnvelopes());
        Assert.HasCount(1, result.Warnings, FormatDiagnostics(result.Diagnostics));
        Assert.AreEqual(DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape, result.Warnings[0].Code);
        Assert.AreEqual(
            "C:\n" + "ew\t" + "est",
            result.CompiledQuery.Run()[0][0]);
    }

    [TestMethod]
    public async Task CompileWithDiagnosticsAsync_WhenRootedOrdinaryPathContainsEscapes_ShouldReturnTheSameWarning()
    {
        const string query = @"select 'C:\new\test' from #system.dual()";

        var result = await InstanceCreator.CompileWithDiagnosticsAsync(
            query,
            Guid.NewGuid().ToString(),
            new SystemSchemaProvider(),
            _loggerResolver,
            new CompilationOptions());

        Assert.IsTrue(result.Succeeded, FormatDiagnostics(result.Diagnostics));
        Assert.HasCount(1, result.Warnings, FormatDiagnostics(result.Diagnostics));
        Assert.AreEqual(DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape, result.Warnings[0].Code);
    }

    [TestMethod]
    public void CompileForInspection_WhenOrdinaryPathIsUsed_ShouldExposeWarningIrAndEscapedGeneratedCode()
    {
        const string query = @"select f.Path from #raw.files('C:\new\test', true) f";

        var result = InstanceCreator.CompileForInspection(
            query,
            Guid.NewGuid().ToString(),
            new RawStringLiteralSchemaProvider(),
            _loggerResolver);

        Assert.HasCount(1, result.Warnings);
        Assert.HasCount(1, result.Diagnostics);
        Assert.AreEqual(DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape, result.Warnings[0].Code);

        var sourceScan = result.ExecutionPlan!.Body.Nodes.OfType<ExecutionSourceScan>().Single();
        var path = (ExecutionLiteral)sourceScan.Binding.Arguments[0];
        Assert.AreEqual("C:\n" + "ew\t" + "est", path.Value.ToClrValue());
        Assert.Contains("\\n", result.GeneratedCSharpCode);
        Assert.Contains("\\t", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CreateForAnalyze_WhenOrdinaryPathIsUsed_ShouldExposeTheWarningThroughTheDiagnosticContext()
    {
        const string query = @"select 'C:\new\test' from #system.dual()";

        var items = InstanceCreator.CreateForAnalyze(
            query,
            Guid.NewGuid().ToString(),
            new SystemSchemaProvider(),
            _loggerResolver);

        var warnings = items.DiagnosticContext.Warnings.ToList();
        Assert.HasCount(1, warnings);
        Assert.AreEqual(DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape, warnings[0].Code);
    }

    [TestMethod]
    public void CompileWithDiagnostics_WhenRelativePathIsBoundToSourcePath_ShouldWarnWithoutChangingTheValue()
    {
        const string query = @"select f.Path from #raw.files('some\text', true) f";

        var result = InstanceCreator.CompileWithDiagnostics(
            query,
            Guid.NewGuid().ToString(),
            new RawStringLiteralSchemaProvider(),
            _loggerResolver);

        Assert.IsTrue(result.Succeeded, FormatDiagnostics(result.Diagnostics));
        Assert.HasCount(1, result.Warnings);
        Assert.AreEqual(DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape, result.Warnings[0].Code);
        Assert.AreEqual("some\t" + "ext", result.CompiledQuery!.Run()[0][0]);
    }

    [TestMethod]
    public void CompileForExecution_WhenRawOrDoubledPathIsUsed_ShouldRemainWarningFreeAndExact()
    {
        var raw = InstanceCreator.CompileWithDiagnostics(
            @"select f.Path from #raw.files(r'C:\new\test', true) f",
            Guid.NewGuid().ToString(),
            new RawStringLiteralSchemaProvider(),
            _loggerResolver);
        var doubled = InstanceCreator.CompileWithDiagnostics(
            @"select f.Path from #raw.files('C:\\new\\test', true) f",
            Guid.NewGuid().ToString(),
            new RawStringLiteralSchemaProvider(),
            _loggerResolver);

        Assert.IsTrue(raw.Succeeded, FormatDiagnostics(raw.Diagnostics));
        Assert.IsTrue(doubled.Succeeded, FormatDiagnostics(doubled.Diagnostics));
        Assert.IsEmpty(raw.Warnings);
        Assert.IsEmpty(doubled.Warnings);
        Assert.AreEqual(@"C:\new\test", raw.CompiledQuery!.Run()[0][0]);
        Assert.AreEqual(@"C:\new\test", doubled.CompiledQuery!.Run()[0][0]);
    }

    [TestMethod]
    public void CompileWithDiagnostics_WhenSameQueryUsesParseAndExecutionCaches_ShouldReplayOneEquivalentWarning()
    {
        var query = $@"select 'C:\new\test' as Path, '{Guid.NewGuid():N}' as Token from #system.dual()";
        var provider = new SystemSchemaProvider();

        var first = InstanceCreator.CompileWithDiagnostics(
            query,
            Guid.NewGuid().ToString(),
            provider,
            _loggerResolver,
            new CompilationOptions());
        var second = InstanceCreator.CompileWithDiagnostics(
            query,
            Guid.NewGuid().ToString(),
            provider,
            _loggerResolver,
            new CompilationOptions());

        Assert.IsTrue(first.Succeeded, FormatDiagnostics(first.Diagnostics));
        Assert.IsTrue(second.Succeeded, FormatDiagnostics(second.Diagnostics));
        Assert.HasCount(1, first.Warnings, FormatDiagnostics(first.Diagnostics));
        Assert.HasCount(1, second.Warnings, FormatDiagnostics(second.Diagnostics));
        AssertEquivalent(first.Warnings[0], second.Warnings[0]);
        Assert.IsEmpty(second.Errors);
        Assert.IsEmpty(second.ToEnvelopes());
    }

    [TestMethod]
    public void CompileWithDiagnostics_WhenCanonicalExecutionCacheIsUsed_ShouldRetainWarning()
    {
        if (Debugger.IsAttached)
            return;

        var suffix = Guid.NewGuid().ToString("N");
        var first = InstanceCreator.CompileWithDiagnostics(
            $"select 'C:\\new\\test' as Path, 'token-{suffix}' as Token from #artifact.items() i",
            $"SuspiciousCanonicalFirst_{suffix}",
            new ArtifactSchemaProvider(new ArtifactSchema("first")),
            _loggerResolver,
            new CompilationOptions());
        var secondProvider = new ArtifactSchemaProvider(new ArtifactSchema("second"));
        var second = InstanceCreator.CompileWithDiagnostics(
            $"select  'C:\\new\\test'  as  Path,  'token-{suffix}'  as  Token  from  #artifact.items()  i",
            $"SuspiciousCanonicalSecond_{suffix}",
            secondProvider,
            _loggerResolver,
            new CompilationOptions());

        Assert.IsTrue(first.Succeeded, FormatDiagnostics(first.Diagnostics));
        Assert.IsTrue(second.Succeeded, FormatDiagnostics(second.Diagnostics));
        Assert.HasCount(1, first.Warnings, FormatDiagnostics(first.Diagnostics));
        Assert.HasCount(1, second.Warnings, FormatDiagnostics(second.Diagnostics));
        Assert.AreEqual(first.Warnings[0].Code, second.Warnings[0].Code);
        Assert.AreEqual(first.Warnings[0].Severity, second.Warnings[0].Severity);
        Assert.AreEqual(first.Warnings[0].Message, second.Warnings[0].Message);
        Assert.AreEqual(
            first.Warnings[0].EndLocation.Offset - first.Warnings[0].Location.Offset,
            second.Warnings[0].EndLocation.Offset - second.Warnings[0].Location.Offset);
        Assert.AreEqual(first.Warnings[0].Location.Offset + 1, second.Warnings[0].Location.Offset);
        Assert.AreEqual(first.Warnings[0].EndLocation.Offset + 1, second.Warnings[0].EndLocation.Offset);
        Assert.IsNotNull(first.BuildItems);
        Assert.IsNotNull(second.BuildItems);
        var firstIdentity = InstanceCreator.GetCanonicalExecutionEntryIdentityForTests(
            first.BuildItems!,
            new ArtifactSchemaProvider(new ArtifactSchema("first")));
        var secondIdentity = InstanceCreator.GetCanonicalExecutionEntryIdentityForTests(
            second.BuildItems,
            secondProvider);
        Assert.AreNotEqual(0, firstIdentity);
        Assert.AreEqual(firstIdentity, secondIdentity);

        using var firstTable = first.CompiledQuery!.Run();
        using var secondTable = second.CompiledQuery!.Run();
        Assert.AreEqual("C:\n" + "ew\t" + "est", firstTable[0][0]);
        Assert.AreEqual("C:\n" + "ew\t" + "est", secondTable[0][0]);
    }

    private static void AssertEquivalent(Diagnostic expected, Diagnostic actual)
    {
        Assert.AreEqual(expected.Code, actual.Code);
        Assert.AreEqual(expected.Severity, actual.Severity);
        Assert.AreEqual(expected.Message, actual.Message);
        Assert.AreEqual(expected.Location.Offset, actual.Location.Offset);
        Assert.AreEqual(expected.EndLocation.Offset, actual.EndLocation.Offset);
    }

    private static string FormatDiagnostics(System.Collections.Generic.IEnumerable<Diagnostic> diagnostics)
    {
        return string.Join(Environment.NewLine, diagnostics.Select(static diagnostic => diagnostic.ToDetailedString()));
    }
}
