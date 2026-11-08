using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Components;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public sealed partial class ComputedSourceArgumentTests
{
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void NullStringDefault_MetadataShouldMatchLegacyConcatenationAtRuntime(bool folding)
    {
        var provider = new CapturingProvider { RequiresMetadataValue = true };
        using var query = InstanceCreator.CompileForExecution(
            "param(root: string = null) select e.Path from #args.echo($root + '/file') e",
            Guid.NewGuid().ToString(), provider, new TestsLoggerResolver(),
            new CompilationOptions(parallelizationMode: ParallelizationMode.None, useConstantFolding: folding));
        foreach (var arguments in provider.Metadata)
            CollectionAssert.AreEqual(new object?[] { "/file", "", null }, arguments);
        using var result = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(result, ["/file"]);
        Assert.AreEqual("/file", provider.Opened.Single().Path);
    }

    [TestMethod]
    [DataRow("$n / 2", 3)]
    [DataRow("($n / 2) * 2", 6)]
    [DataRow("$n % 2", 1)]
    public void StaticNumericMetadata_ShouldMatchRuntimeWidthAndIntegerArithmetic(string expression, int expected)
    {
        var provider = new CapturingProvider { RequiresMetadataValue = true };
        using var query = Compile($"param(n: int = 7) select e.Number from #args.echo('path', '', {expression}) e", provider);
        foreach (var arguments in provider.Metadata)
            CollectionAssert.AreEqual(new object?[] { "path", "", expected }, arguments);
        using var result = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(result, [expected]);
        Assert.AreEqual(expected, provider.Opened.Single().Number);
    }
    [TestMethod]
    public void DefaultExpressionsAndLetValues_ShouldProvideCanonicalStaticMetadata()
    {
        var provider = new CapturingProvider { RequiresMetadataValue = true };
        using var query = Compile("param(root: string = 'root', n: int = 20) let suffix: string = '/file'; " +
            "select e.Path, e.Number from #args.echo(number: $n * 2 + 2, path: $root + $suffix) e", provider);
        Assert.IsNotEmpty(provider.Metadata);
        foreach (var arguments in provider.Metadata)
            CollectionAssert.AreEqual(new object?[] { "root/file", "", 42 }, arguments);
        var metadataCalls = provider.Metadata.Count;
        Set(query, "root", "override");
        Set(query, "n", 30);
        using var result = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(result, ["override/file", 62]);
        Assert.AreEqual(metadataCalls, provider.Metadata.Count, "Runtime overrides must not rediscover source metadata.");
    }

    [TestMethod]
    [DataRow("param(root: string)", "$root + '/file'")]
    [DataRow("param(root: string = 'root')", "ToUpper($root)")]
    [DataRow("param(root: string = 'root')", "$root::string")]
    [DataRow("param(root: string = 'root')", "case when $root = 'root' then 'file' else 'other' end")]
    public void UnavailableRequiredMetadata_ShouldReportMq3062(string declaration, string expression)
    {
        var provider = new CapturingProvider { RequiresMetadataValue = true };
        var error = Assert.Throws<MusoqQueryException>(() =>
        {
            using var query = Compile($"{declaration} select e.Path from #args.echo({expression}) e", provider);
        });
        Assert.AreEqual(DiagnosticCode.MQ3062_InvalidScriptParameterSourceArgument, error.PrimaryEnvelope.Code);
        Assert.AreEqual(DiagnosticPhase.Bind, error.PrimaryEnvelope.Phase);
        Assert.Contains("static", error.PrimaryEnvelope.Message);
        Assert.Contains("default", error.PrimaryEnvelope.Message);
        Assert.IsNotEmpty(error.PrimaryEnvelope.SuggestedFixes);
        Assert.AreEqual(0, provider.OpenCount);
    }

    [TestMethod]
    public void DynamicNamedMiddleArgument_ShouldKeepKnownPrefixWithoutShiftingLaterLiteral()
    {
        var provider = new CapturingProvider { RequiresMetadataValue = true };
        using var query = Compile("param(root: string = 'root', suffix: string) " +
            "select e.Path, e.Suffix, e.Number from #args.echo(number: 42, suffix: $suffix + '.bin', path: $root + '/file') e", provider);
        Assert.IsNotEmpty(provider.Metadata);
        foreach (var arguments in provider.Metadata)
            CollectionAssert.AreEqual(new object?[] { "root/file" }, arguments);
        Set(query, "suffix", "data");
        using var result = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(result, ["root/file", "data.bin", 42]);
    }

    [TestMethod]
    public void CorrelatedMetadata_ShouldRemainUnavailableEvenWithDefaultedParameter()
    {
        var provider = new CapturingProvider { RequiresMetadataValue = true };
        var error = Assert.Throws<MusoqQueryException>(() =>
        {
            using var query = Compile("param(root: string = 'root/') select e.Path from #args.seed() s " +
                "cross apply #args.echo($root + s.Path) e", provider);
        });
        Assert.AreEqual(DiagnosticCode.MQ3062_InvalidScriptParameterSourceArgument, error.PrimaryEnvelope.Code);
        Assert.AreEqual(0, provider.OpenCount);
    }

    [TestMethod]
    [DataRow("$root + '/file'")]
    [DataRow("ToUpper($root)")]
    [DataRow("case when $n > 0 then ToUpper($root) else $root end")]
    public void CompiledArguments_ShouldUseNativeCarrierWithoutBoxingOrRuntimeMetadata(string expression)
    {
        var sql = $"param(root: string = 'root', n: int = 41) select e.Number from #args.echo({expression}, '', $n + 1) e";
        var inspection = InstanceCreator.CompileForInspection(sql, Guid.NewGuid().ToString(), new CapturingProvider(),
            new TestsLoggerResolver(), Options);
        Assert.Contains("SourceArguments.Create", inspection.GeneratedCSharpCode);
        Assert.DoesNotContain("new object[", inspection.GeneratedCSharpCode);
        Assert.DoesNotContain(".GetTableByName(", inspection.GeneratedCSharpCode);
        Assert.DoesNotContain(".DynamicInvoke(", inspection.GeneratedCSharpCode);
        using var query = Compile(sql, new CapturingProvider());
        foreach (var method in GeneratedExecutionIlInspection.GetGeneratedComputeMethods(query))
            Assert.IsFalse(GeneratedExecutionIlInspection.ContainsBoxOpcode(method));
    }

    [TestMethod]
    public void InitialFromArgument_ShouldRejectInjectionOfItsUnopenedSource()
    {
        var failure = Assert.Throws<MusoqQueryException>(() =>
        {
            using var query = Compile("param(root: string = 'root') select e.Path from #args.echo(SourcePath($root)) e",
                new CapturingProvider());
        });
        Assert.AreEqual(DiagnosticPhase.Bind, failure.PrimaryEnvelope.Phase);
        Assert.AreNotEqual(DiagnosticCode.MQ9001_InternalCompilerError, failure.PrimaryEnvelope.Code);
    }

    [TestMethod]
    public void InitialFromArgument_ShouldRejectAggregateCalls()
    {
        var failure = Assert.Throws<MusoqQueryException>(() =>
        {
            using var query = Compile("param(root: string = 'root') select e.Path from #args.echo(AggregateValues($root, '')) e",
                new CapturingProvider());
        });
        Assert.AreEqual(DiagnosticPhase.Bind, failure.PrimaryEnvelope.Phase);
        Assert.AreNotEqual(DiagnosticCode.MQ9001_InternalCompilerError, failure.PrimaryEnvelope.Code);
    }
}
