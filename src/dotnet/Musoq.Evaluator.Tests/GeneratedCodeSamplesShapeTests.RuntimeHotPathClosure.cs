using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void RuntimeHotPathIndexSamples_WhenCheckedIn_ShouldUseSharedCapacityShapes()
    {
        var samples = ReadSamples().ToDictionary(static sample => sample.FileName, static sample => sample.Content);

        var distinct = samples["Q08_Distinct.cs"];
        Assert.Contains("CreateKeySet [distinctKeys: ValueTuple<string, string>]", distinct);
        Assert.Contains("new HashSet<ValueTuple<string, string>>()", distinct);

        Assert.Contains("new HashSet<string>(left.Count + right.Count)", samples[UnionSampleFileName]);
        Assert.Contains("new HashSet<string>(right.Count)", samples[ExceptSampleFileName]);
        Assert.Contains("new HashSet<string>(right.Count)", samples[IntersectSampleFileName]);

        var subquery = samples[InSubqueryBasicSampleFileName];
        Assert.Contains("CreateKeySet [_sq_1Keys: string]", subquery);
        Assert.Contains("new HashSet<string>()", subquery);

        var directJoin = samples[InnerJoinSampleFileName];
        Assert.Contains("CreateHash [bHash: int -> BasicEntity]", directJoin);
        Assert.Contains("new Dictionary<int, HashJoinBucket<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>>()", directJoin);

        var sidecar = samples[CteSidecarHashJoinSampleFileName];
        Assert.Contains("CreateHash [cte0HashSidecar0Id: int -> Row]", sidecar);
        Assert.Contains("new Dictionary<int, HashJoinBucket<Cte0HashPayload0>>()", sidecar);
    }

    [TestMethod]
    public void RuntimeHotPathSidecarSamples_WhenCheckedIn_ShouldAvoidRetiredInternalShapes()
    {
        var samples = ReadSamples()
            .Where(static sample => sample.FileName is
                CteSidecarHashJoinSampleFileName or
                CteSidecarKeySetSemiJoinSampleFileName or
                CteSidecarFanoutThreeHashesSampleFileName or
                CteSidecarStagedGraphMixedSampleFileName)
            .ToArray();

        Assert.HasCount(4, samples);

        foreach (var sample in samples)
        {
            var generatedCode = ExtractGeneratedCodeSection(sample.Content);
            AssertSampleUsesTypedCteIndexResults(sample.Content);
            AssertSampleDoesNotUseCteRowResults(sample.Content);
            Assert.IsFalse(generatedCode.Contains("Musoq.Evaluator.Tables.Table BuildCte", StringComparison.Ordinal), sample.FileName);
            Assert.IsFalse(generatedCode.Contains("ContextMaterializer.Merge", StringComparison.Ordinal), sample.FileName);
            Assert.IsFalse(generatedCode.Contains("object[] _cteIndexResults", StringComparison.Ordinal), sample.FileName);
            Assert.IsFalse(generatedCode.Contains("EvaluationHelper.CastGeneratedRows<", StringComparison.Ordinal), sample.FileName);
            Assert.IsFalse(generatedCode.Contains("_tableResults[", StringComparison.Ordinal), sample.FileName);
        }
    }

    [TestMethod]
    public void ChunkedSourceSamples_WhenCheckedIn_ShouldUseCachedCountLoopsAndCancellationChecks()
    {
        var simpleSelect = ReadSamples()
            .Single(static sample => sample.FileName == "Q01_SimpleSelectWhere.cs")
            .Content;

        Assert.Contains("foreach (var ko3ikoChunk in ko3ikoRows)", simpleSelect);
        Assert.IsFalse(simpleSelect.Contains("ko3ikoChunk is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[]", StringComparison.Ordinal));
        Assert.IsFalse(simpleSelect.Contains("ko3ikoChunk is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>", StringComparison.Ordinal));
        Assert.Contains("if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> ko3ikoChunkView)", simpleSelect);
        Assert.Contains("if (ko3ikoChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] ko3ikoChunkViewArray)", simpleSelect);
        Assert.Contains("if (ko3ikoChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> ko3ikoChunkViewList)", simpleSelect);
        Assert.Contains("int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;", simpleSelect);
        Assert.Contains("for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)", simpleSelect);
        Assert.Contains("for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunk.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)", simpleSelect);
        Assert.Contains("if ((ko3ikoIndex & 1023) == 0)", simpleSelect);
        Assert.Contains("token.ThrowIfCancellationRequested();", simpleSelect);
    }

    [TestMethod]
    public void HelperChunkLoops_WhenCheckedIn_ShouldPassCancellationTokenAndEmitChecks()
    {
        var samples = ReadSamples().ToDictionary(static sample => sample.FileName, static sample => sample.Content);
        var cteGeneratedCode = ExtractGeneratedCodeSection(samples[CteWithJoinSampleFileName]);
        var cteMethod = CSharpSyntaxTree.ParseText(cteGeneratedCode)
            .GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single(static method => method.Identifier.ValueText == "BuildCte0");
        var cteMethodBody = cteMethod.ToFullString();

        Assert.Contains("CancellationToken token", cteMethod.ParameterList.ToFullString());
        Assert.IsTrue(
            Regex.IsMatch(
                cteMethodBody,
                @"if \(\([A-Za-z0-9_]+Index & 1023\) == 0\).*?token\.ThrowIfCancellationRequested\(\);",
                RegexOptions.Singleline),
            cteMethodBody);

        var hashJoinComputeMethod = GetComputeMethod(samples[InnerJoinSampleFileName]);
        Assert.Contains("foreach (var bChunk in bRows)", hashJoinComputeMethod);
        Assert.Contains("foreach (var aChunk in aRows)", hashJoinComputeMethod);
        Assert.Contains("token.ThrowIfCancellationRequested();", hashJoinComputeMethod);
        Assert.Contains("__musoqFinalShapeRows.Add(new ResultShape0(a.Name, b.Country));", hashJoinComputeMethod);
    }

    [TestMethod]
    public void EnumerableResultSamples_WhenCheckedIn_ShouldUseExplicitResultChunkHelperName()
    {
        var samples = ReadSamples();

        Assert.IsFalse(samples.Any(static sample =>
            sample.Content.Contains("EvaluationHelper.ConvertEnumerableToChunks", StringComparison.Ordinal)));
        Assert.IsTrue(samples.Any(static sample =>
            sample.Content.Contains("EvaluationHelper.ConvertEnumerableOutputToChunks", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void ForeachStatements_WhenCheckedIn_ShouldAlwaysUseBraces()
    {
        var offenders = ReadSamples()
            .SelectMany(static sample =>
            {
                var generatedCode = ExtractGeneratedCodeSection(sample.Content);
                return CSharpSyntaxTree.ParseText(generatedCode)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<ForEachStatementSyntax>()
                    .Where(static statement => statement.Statement is not BlockSyntax)
                    .Select(statement => $"{sample.FileName}: {GetLine(generatedCode, statement.SpanStart)}");
            })
            .ToArray();

        Assert.IsEmpty(
            offenders,
            $"Generated foreach statements must use braces: {string.Join(Environment.NewLine, offenders)}");
    }
}
