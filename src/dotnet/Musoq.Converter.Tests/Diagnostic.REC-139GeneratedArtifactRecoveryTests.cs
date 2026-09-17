using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Build;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Targets.Abstractions;

namespace Musoq.Converter.Tests;

[TestClass]
[DoNotParallelize]
public sealed class DiagnosticREC139GeneratedArtifactRecoveryTests
{
    private const string ArtifactQuery = "select i.Value from #artifact.items() i";
    private const string RecompileGuidance = "Recompile the query artifact with the current engine and schema provider.";

    [TestMethod]
    public void GeneratedCompilationFailures_ShouldKeepNativeFactsAndGeneratedLocations()
    {
        Assert.HasCount(6, GeneratedCompilationCases);

        foreach (var testCase in GeneratedCompilationCases)
        {
            var query = "select 1";
            var context = new DiagnosticContext(new SourceText(query, $"{testCase.Id}.query.musoq"));
            TargetDiagnosticReporter.Report(CreateGeneratedDiagnostics(testCase), context);

            var diagnostic = context.Diagnostics.Single();
            Assert.AreEqual(DiagnosticCode.MQ8001_CodeGenerationFailed, diagnostic.Code, testCase.Id);
            Assert.AreEqual(DiagnosticPhase.CodeGeneration, diagnostic.Phase, testCase.Id);
            Assert.AreEqual(DiagnosticSourceKind.GeneratedSource, diagnostic.SourceKind, testCase.Id);
            Assert.IsFalse(diagnostic.Message.Contains("unsupported SQL", StringComparison.OrdinalIgnoreCase), testCase.Id);
            Assert.IsTrue(diagnostic.Arguments.TryGetValue("targetCode", out var targetCode), testCase.Id);
            Assert.IsNotNull(targetCode);

            if (testCase.HasExactSourceMap)
            {
                Assert.IsTrue(targetCode.StartsWith("CS", StringComparison.Ordinal), testCase.Id);
                Assert.IsTrue(diagnostic.Location.IsValid, testCase.Id);
                Assert.AreEqual(testCase.SourceName, diagnostic.Location.FilePath, testCase.Id);
                Assert.IsGreaterThan(query.Length, diagnostic.Location.Offset, testCase.Id);
                Assert.IsGreaterThanOrEqualTo(diagnostic.Location.Offset, diagnostic.EndLocation.Offset, testCase.Id);
                Assert.IsNotNull(diagnostic.ContextSnippet, testCase.Id);
                Assert.IsFalse(diagnostic.ContextSnippet!.Contains(query, StringComparison.Ordinal), testCase.Id);
            }
            else
            {
                Assert.AreEqual("CS9999", targetCode, testCase.Id);
                Assert.AreEqual(SourceLocation.None, diagnostic.Location, testCase.Id);
                Assert.AreEqual(SourceLocation.None, diagnostic.EndLocation, testCase.Id);
                Assert.IsNull(diagnostic.ContextSnippet, testCase.Id);
            }

            var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);
            Assert.AreEqual(DiagnosticSourceKind.GeneratedSource, envelope.SourceKind, testCase.Id);
            Assert.AreEqual(DiagnosticPhase.CodeGeneration, envelope.Phase, testCase.Id);
            Assert.IsFalse(envelope.Message.Contains("unsupported SQL", StringComparison.OrdinalIgnoreCase), testCase.Id);
        }
    }

    [TestMethod]
    public void IncompatibleArtifacts_ShouldExposeRecompileGuidanceAndNeverLookLikeSql()
    {
        Assert.HasCount(6, ArtifactMismatchCases);
        var provider = new ArtifactSchemaProvider(new ArtifactSchema("single"));
        var artifactResult = InstanceCreator.CompileArtifactWithDiagnostics(
            ArtifactQuery,
            "REC139Artifact",
            provider,
            new TestsLoggerResolver());

        Assert.IsTrue(
            artifactResult.Succeeded,
            string.Join(Environment.NewLine, artifactResult.Diagnostics.Select(static diagnostic => diagnostic.ToDetailedString())));
        var artifact = artifactResult.Artifact ?? throw new AssertFailedException("REC-139 artifact fixture did not compile.");

        foreach (var testCase in ArtifactMismatchCases)
        {
            var tampered = CreateArtifactVariant(artifact, testCase.Kind);
            var result = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
                ArtifactQuery,
                tampered,
                new ArtifactSchemaProvider(new ArtifactSchema("single")),
                new TestsLoggerResolver(),
                testCase.ValidationMode);

            Assert.IsFalse(result.Succeeded, testCase.Id);
            Assert.IsNull(result.CompiledQuery, testCase.Id);
            Assert.IsNotEmpty(result.Errors, testCase.Id);
            Assert.IsTrue(
                result.Errors.All(static diagnostic => diagnostic.Code == DiagnosticCode.MQ8002_CompiledArtifactIncompatible),
                $"{testCase.Id}: {string.Join(Environment.NewLine, result.Errors.Select(static diagnostic => diagnostic.ToDetailedString()))}");

            var diagnostic = result.Errors.Single();
            Assert.AreEqual(DiagnosticPhase.Runtime, diagnostic.Phase, testCase.Id);
            Assert.AreEqual(DiagnosticSourceKind.GeneratedSource, diagnostic.SourceKind, testCase.Id);
            Assert.AreEqual(SourceLocation.None, diagnostic.Location, testCase.Id);
            Assert.AreEqual(SourceLocation.None, diagnostic.EndLocation, testCase.Id);
            Assert.IsFalse(diagnostic.Message.Contains("unsupported SQL", StringComparison.OrdinalIgnoreCase), testCase.Id);
            Assert.IsTrue(diagnostic.Message.Contains(testCase.MessageFragment, StringComparison.Ordinal), testCase.Id);

            var envelope = result.ToEnvelopes().Single(static envelope =>
                envelope.Code == DiagnosticCode.MQ8002_CompiledArtifactIncompatible);
            CollectionAssert.Contains(envelope.SuggestedFixes.ToArray(), RecompileGuidance, testCase.Id);
            Assert.AreEqual(DiagnosticSourceKind.GeneratedSource, envelope.SourceKind, testCase.Id);
            Assert.IsNull(envelope.Offset, testCase.Id);
            Assert.IsNull(envelope.EndOffset, testCase.Id);
            Assert.IsFalse(envelope.Message.Contains("unsupported SQL", StringComparison.OrdinalIgnoreCase), testCase.Id);
        }
    }

    [TestMethod]
    public void InternalFailures_ShouldExposeCorrelationWithoutNativeDetailsByDefault()
    {
        Assert.HasCount(6, InternalFailureCases);

        foreach (var testCase in InternalFailureCases)
        {
            var inner = testCase.ExceptionFactory();
            var wrapped = InternalDiagnosticException.ForCompiler(inner, testCase.Span);
            var source = new SourceText("select d.Dummy from #system.dual() d", $"{testCase.Id}.query.musoq");
            var diagnostic = wrapped.ToDiagnostic(source);
            var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, source.Text);
            var defaultText = MusoqErrorEnvelopeFormatter.FormatText(envelope);
            var defaultJson = MusoqErrorEnvelopeFormatter.FormatJson(envelope);
            var verbose = MusoqErrorEnvelope.FromExceptionVerbose(wrapped, source.Text);

            Assert.AreEqual(DiagnosticCode.MQ9001_InternalCompilerError, diagnostic.Code, testCase.Id);
            Assert.AreEqual(DiagnosticPhase.Internal, diagnostic.Phase, testCase.Id);
            Assert.AreEqual(DiagnosticSourceKind.Internal, diagnostic.SourceKind, testCase.Id);
            Assert.IsTrue(wrapped.CorrelationId.StartsWith("internal-", StringComparison.Ordinal), testCase.Id);
            Assert.AreEqual(wrapped.CorrelationId, diagnostic.CorrelationId, testCase.Id);
            Assert.AreEqual(wrapped.CorrelationId, diagnostic.Arguments["correlationId"], testCase.Id);
            Assert.AreEqual(inner.GetType().FullName, diagnostic.Arguments["exceptionType"], testCase.Id);
            Assert.AreEqual(wrapped.CorrelationId, envelope.CorrelationId, testCase.Id);
            Assert.AreEqual(wrapped.CorrelationId, envelope.Arguments["correlationId"], testCase.Id);
            Assert.IsFalse(defaultText.Contains(testCase.Secret, StringComparison.Ordinal), testCase.Id);
            Assert.IsFalse(defaultJson.Contains(testCase.Secret, StringComparison.Ordinal), testCase.Id);
            Assert.IsFalse(defaultText.Contains(testCase.NativeSourceMarker, StringComparison.Ordinal), testCase.Id);
            Assert.IsFalse(defaultJson.Contains(testCase.NativeSourceMarker, StringComparison.Ordinal), testCase.Id);
            Assert.IsTrue(verbose.Details?.Contains(testCase.Secret, StringComparison.Ordinal) == true, testCase.Id);

            if (testCase.Span is { } span)
            {
                Assert.AreEqual(span.Start, envelope.Offset, testCase.Id);
                Assert.AreEqual(span.End, envelope.EndOffset, testCase.Id);
            }
            else
            {
                Assert.IsNull(envelope.Offset, testCase.Id);
                Assert.IsNull(envelope.EndOffset, testCase.Id);
            }
        }
    }

    [TestMethod]
    public void KnownInvalidQueries_ShouldStopBeforeGeneratedRecovery()
    {
        Assert.HasCount(6, InvalidQueryCases);

        foreach (var testCase in InvalidQueryCases)
        {
            var result = InstanceCreator.CompileArtifactWithDiagnostics(
                testCase.Query,
                $"REC139_{testCase.Id}",
                new SystemSchemaProvider(),
                new TestsLoggerResolver());

            Assert.IsFalse(result.Succeeded, testCase.Id);
            Assert.IsNull(result.Artifact, testCase.Id);
            Assert.IsTrue(result.Errors.Any(diagnostic => diagnostic.Code == testCase.ExpectedCode), testCase.Id);
            Assert.IsFalse(result.Errors.Any(static diagnostic =>
                diagnostic.Code is DiagnosticCode.MQ8001_CodeGenerationFailed or
                DiagnosticCode.MQ8002_CompiledArtifactIncompatible or
                DiagnosticCode.MQ9001_InternalCompilerError), testCase.Id);
            Assert.IsTrue(result.Errors.All(static diagnostic => diagnostic.SourceKind == DiagnosticSourceKind.Query), testCase.Id);
            Assert.IsTrue(result.Errors.All(static diagnostic => diagnostic.Phase is DiagnosticPhase.Parse or DiagnosticPhase.Bind), testCase.Id);
        }
    }

    private static IReadOnlyList<TargetDiagnostic> CreateGeneratedDiagnostics(GeneratedCompilationCase testCase)
    {
        if (!testCase.HasExactSourceMap)
        {
            return
            [
                new TargetDiagnostic(
                    "CS9999",
                    TargetDiagnosticSeverity.Error,
                    "generated compiler failure has no exact source map")
            ];
        }

        var syntaxTree = CSharpSyntaxTree.ParseText(
            testCase.GeneratedSource,
            new CSharpParseOptions(LanguageVersion.CSharp13),
            testCase.SourceName!);
        var compilation = CSharpCompilation.Create(
            testCase.Id,
            [syntaxTree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var finalizer = ExecutionTargetCatalog.ResolveFinalizer(ExecutionTargetIds.CSharpClr);
        var result = (CSharpClrFinalizationResult)finalizer.Finalize(
            new CSharpRenderedQueryArtifact(compilation, $"{testCase.Id}.CompiledQuery"),
            new CSharpClrFinalizationOptions(false));

        Assert.IsFalse(result.Success, testCase.Id);
        var diagnostics = result.Diagnostics
            .Where(static diagnostic => diagnostic.Severity == TargetDiagnosticSeverity.Error)
            .Take(1)
            .ToArray();
        Assert.HasCount(1, diagnostics, testCase.Id);
        return diagnostics;
    }

    private static ICompiledQueryArtifact CreateArtifactVariant(
        ICompiledQueryArtifact artifact,
        ArtifactMismatchKind kind)
    {
        var metadata = new Dictionary<string, string>(artifact.Metadata, StringComparer.Ordinal);
        return kind switch
        {
            ArtifactMismatchKind.FormatVersion => new CompiledQueryArtifact(
                artifact.AssemblyBytes,
                artifact.SymbolsBytes,
                artifact.RunnableTypeName,
                artifact.EngineVersion,
                "REC139-old-format",
                artifact.CompilationOptionsSignature,
                metadata),
            ArtifactMismatchKind.EngineVersion => new CompiledQueryArtifact(
                artifact.AssemblyBytes,
                artifact.SymbolsBytes,
                artifact.RunnableTypeName,
                "REC139-old-engine",
                artifact.ArtifactFormatVersion,
                artifact.CompilationOptionsSignature,
                metadata),
            ArtifactMismatchKind.OptionsSignature => new CompiledQueryArtifact(
                artifact.AssemblyBytes,
                artifact.SymbolsBytes,
                artifact.RunnableTypeName,
                artifact.EngineVersion,
                artifact.ArtifactFormatVersion,
                "REC139-old-options",
                metadata),
            ArtifactMismatchKind.RuntimeContract => WithMetadata(
                artifact,
                metadata,
                CompiledQueryArtifactSupport.MetadataRuntimeV2ContractSignature,
                "runtime-v2=REC139-old"),
            ArtifactMismatchKind.SemanticsVersion => WithMetadata(
                artifact,
                metadata,
                CompiledQueryArtifactSupport.MetadataExecutionSemanticsVersion,
                "REC139-old-semantics"),
            ArtifactMismatchKind.GeneratedHash => WithMetadata(
                artifact,
                metadata,
                CompiledQueryArtifactSupport.MetadataGeneratedCodeSha256,
                "REC139-invalid-generated-hash"),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }

    private static ICompiledQueryArtifact WithMetadata(
        ICompiledQueryArtifact artifact,
        Dictionary<string, string> metadata,
        string key,
        string value)
    {
        metadata[key] = value;
        return new CompiledQueryArtifact(
            artifact.AssemblyBytes,
            artifact.SymbolsBytes,
            artifact.RunnableTypeName,
            artifact.EngineVersion,
            artifact.ArtifactFormatVersion,
            artifact.CompilationOptionsSignature,
            metadata);
    }

    private enum ArtifactMismatchKind
    {
        FormatVersion,
        EngineVersion,
        OptionsSignature,
        RuntimeContract,
        SemanticsVersion,
        GeneratedHash
    }

    private sealed record GeneratedCompilationCase(
        string Id,
        bool HasExactSourceMap,
        string? SourceName,
        string GeneratedSource);

    private sealed record ArtifactMismatchCase(
        string Id,
        ArtifactMismatchKind Kind,
        string MessageFragment,
        CompiledQueryArtifactLoadOptions ValidationMode);

    private sealed record InternalFailureCase(
        string Id,
        Func<Exception> ExceptionFactory,
        string Secret,
        string NativeSourceMarker,
        TextSpan? Span);

    private sealed record InvalidQueryCase(string Id, string Query, DiagnosticCode ExpectedCode);

    private static IReadOnlyList<GeneratedCompilationCase> GeneratedCompilationCases { get; } =
    [
        new(
            "GEN-MAPPED-01",
            true,
            "REC139.Generated.01.g.cs",
            "namespace REC139 { public sealed class CompiledQuery { public void Broken( } }"),
        new(
            "GEN-MAPPED-02",
            true,
            "REC139.Generated.02.g.cs",
            "namespace REC139 { public sealed class CompiledQuery { public void Broken( } }"),
        new(
            "GEN-MAPPED-03",
            true,
            "REC139.Generated.03.g.cs",
            "namespace REC139 { public sealed class CompiledQuery { public void Broken( } }"),
        new("GEN-UNMAPPED-01", false, null, string.Empty),
        new("GEN-UNMAPPED-02", false, null, string.Empty),
        new("GEN-UNMAPPED-03", false, null, string.Empty)
    ];

    private static IReadOnlyList<ArtifactMismatchCase> ArtifactMismatchCases { get; } =
    [
        new(
            "ART-01",
            ArtifactMismatchKind.FormatVersion,
            "artifact format version",
            new CompiledQueryArtifactLoadOptions()),
        new(
            "ART-02",
            ArtifactMismatchKind.EngineVersion,
            "engine version",
            new CompiledQueryArtifactLoadOptions()),
        new(
            "ART-03",
            ArtifactMismatchKind.OptionsSignature,
            "compilation options signature",
            new CompiledQueryArtifactLoadOptions()),
        new(
            "ART-04",
            ArtifactMismatchKind.RuntimeContract,
            "RuntimeV2ContractSignature",
            new CompiledQueryArtifactLoadOptions()),
        new(
            "ART-05",
            ArtifactMismatchKind.SemanticsVersion,
            "ExecutionSemanticsVersion",
            new CompiledQueryArtifactLoadOptions()),
        new(
            "ART-06",
            ArtifactMismatchKind.GeneratedHash,
            "GeneratedCodeSha256",
            new CompiledQueryArtifactLoadOptions
            {
                ValidationMode = CompiledQueryArtifactValidationMode.StrictGeneratedCodeHash
            })
    ];

    private static IReadOnlyList<InternalFailureCase> InternalFailureCases { get; } =
    [
        new(
            "INT-01",
            () => new InvalidOperationException("REC139-secret-invalid-operation"),
            "REC139-secret-invalid-operation",
            "REC139-native-source-01",
            null),
        new(
            "INT-02",
            () => new ArgumentException("REC139-secret-argument"),
            "REC139-secret-argument",
            "REC139-native-source-02",
            new TextSpan(7, 1)),
        new(
            "INT-03",
            () => new InvalidDataException("REC139-secret-data"),
            "REC139-secret-data",
            "REC139-native-source-03",
            null),
        new(
            "INT-04",
            () => new TypeLoadException("REC139-secret-type-load"),
            "REC139-secret-type-load",
            "REC139-native-source-04",
            new TextSpan(0, 6)),
        new(
            "INT-05",
            () => new InvalidOperationException("REC139-secret-path C:\\REC139\\native.cs:27"),
            "REC139-secret-path C:\\REC139\\native.cs:27",
            "C:\\REC139\\native.cs:27",
            null),
        new(
            "INT-06",
            () => new Exception("REC139-secret-nested", new InvalidOperationException("REC139-inner-native")),
            "REC139-secret-nested",
            "REC139-inner-native",
            new TextSpan(12, 4))
    ];

    private static IReadOnlyList<InvalidQueryCase> InvalidQueryCases { get; } =
    [
        new("EARLY-01", "SELECT FROM #system.dual()", DiagnosticCode.MQ2005_InvalidSelectList),
        new("EARLY-02", "SELECT nonexistent FROM #system.dual()", DiagnosticCode.MQ3001_UnknownColumn),
        new("EARLY-03", "SELECT a.nonexistent FROM #system.dual() a", DiagnosticCode.MQ3001_UnknownColumn),
        new("EARLY-04", "SELECT 1 as a, bad_column FROM #system.dual()", DiagnosticCode.MQ3001_UnknownColumn),
        new("EARLY-05", "SELECT Dummy FROM #system.dual() WHERE missing = 1", DiagnosticCode.MQ3001_UnknownColumn),
        new("EARLY-06", "SELECT Dummy FROM #system.dual() ORDER BY nonexistent", DiagnosticCode.MQ3001_UnknownColumn)
    ];
}
