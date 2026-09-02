using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticInterpret052InterpretationPlacementTests : BinaryOrTextualEvaluatorTestBase
{
    [TestMethod]
    public void InterpretationFunctions_InSelect_ShouldReportMq3033ForEveryFunction()
    {
        var cases = new[]
        {
            (FunctionName: "Interpret", Declaration: "binary Packet { Value: int le };", Source: "files",
                Invocation: "Interpret<Packet>(f.Content)", IsBinary: true),
            (FunctionName: "Parse", Declaration: "text Record { Value: rest };", Source: "lines",
                Invocation: "Parse<Record>(f.Text)", IsBinary: false),
            (FunctionName: "TryInterpret", Declaration: "binary Packet { Value: int le };", Source: "files",
                Invocation: "TryInterpret<Packet>(f.Content)", IsBinary: true),
            (FunctionName: "TryParse", Declaration: "text Record { Value: rest };", Source: "lines",
                Invocation: "TryParse<Record>(f.Text)", IsBinary: false),
            (FunctionName: "InterpretAt", Declaration: "binary Packet { Value: int le };", Source: "files",
                Invocation: "InterpretAt<Packet>(f.Content, 0)", IsBinary: true),
            (FunctionName: "PartialInterpret", Declaration: "binary Packet { Value: int le };", Source: "files",
                Invocation: "PartialInterpret<Packet>(f.Content)", IsBinary: true),
            (FunctionName: "PartialParse", Declaration: "text Record { Value: rest };", Source: "lines",
                Invocation: "PartialParse<Record>(f.Text)", IsBinary: false)
        };

        foreach (var testCase in cases)
        {
            var query = $@"
                {testCase.Declaration}
                select {testCase.Invocation}
                from #test.{testCase.Source}() f";

            var exception = Assert.Throws<MusoqQueryException>(() =>
                CompileGeneratedQuery(
                    query,
                    Guid.NewGuid().ToString(),
                    CreateSchemaProvider(testCase.IsBinary),
                    LoggerResolver,
                    TestCompilationOptions));

            AssertPlacementDiagnostic(exception, query, testCase.FunctionName);
        }
    }

    [TestMethod]
    public void TryInterpret_InWhere_ShouldReportMq3033()
    {
        const string query = @"
            binary Packet {
                Value: int le
            };
            select 1
            from #test.files() f
            where TryInterpret<Packet>(f.Content) is not null";

        var exception = Assert.Throws<MusoqQueryException>(() =>
            CompileGeneratedQuery(
                query,
                Guid.NewGuid().ToString(),
                CreateSchemaProvider(isBinary: true),
                LoggerResolver,
                TestCompilationOptions));

        AssertPlacementDiagnostic(exception, query, "TryInterpret");
    }

    [TestMethod]
    public void InterpretAt_InHaving_ShouldReportMq3033()
    {
        const string query = @"
            binary Packet {
                Value: int le
            };
            select Count(*) as Total
            from #test.files() f
            group by f.Name
            having InterpretAt<Packet>(f.Content, 0) is not null";

        var exception = Assert.Throws<MusoqQueryException>(() =>
            CompileGeneratedQuery(
                query,
                Guid.NewGuid().ToString(),
                CreateSchemaProvider(isBinary: true),
                LoggerResolver,
                TestCompilationOptions));

        AssertPlacementDiagnostic(exception, query, "InterpretAt");
    }

    private static ISchemaProvider CreateSchemaProvider(bool isBinary)
    {
        if (isBinary)
        {
            return new BinarySchemaProvider(
                new Dictionary<string, IEnumerable<BinaryEntity>>
                {
                    ["#test"] = [new BinaryEntity { Name = "sample.bin", Content = [0, 0, 0, 0] }]
                });
        }

        return new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>>
            {
                ["#test"] = [new TextEntity { Name = "sample.txt", Text = "value" }]
            });
    }

    private static void AssertPlacementDiagnostic(
        MusoqQueryException exception,
        string query,
        string functionName)
    {
        AssertErrorEnvelope(
            exception,
            DiagnosticCode.MQ3033_InterpretFunctionOutsideApply,
            DiagnosticPhase.Bind,
            functionName);
        AssertHasGuidance(exception);

        var envelope = exception.PrimaryEnvelope;
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.IsNotNull(envelope.Offset);
        Assert.IsNotNull(envelope.Length);
        Assert.IsGreaterThan(0, envelope.Length.Value);
        Assert.IsNotNull(envelope.Snippet);
        StringAssert.Contains(envelope.Snippet, functionName);
        Assert.IsNotEmpty(envelope.Actions);
        Assert.IsTrue(
            envelope.Offset.Value >= 0 && envelope.Offset.Value < query.Length,
            $"Expected a query offset within the source, got {envelope.Offset.Value} for {functionName}.");
    }
}
