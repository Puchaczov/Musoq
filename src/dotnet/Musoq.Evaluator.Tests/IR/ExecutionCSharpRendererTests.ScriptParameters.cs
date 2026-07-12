using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using ExecutionCSharpRenderer = Musoq.Targets.CSharpClr.ExecutionCSharpRenderer;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class ExecutionCSharpRendererTests
{
    [TestMethod]
    public void RenderMethod_WhenScriptParametersDeclared_ShouldBindLocalsBeforeOpeningSources()
    {
        var renderer = new ExecutionCSharpRenderer(
        [
            new ScriptParameterDefinition("author", typeof(string), false, null),
            new ScriptParameterDefinition("limit", typeof(int), true, 100)
        ]);
        var method = renderer.RenderMethod(CreatePlan(), "ExecutePlan");
        var code = method.NormalizeWhitespace().ToFullString();

        Assert.Contains("var paramAuthor = ScriptParameterBinder.GetRequired<string>(__musoqExecutionState.Parameters, \"author\");", code);
        Assert.Contains("var paramLimit = ScriptParameterBinder.GetOptional<int>(__musoqExecutionState.Parameters, \"limit\", 100);", code);

        var fromPhaseIndex = code.IndexOf("OnPhaseChanged(\"ExecutePlan\", QueryPhase.From);", StringComparison.Ordinal);
        var authorBindingIndex = code.IndexOf("var paramAuthor", StringComparison.Ordinal);
        var sourceOpenIndex = code.IndexOf("provider.GetSchema(\"test\")", StringComparison.Ordinal);

        Assert.IsLessThan(authorBindingIndex, fromPhaseIndex);
        Assert.IsLessThan(sourceOpenIndex, authorBindingIndex);
    }

    [TestMethod]
    public void RenderBlock_WhenScriptParameterReadIsUsed_ShouldReferenceBoundLocal()
    {
        var renderer = new ExecutionCSharpRenderer(
        [
            new ScriptParameterDefinition("author", typeof(string), false, null)
        ]);
        var block = renderer.RenderBlock(new ExecutionBlock(
        [
            new ExecutionLet(
                new ExecutionVariable("requestedAuthor", typeof(string)),
                new ExecutionScriptParameterRead("author", typeof(string)))
        ]));
        var code = block.NormalizeWhitespace().ToFullString();

        Assert.Contains("string requestedAuthor = paramAuthor;", code);
    }

    [TestMethod]
    public void RenderBlock_WhenScriptParameterReadIsMissingFromRenderContext_ShouldThrow()
    {
        var renderer = new ExecutionCSharpRenderer();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            renderer.RenderBlock(new ExecutionBlock(
            [
                new ExecutionLet(
                    new ExecutionVariable("requestedAuthor", typeof(string)),
                    new ExecutionScriptParameterRead("author", typeof(string)))
            ])));

        Assert.AreEqual("Script parameter 'author' is not declared in render context.", exception.Message);
    }

    [TestMethod]
    public void RenderMethod_WhenScriptVariablesDeclared_ShouldEmitConstAndTypedLocals()
    {
        var renderer = new ExecutionCSharpRenderer(
            scriptVariableDefinitions:
            [
                new ScriptVariableDefinition("topic", typeof(string), "important", true),
                new ScriptVariableDefinition(
                    "created",
                    typeof(DateTime),
                    new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc),
                    false)
            ]);
        var method = renderer.RenderMethod(CreatePlan(), "ExecutePlan");
        var code = method.NormalizeWhitespace().ToFullString();

        Assert.Contains("const string letTopic = \"important\";", code);
        Assert.Contains("DateTime letCreated = new DateTime(", code);
        Assert.AreEqual(0, CountOccurrences(code, "ScriptParameterBinder.Get"));
    }

    [TestMethod]
    public void RenderBlock_WhenScriptVariableReadIsUsed_ShouldReferenceDeclaredLocal()
    {
        var renderer = new ExecutionCSharpRenderer(
            scriptVariableDefinitions:
            [
                new ScriptVariableDefinition("topic", typeof(string), "important", true)
            ]);
        var block = renderer.RenderBlock(new ExecutionBlock(
        [
            new ExecutionLet(
                new ExecutionVariable("requestedTopic", typeof(string)),
                new ExecutionScriptVariableRead("topic", typeof(string)))
        ]));
        var code = block.NormalizeWhitespace().ToFullString();

        Assert.Contains("string requestedTopic = letTopic;", code);
    }

    [TestMethod]
    public void RenderBlock_WhenScriptVariableReadIsMissingFromRenderContext_ShouldThrow()
    {
        var renderer = new ExecutionCSharpRenderer();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            renderer.RenderBlock(new ExecutionBlock(
            [
                new ExecutionLet(
                    new ExecutionVariable("requestedTopic", typeof(string)),
                    new ExecutionScriptVariableRead("topic", typeof(string)))
            ])));

        Assert.AreEqual("Script variable 'topic' is not declared in render context.", exception.Message);
    }

    [TestMethod]
    public void RenderMethod_WhenScriptParameterReadIsUsed_ShouldReadDictionaryOnlyDuringBinding()
    {
        var renderer = new ExecutionCSharpRenderer(
        [
            new ScriptParameterDefinition("author", typeof(string), false, null)
        ]);
        var method = renderer.RenderMethod(
            CreateProjectionPlan(
                "Q_ParameterProjection",
                "Author",
                typeof(string),
                new ExecutionScriptParameterRead("author", typeof(string))),
            "ExecutePlan");
        var code = method.NormalizeWhitespace().ToFullString();

        Assert.Contains("var __musoqExecutionState = ExecutionState.Capture(Parameters);", code);
        Assert.Contains("var paramAuthor = ScriptParameterBinder.GetRequired<string>(__musoqExecutionState.Parameters, \"author\");", code);
        Assert.Contains("result.Add(new ResultRow0(paramAuthor));", code);
        Assert.AreEqual(1, CountOccurrences(code, "ExecutionState.Capture(Parameters)"));
        Assert.AreEqual(2, CountOccurrences(code, "__musoqExecutionState.Parameters"));
    }

    [TestMethod]
    public void RenderMethod_WhenScriptParameterLocalNamesCollide_ShouldEmitDeterministicSuffixes()
    {
        var renderer = new ExecutionCSharpRenderer(
        [
            new ScriptParameterDefinition("author-name", typeof(string), false, null),
            new ScriptParameterDefinition("author_name", typeof(string), false, null)
        ]);
        var method = renderer.RenderMethod(CreatePlan(), "ExecutePlan");
        var code = method.NormalizeWhitespace().ToFullString();

        Assert.Contains("var paramAuthorName = ScriptParameterBinder.GetRequired<string>(__musoqExecutionState.Parameters, \"author-name\");", code);
        Assert.Contains("var paramAuthorName1 = ScriptParameterBinder.GetRequired<string>(__musoqExecutionState.Parameters, \"author_name\");", code);
        Assert.IsLessThan(
            code.IndexOf("var paramAuthorName1 = ", StringComparison.Ordinal), code.IndexOf("var paramAuthorName = ", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RenderClassMembers_WhenHashBuildHelperUsesScriptParameter_ShouldReceiveTypedCapture()
    {
        var renderer = new ExecutionCSharpRenderer(
        [
            new ScriptParameterDefinition("country", typeof(string), false, null)
        ]);
        var plan = CreateHashBuildScriptParameterCapturePlan();
        var methodCode = renderer.RenderMethod(plan, "ExecutePlan").NormalizeWhitespace().ToFullString();
        var helperCode = RenderClassMembersCode(renderer, plan);

        Assert.Contains("var paramCountry = ScriptParameterBinder.GetRequired<string>(__musoqExecutionState.Parameters, \"country\");", methodCode);
        Assert.AreEqual(1, CountOccurrences(methodCode, "ScriptParameterBinder.Get"));
        Assert.Contains("BuildHash(leftRows, hash, token, paramCountry);", methodCode);
        Assert.Contains("private static void BuildHash(IEnumerable<Musoq.Evaluator.Tables.Row> leftRows, Dictionary<string, HashJoinBucket<Musoq.Evaluator.Tables.Row>> hash, CancellationToken token, string paramCountry)", helperCode);
        Assert.Contains("token.ThrowIfCancellationRequested();", helperCode);
        Assert.Contains("string key = paramCountry;", helperCode);
        AssertNoParameterAccessInHelpers(helperCode);
    }

    [TestMethod]
    public void RenderClassMembers_WhenHashBuildHelperIsCreated_ShouldAnnotateHelperExtractionMetadata()
    {
        var renderer = new ExecutionCSharpRenderer(
        [
            new ScriptParameterDefinition("country", typeof(string), false, null)
        ]);
        var plan = CreateHashBuildScriptParameterCapturePlan();

        var helper = renderer
            .RenderClassMembers(plan)
            .OfType<MethodDeclarationSyntax>()
            .Single(static method => method.Identifier.ValueText == "BuildHash");
        var invocation = renderer
            .RenderMethod(plan, "ExecutePlan")
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Single(static node => node.Expression.ToString() == "BuildHash");

        Assert.IsTrue(CodegenHelperExtractionMetadata.TryGetCandidate(helper, out var helperInfo));
        Assert.IsTrue(CodegenHelperExtractionMetadata.TryGetCallSite(invocation, out var callInfo));
        Assert.AreEqual(CodegenHelperExtractionRole.HashJoinBuild, helperInfo.Role);
        Assert.AreEqual("BuildHash", helperInfo.HelperName);
        Assert.AreEqual(helperInfo, callInfo);
    }

    [TestMethod]
    public void RenderClassMembers_WhenHashBuildHelperUsesScriptVariable_ShouldReceiveTypedCapture()
    {
        var renderer = new ExecutionCSharpRenderer(
            scriptVariableDefinitions:
            [
                new ScriptVariableDefinition("country", typeof(string), "PL", true)
            ]);
        var plan = CreateHashBuildScriptVariableCapturePlan();
        var methodCode = renderer.RenderMethod(plan, "ExecutePlan").NormalizeWhitespace().ToFullString();
        var helperCode = RenderClassMembersCode(renderer, plan);

        Assert.Contains("const string letCountry = \"PL\";", methodCode);
        Assert.AreEqual(0, CountOccurrences(methodCode, "ScriptParameterBinder.Get"));
        Assert.Contains("BuildHash(leftRows, hash, token, letCountry);", methodCode);
        Assert.Contains("private static void BuildHash(IEnumerable<Musoq.Evaluator.Tables.Row> leftRows, Dictionary<string, HashJoinBucket<Musoq.Evaluator.Tables.Row>> hash, CancellationToken token, string letCountry)", helperCode);
        Assert.Contains("token.ThrowIfCancellationRequested();", helperCode);
        Assert.Contains("string key = letCountry;", helperCode);
        AssertNoParameterAccessInHelpers(helperCode);
    }

    [TestMethod]
    public void RenderClassMembers_WhenHashProbeHelperUsesScriptParameter_ShouldReceiveTypedCapture()
    {
        var renderer = new ExecutionCSharpRenderer(
        [
            new ScriptParameterDefinition("label", typeof(string), false, null)
        ]);
        var plan = CreateHashProbeScriptParameterCapturePlan();
        var methodCode = renderer.RenderMethod(plan, "ExecutePlan").NormalizeWhitespace().ToFullString();
        var helperCode = RenderClassMembersCode(renderer, plan);

        Assert.Contains("var paramLabel = ScriptParameterBinder.GetRequired<string>(__musoqExecutionState.Parameters, \"label\");", methodCode);
        Assert.AreEqual(1, CountOccurrences(methodCode, "ScriptParameterBinder.Get"));
        Assert.Contains("AppendLeftJoinRows(rightRows, hash, result, token, paramLabel);", methodCode);
        Assert.Contains("private static void AppendLeftJoinRows(IEnumerable<Musoq.Evaluator.Tables.Row> rightRows, Dictionary<int, HashJoinBucket<Musoq.Evaluator.Tables.Row>> hash, Musoq.Evaluator.Tables.Table result, CancellationToken token, string paramLabel)", helperCode);
        Assert.Contains("token.ThrowIfCancellationRequested();", helperCode);
        Assert.Contains("result.Add(new ResultRow0(paramLabel));", helperCode);
        AssertNoParameterAccessInHelpers(helperCode);
    }

    [TestMethod]
    public void RenderClassMembers_WhenSingleKeyAggregateHelpersUseScriptParameters_ShouldReceiveTypedCaptures()
    {
        var renderer = new ExecutionCSharpRenderer(
        [
            new ScriptParameterDefinition("country", typeof(string), false, null),
            new ScriptParameterDefinition("include", typeof(bool), false, null)
        ]);
        var plan = CreateSingleKeyAggregateScriptParameterCapturePlan();
        var methodCode = renderer.RenderMethod(plan, "ExecutePlan").NormalizeWhitespace().ToFullString();
        var helperCode = RenderClassMembersCode(renderer, plan);

        Assert.Contains("var paramCountry = ScriptParameterBinder.GetRequired<string>(__musoqExecutionState.Parameters, \"country\");", methodCode);
        Assert.Contains("var paramInclude = ScriptParameterBinder.GetRequired<bool>(__musoqExecutionState.Parameters, \"include\");", methodCode);
        Assert.AreEqual(2, CountOccurrences(methodCode, "ScriptParameterBinder.Get"));
        Assert.Contains("PopulateResultSingleKeyGroups(rows, rootGroup, groupsToFinalize, groups, token, paramCountry);", methodCode);
        Assert.Contains("FinalizeResultSingleKeyGroups(result, groupsToFinalize, token, paramInclude);", methodCode);
        Assert.Contains("IEnumerable<Musoq.Evaluator.Tables.Row> rows", helperCode);
        Assert.Contains("string paramCountry", helperCode);
        Assert.Contains("bool paramInclude", helperCode);
        Assert.Contains("string groupKey = paramCountry;", helperCode);
        Assert.Contains("if (paramInclude)", helperCode);
        AssertNoParameterAccessInHelpers(helperCode);
    }

    [TestMethod]
    public void RenderClassMembers_WhenParallelAggregateUsesScriptParameter_ShouldStoreTypedWorkerCapture()
    {
        var renderer = new ExecutionCSharpRenderer(
        [
            new ScriptParameterDefinition("country", typeof(string), false, null)
        ]);
        var plan = CreateParallelAggregateScriptParameterCapturePlan();
        var methodCode = renderer.RenderMethod(plan, "ExecutePlan").NormalizeWhitespace().ToFullString();
        var helperCode = RenderClassMembersCode(renderer, plan);

        Assert.Contains("var paramCountry = ScriptParameterBinder.GetRequired<string>(__musoqExecutionState.Parameters, \"country\");", methodCode);
        Assert.AreEqual(1, CountOccurrences(methodCode, "ScriptParameterBinder.Get"));
        Assert.Contains("ParallelSingleKeyAggregate_0(groupsToFinalizeParallelRows, 4, token, paramCountry)", methodCode);
        Assert.Contains("private static List<ResultAggregateGroup> ParallelSingleKeyAggregate_0(IReadOnlyList<Musoq.Evaluator.Tables.Row> rows, int maxDegreeOfParallelism, CancellationToken cancellationToken, string paramCountry)", helperCode);
        Assert.Contains("private readonly string _paramCountry;", helperCode);
        Assert.Contains("public ParallelSingleKeyAggregateWorker_0(IReadOnlyList<Musoq.Evaluator.Tables.Row> rows, int workerCount, List<ResultAggregateGroup>[] shards, CancellationToken cancellationToken, string paramCountry)", helperCode);
        Assert.Contains("ParallelSingleKeyAggregateShard_0(_rows, _workerCount, _shards, _cancellationToken, shardIndex, _paramCountry);", helperCode);
        Assert.Contains("string groupKey = paramCountry;", helperCode);
        AssertNoParameterAccessInHelpers(helperCode);
    }

    [TestMethod]
    public void RenderClassMembers_WhenParallelFilterProjectUsesScriptParameters_ShouldReceiveTypedCaptures()
    {
        var renderer = new ExecutionCSharpRenderer(
        [
            new ScriptParameterDefinition("include", typeof(bool), false, null),
            new ScriptParameterDefinition("label", typeof(string), false, null)
        ]);
        var plan = CreateParallelFilterProjectScriptParameterCapturePlan();
        var methodCode = renderer.RenderMethod(plan, "ExecutePlan").NormalizeWhitespace().ToFullString();
        var helperCode = RenderClassMembersCode(renderer, plan);

        Assert.Contains("var paramInclude = ScriptParameterBinder.GetRequired<bool>(__musoqExecutionState.Parameters, \"include\");", methodCode);
        Assert.Contains("var paramLabel = ScriptParameterBinder.GetRequired<string>(__musoqExecutionState.Parameters, \"label\");", methodCode);
        Assert.AreEqual(2, CountOccurrences(methodCode, "ScriptParameterBinder.Get"));
        Assert.Contains("PopulateResult(result, rows, token, paramInclude, paramLabel);", methodCode);
        Assert.Contains("private static void PopulateResult(Musoq.Evaluator.Tables.Table result, IEnumerable<Musoq.Evaluator.Tables.Row> rowRows, CancellationToken token, bool paramInclude, string paramLabel)", helperCode);
        Assert.Contains("if (paramInclude)", helperCode);
        Assert.Contains("return new ResultRow0(paramLabel);", helperCode);
        AssertNoParameterAccessInHelpers(helperCode);
    }

    [TestMethod]
    public void RenderClassMembers_WhenStoredTableBuildUsesScriptParameter_ShouldReceiveTypedCapture()
    {
        var renderer = new ExecutionCSharpRenderer(
        [
            new ScriptParameterDefinition("country", typeof(string), false, null)
        ]);
        var plan = CreateStoredTableBuildScriptParameterCapturePlan();
        var methodCode = renderer.RenderMethod(plan, "ExecutePlan").NormalizeWhitespace().ToFullString();
        var helperCode = RenderClassMembersCode(renderer, plan);

        Assert.Contains("var paramCountry = ScriptParameterBinder.GetRequired<string>(__musoqExecutionState.Parameters, \"country\");", methodCode);
        Assert.AreEqual(1, CountOccurrences(methodCode, "ScriptParameterBinder.Get"));
        Assert.Contains("BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, OnDataSourceProgress, _cteRowResults, paramCountry)", methodCode);
        Assert.IsLessThan(
            methodCode.IndexOf("BuildCte0(", StringComparison.Ordinal), methodCode.IndexOf("var paramCountry =", StringComparison.Ordinal));
        Assert.Contains("private static List<ResultRow0> BuildCte0(", helperCode);
        Assert.Contains("string paramCountry", helperCode);
        Assert.Contains("cte.Add(new ResultRow0(paramCountry));", helperCode);
        AssertNoParameterAccessInHelpers(helperCode);
    }

    [TestMethod]
    public void RenderClassMembers_WhenRankingWindowKeyExtractionUsesScriptParameters_ShouldReceiveTypedCaptures()
    {
        var renderer = new ExecutionCSharpRenderer(
        [
            new ScriptParameterDefinition("country", typeof(string), false, null),
            new ScriptParameterDefinition("sortLabel", typeof(string), false, null)
        ]);
        var plan = CreateRankingWindowKeyExtractionScriptParameterCapturePlan();
        var methodCode = renderer.RenderMethod(plan, "ExecutePlan").NormalizeWhitespace().ToFullString();
        var helperCode = RenderClassMembersCode(renderer, plan);

        Assert.Contains("var paramCountry = ScriptParameterBinder.GetRequired<string>(__musoqExecutionState.Parameters, \"country\");", methodCode);
        Assert.Contains("var paramSortLabel = ScriptParameterBinder.GetRequired<string>(__musoqExecutionState.Parameters, \"sortLabel\");", methodCode);
        Assert.AreEqual(2, CountOccurrences(methodCode, "ScriptParameterBinder.Get"));
        Assert.Contains("ExtractRankingsWindowKeys(windowRows, rankingsPartitionKeys, rankingsOrderKeys, paramCountry, paramSortLabel);", methodCode);
        Assert.Contains("private static void ExtractRankingsWindowKeys(", helperCode);
        Assert.Contains("string paramCountry", helperCode);
        Assert.Contains("string paramSortLabel", helperCode);
        Assert.Contains("rankingsPartitionKeys[windowIndex] = (string)(paramCountry);", helperCode);
        Assert.Contains("rankingsOrderKeys[windowIndex] = new WindowRankingsOrderKeysKey(paramSortLabel);", helperCode);
        AssertNoParameterAccessInHelpers(helperCode);
    }

    [TestMethod]
    public void RenderClassMembers_WhenWindowAppendRowsUsesScriptParameter_ShouldReceiveTypedCapture()
    {
        var renderer = new ExecutionCSharpRenderer(
        [
            new ScriptParameterDefinition("label", typeof(string), false, null)
        ]);
        var plan = CreateWindowAppendRowsScriptParameterCapturePlan();
        var methodCode = renderer.RenderMethod(plan, "ExecutePlan").NormalizeWhitespace().ToFullString();
        var helperCode = RenderClassMembersCode(renderer, plan);

        Assert.Contains("var paramLabel = ScriptParameterBinder.GetRequired<string>(__musoqExecutionState.Parameters, \"label\");", methodCode);
        Assert.AreEqual(1, CountOccurrences(methodCode, "ScriptParameterBinder.Get"));
        Assert.Contains("AppendResultWindowRows(resultWindowRows, result, paramLabel);", methodCode);
        Assert.Contains("private static void AppendResultWindowRows(", helperCode);
        Assert.Contains("string paramLabel", helperCode);
        Assert.Contains("result.Add(new ResultRow0(paramLabel));", helperCode);
        AssertNoParameterAccessInHelpers(helperCode);
    }
}
