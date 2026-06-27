using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.CodeGeneration;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class ExecutionCSharpRendererTests
{
    [TestMethod]
    public void WhenRenderingDefaultTable_ForSimpleProjection_ShouldEmitDirectRowsWithoutComputeTable()
    {
        using var workspace = new AdhocWorkspace();
        var generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);
        var context = new RenderContext(
            generator,
            new RenderContextOptions(AssemblyName: "Query.Compiled_Test"));
        var renderer = new CSharpRenderer(context);

        var outcome = renderer.TryRenderExecutionQueryMethod(CreatePlan(), "compiled");
        Assert.IsTrue(outcome.IsSupported, outcome.UnsupportedReason);
        var method = outcome.Method!.Value;
        Assert.AreEqual(FinalResultSinkKind.TableRowsMaterialized, method.Metadata.FinalResultSinkKind);
        Assert.AreEqual(QueryResultRowPathKind.DirectRows, method.Metadata.RowPathKind);
        Assert.IsFalse(method.Metadata.RequiresComputeTableMethod);
        Assert.AreEqual(FinalProjectionSinkRejectionKind.None, method.Metadata.FinalSinkRejectionKind);
        Assert.IsNull(method.Metadata.FinalSinkRejectionReason);
        context.AddClassMember(method.MethodDeclaration);
        var code = renderer.RenderCompilationUnit("compiled").NormalizeWhitespace().ToFullString();

        StringAssert.Contains(code, "using Musoq.Evaluator.Runtime;");
        StringAssert.Contains(code, "public Table Run(CancellationToken token)");
        StringAssert.Contains(code, "return QueryRows.DeferredTable<ResultRow0>");
        StringAssert.Contains(code, "QueryRows.DeferredTable<ResultRow0>(\"result\", __columns_");
        StringAssert.Contains(code, "private IEnumerable<ResultRow0> ComputeRows_compiled_0(");
        StringAssert.Contains(code, "return new QueryTableEnumerable<ResultRow0>(");
        StringAssert.Contains(code, "TableProjectionRows.ProjectRowsSerial<");
        StringAssert.Contains(code, "new ResultRow0(p.Name)");
        Assert.IsFalse(code.Contains("QueryRows.DeferredTable<ResultRow0>(\"result\", new Column[]", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("private sealed class ResultShape0 : Row", StringComparison.Ordinal));
        StringAssert.Contains(code, "onCompleted: () =>");
        Assert.IsFalse(code.Contains("private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("private Table ComputeTable_compiled_0(", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("QueryRows.FromTable<ResultRow0>", StringComparison.Ordinal));
        Assert.IsFalse(
            code.Contains("return ComputeTable_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, token);", StringComparison.Ordinal),
            "Default table Run should materialize from ComputeRows_* rather than call ComputeTable_* directly.");
    }

    [TestMethod]
    public void WhenRenderingTableViaRows_ForSimpleProjection_ShouldEmitDirectRowsWithoutComputeTable()
    {
        using var workspace = new AdhocWorkspace();
        var generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);
        var context = new RenderContext(
            generator,
            new RenderContextOptions(
                AssemblyName: "Query.Compiled_Test",
                ResultMode: QueryResultMode.TableViaRows,
                FinalResultSinkKind: FinalResultSinkKind.TableRowsMaterialized));
        var renderer = new CSharpRenderer(context);

        var outcome = renderer.TryRenderExecutionQueryMethod(CreatePlan(), "compiled");
        Assert.IsTrue(outcome.IsSupported, outcome.UnsupportedReason);
        var method = outcome.Method!.Value;
        Assert.AreEqual(FinalResultSinkKind.TableRowsMaterialized, method.Metadata.FinalResultSinkKind);
        Assert.AreEqual(QueryResultRowPathKind.DirectRows, method.Metadata.RowPathKind);
        Assert.IsFalse(method.Metadata.RequiresComputeTableMethod);
        context.AddClassMember(method.MethodDeclaration);
        var code = renderer.RenderCompilationUnit("compiled").NormalizeWhitespace().ToFullString();

        StringAssert.Contains(code, "using Musoq.Evaluator.Runtime;");
        StringAssert.Contains(code, "public Table Run(CancellationToken token)");
        StringAssert.Contains(code, "return QueryRows.DeferredTable<ResultRow0>");
        StringAssert.Contains(code, "QueryRows.DeferredTable<ResultRow0>(\"result\", __columns_");
        StringAssert.Contains(code, "private IEnumerable<ResultRow0> ComputeRows_compiled_0(");
        StringAssert.Contains(code, "return new QueryTableEnumerable<ResultRow0>(");
        StringAssert.Contains(code, "TableProjectionRows.ProjectRowsSerial<");
        StringAssert.Contains(code, "new ResultRow0(p.Name)");
        Assert.IsFalse(code.Contains("QueryRows.DeferredTable<ResultRow0>(\"result\", new Column[]", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("private Table ComputeTable_compiled_0(", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("QueryRows.FromTable<ResultRow0>", StringComparison.Ordinal));
        Assert.IsFalse(
            code.Contains("return ComputeTable_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, token);", StringComparison.Ordinal),
            "TableViaRows Run should materialize from ComputeRows_* rather than call ComputeTable_* directly.");
    }

    [TestMethod]
    public void WhenRenderingDefaultTable_ForBlockingShape_ShouldEmitDirectShapeRows()
    {
        using var workspace = new AdhocWorkspace();
        var generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);
        var context = new RenderContext(
            generator,
            new RenderContextOptions(AssemblyName: "Query.Compiled_Test"));
        var renderer = new CSharpRenderer(context);

        var outcome = renderer.TryRenderExecutionQueryMethod(CreatePostOperationMetadataPlan(), "compiled");
        Assert.IsTrue(outcome.IsSupported, outcome.UnsupportedReason);
        var method = outcome.Method!.Value;
        Assert.AreEqual(FinalResultSinkKind.TableRowsMaterialized, method.Metadata.FinalResultSinkKind);
        Assert.AreEqual(QueryResultRowPathKind.DirectRows, method.Metadata.RowPathKind);
        Assert.IsFalse(method.Metadata.RequiresComputeTableMethod);
        Assert.AreEqual(FinalProjectionSinkRejectionKind.None, method.Metadata.FinalSinkRejectionKind);
        Assert.IsNull(method.Metadata.FinalSinkRejectionReason);
        context.AddClassMember(method.MethodDeclaration);
        var code = renderer.RenderCompilationUnit("compiled").NormalizeWhitespace().ToFullString();

        StringAssert.Contains(code, "private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(");
        StringAssert.Contains(code, "private IEnumerable<ResultRow0> ComputeRows_compiled_0(");
        StringAssert.Contains(code, "var __musoqFinalShapeRows = new List<ResultShape0>();");
        StringAssert.Contains(code, "__musoqFinalShapeRows.Add(");
        StringAssert.Contains(code, "return __musoqFinalShapeRows;");
        Assert.IsFalse(code.Contains("private Table ComputeTable_compiled_0(", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("QueryRows.FromTable<ResultRow0>", StringComparison.Ordinal));
    }

    [TestMethod]
    public void WhenRenderingDefaultTable_ForParallelProjection_ShouldEmitShardBackedRows()
    {
        using var workspace = new AdhocWorkspace();
        var generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);
        var context = new RenderContext(
            generator,
            new RenderContextOptions(AssemblyName: "Query.Compiled_Test"));
        var renderer = new CSharpRenderer(context);

        var outcome = renderer.TryRenderExecutionQueryMethod(CreateParallelProjectionPlan(), "compiled");
        Assert.IsTrue(outcome.IsSupported, outcome.UnsupportedReason);
        var method = outcome.Method!.Value;
        Assert.AreEqual(FinalResultSinkKind.GeneratedRowParallelShards, method.Metadata.FinalResultSinkKind);
        Assert.AreEqual(QueryResultRowPathKind.ShardRows, method.Metadata.RowPathKind);
        Assert.IsFalse(method.Metadata.RequiresComputeTableMethod);
        Assert.AreEqual(FinalProjectionSinkRejectionKind.None, method.Metadata.FinalSinkRejectionKind);
        context.AddClassMember(method.MethodDeclaration);
        var code = renderer.RenderCompilationUnit("compiled").NormalizeWhitespace().ToFullString();

        StringAssert.Contains(code, "private IEnumerable<ResultRow0> ComputeRows_compiled_0(");
        StringAssert.Contains(code, "EvaluationHelper.ProjectChunkedRowsParallel<");
        StringAssert.Contains(code, "EvaluationHelper.GetParallelProjectionRowsOrEmpty<");
        StringAssert.Contains(code, "QueryRows.FromRowShards(EvaluationHelper.ProjectRowsParallel<");
        StringAssert.Contains(code, "TableProjectionRows.ProjectRowsSerial<");
        StringAssert.Contains(code, "return new QueryTableEnumerable<ResultRow0>(");
        Assert.IsFalse(code.Contains("private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("TypedProjectionRows.ProjectValuesParallel<", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("yield return new ResultRow0(__musoqShapeRow.Name);", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("private Table ComputeTable_compiled_0(", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("QueryRows.FromTable<ResultRow0>", StringComparison.Ordinal));
    }

    [TestMethod]
    public void WhenRenderingDefaultTable_ForParallelProjectionWithRowLocalCse_ShouldEmitOptionalRowShards()
    {
        using var workspace = new AdhocWorkspace();
        var generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);
        var context = new RenderContext(
            generator,
            new RenderContextOptions(AssemblyName: "Query.Compiled_Test"));
        var renderer = new CSharpRenderer(context);

        var outcome = renderer.TryRenderExecutionQueryMethod(CreateParallelProjectionPlanWithRowLocalMethodCse(), "compiled");
        Assert.IsTrue(outcome.IsSupported, outcome.UnsupportedReason);
        var method = outcome.Method!.Value;
        Assert.AreEqual(FinalResultSinkKind.GeneratedRowParallelShards, method.Metadata.FinalResultSinkKind);
        Assert.AreEqual(QueryResultRowPathKind.ShardRows, method.Metadata.RowPathKind);
        Assert.AreEqual(FinalProjectionSinkRejectionKind.None, method.Metadata.FinalSinkRejectionKind);
        context.AddClassMember(method.MethodDeclaration);
        var code = renderer.RenderCompilationUnit("compiled").NormalizeWhitespace().ToFullString();

        StringAssert.Contains(code, "EvaluationHelper.ProjectChunkedRowsParallel<");
        StringAssert.Contains(code, "QueryRows.FromRowShards(EvaluationHelper.ProjectRowsParallel<");
        StringAssert.Contains(code, "TableProjectionRows.ProjectOptionalRowsSerial<");
        StringAssert.Contains(code, "string upper = (string)libraryBase0.ToUpper(p.Name);");
        StringAssert.Contains(code, "return new ResultRow0(p.Name, upper);");
        Assert.IsFalse(code.Contains("private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("TypedProjectionRows.ProjectOptionalValuesParallel<", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("private Table ComputeTable_compiled_0(", StringComparison.Ordinal));
    }

    [TestMethod]
    public void WhenRenderingTypedEnumerable_ForSimpleProjection_ShouldReportDirectRowsMetadata()
    {
        using var workspace = new AdhocWorkspace();
        var generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);
        var context = new RenderContext(
            generator,
            new RenderContextOptions(
                AssemblyName: "Query.Compiled_Test",
                ResultMode: QueryResultMode.TypedEnumerable,
                OutputType: typeof(TestNameDto),
                FinalResultSinkKind: FinalResultSinkKind.TypedSerialEnumerable));
        var renderer = new CSharpRenderer(context);

        var outcome = renderer.TryRenderExecutionQueryMethod(CreatePlan(), "compiled");
        Assert.IsTrue(outcome.IsSupported, outcome.UnsupportedReason);
        var method = outcome.Method!.Value;
        Assert.AreEqual(FinalResultSinkKind.TypedSerialEnumerable, method.Metadata.FinalResultSinkKind);
        Assert.AreEqual(QueryResultRowPathKind.DirectRows, method.Metadata.RowPathKind);
        Assert.IsFalse(method.Metadata.RequiresComputeTableMethod);
        Assert.AreEqual(FinalProjectionSinkRejectionKind.None, method.Metadata.FinalSinkRejectionKind);
        context.AddClassMember(method.MethodDeclaration);
        var code = renderer.RenderCompilationUnit("compiled").NormalizeWhitespace().ToFullString();

        StringAssert.Contains(code, "private IEnumerable<Musoq.Evaluator.Tests.IR.ExecutionCSharpRendererTests.TestNameDto> ComputeRows_compiled_0(");
        StringAssert.Contains(code, "TypedProjectionRows.ProjectValuesSerial<");
        Assert.IsFalse(code.Contains("private Table ComputeTable_compiled_0(", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("QueryRows.FromTable<ResultRow0>", StringComparison.Ordinal));
    }

    [TestMethod]
    public void WhenRenderingTypedEnumerable_ForParallelProjection_ShouldEmitTypedShardBackedRows()
    {
        using var workspace = new AdhocWorkspace();
        var generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);
        var context = new RenderContext(
            generator,
            new RenderContextOptions(
                AssemblyName: "Query.Compiled_Test",
                ResultMode: QueryResultMode.TypedEnumerable,
                OutputType: typeof(TestNameDto),
                FinalResultSinkKind: FinalResultSinkKind.TypedSerialEnumerable));
        var renderer = new CSharpRenderer(context);

        var outcome = renderer.TryRenderExecutionQueryMethod(CreateParallelProjectionPlan(), "compiled");
        Assert.IsTrue(outcome.IsSupported, outcome.UnsupportedReason);
        var method = outcome.Method!.Value;
        Assert.AreEqual(FinalResultSinkKind.TypedParallelShards, method.Metadata.FinalResultSinkKind);
        Assert.AreEqual(QueryResultRowPathKind.ShardRows, method.Metadata.RowPathKind);
        Assert.IsFalse(method.Metadata.RequiresComputeTableMethod);
        Assert.AreEqual(FinalProjectionSinkRejectionKind.None, method.Metadata.FinalSinkRejectionKind);
        context.AddClassMember(method.MethodDeclaration);
        var code = renderer.RenderCompilationUnit("compiled").NormalizeWhitespace().ToFullString();

        StringAssert.Contains(code, "private IEnumerable<Musoq.Evaluator.Tests.IR.ExecutionCSharpRendererTests.TestNameDto> ComputeRows_compiled_0(");
        StringAssert.Contains(code, "TypedProjectionRows.ProjectChunkedValuesParallel<");
        StringAssert.Contains(code, "EvaluationHelper.GetParallelProjectionRowsOrEmpty<");
        StringAssert.Contains(code, "QueryRows.FromShards(TypedProjectionRows.ProjectValuesParallel<");
        StringAssert.Contains(code, "TypedProjectionRows.ProjectValuesSerial<");
        Assert.IsFalse(code.Contains("private Table ComputeTable_compiled_0(", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("QueryRows.FromTable<ResultRow0>", StringComparison.Ordinal));
    }

    [TestMethod]
    public void WhenRenderingTypedEnumerable_ForProjectionPostOperations_ShouldEmitTypedPostOperationRows()
    {
        using var workspace = new AdhocWorkspace();
        var generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);
        var context = new RenderContext(
            generator,
            new RenderContextOptions(
                AssemblyName: "Query.Compiled_Test",
                ResultMode: QueryResultMode.TypedEnumerable,
                OutputType: typeof(TestNameDto),
                FinalResultSinkKind: FinalResultSinkKind.TypedSerialEnumerable));
        var renderer = new CSharpRenderer(context);

        var outcome = renderer.TryRenderExecutionQueryMethod(CreateProjectionPostOperationPlan(), "compiled");
        Assert.IsTrue(outcome.IsSupported, outcome.UnsupportedReason);
        var method = outcome.Method!.Value;
        Assert.AreEqual(FinalResultSinkKind.TypedSerialEnumerable, method.Metadata.FinalResultSinkKind);
        Assert.AreEqual(QueryResultRowPathKind.DirectRows, method.Metadata.RowPathKind);
        Assert.IsFalse(method.Metadata.RequiresComputeTableMethod);
        context.AddClassMember(method.MethodDeclaration);
        var code = renderer.RenderCompilationUnit("compiled").NormalizeWhitespace().ToFullString();

        StringAssert.Contains(code, "public IEnumerable<Musoq.Evaluator.Tests.IR.ExecutionCSharpRendererTests.TestNameDto> Run(CancellationToken token)");
        StringAssert.Contains(code, "TableProjectionRows.ProjectRowsSerial<");
        StringAssert.Contains(code, "TypedPostOperationRows.Distinct<ResultRow0>");
        StringAssert.Contains(code, "TypedPostOperationRows.Order<ResultRow0>");
        StringAssert.Contains(code, "__musoqTypedPostRows = __musoqTypedPostRows.Skip(1);");
        StringAssert.Contains(code, "__musoqTypedPostRows = __musoqTypedPostRows.Take(2);");
        StringAssert.Contains(code, "TypedPostOperationRows.Project<ResultRow0, Musoq.Evaluator.Tests.IR.ExecutionCSharpRendererTests.TestNameDto>");
        Assert.IsFalse(code.Contains("private Table ComputeTable_compiled_0(", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("QueryRows.FromTable<ResultRow0>", StringComparison.Ordinal));
    }

    [TestMethod]
    public void WhenRenderingTypedEnumerable_ForHiddenSortProjectionPostOperations_ShouldEmitTypedShapeRows()
    {
        using var workspace = new AdhocWorkspace();
        var generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);
        var context = new RenderContext(
            generator,
            new RenderContextOptions(
                AssemblyName: "Query.Compiled_Test",
                ResultMode: QueryResultMode.TypedEnumerable,
                OutputType: typeof(TestNameDto),
                FinalResultSinkKind: FinalResultSinkKind.TypedSerialEnumerable));
        var renderer = new CSharpRenderer(context);

        var outcome = renderer.TryRenderExecutionQueryMethod(CreateHiddenSortProjectionPostOperationPlan(), "compiled");
        Assert.IsTrue(outcome.IsSupported, outcome.UnsupportedReason);
        var method = outcome.Method!.Value;
        Assert.AreEqual(FinalResultSinkKind.TypedSerialEnumerable, method.Metadata.FinalResultSinkKind);
        Assert.AreEqual(QueryResultRowPathKind.DirectRows, method.Metadata.RowPathKind);
        Assert.IsFalse(method.Metadata.RequiresComputeTableMethod);
        Assert.AreEqual(FinalProjectionSinkRejectionKind.None, method.Metadata.FinalSinkRejectionKind);
        context.AddClassMember(method.MethodDeclaration);
        var code = renderer.RenderCompilationUnit("compiled").NormalizeWhitespace().ToFullString();

        StringAssert.Contains(code, "private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(");
        StringAssert.Contains(code, "private IEnumerable<Musoq.Evaluator.Tests.IR.ExecutionCSharpRendererTests.TestNameDto> ComputeRows_compiled_0(");
        StringAssert.Contains(code, "foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(");
        StringAssert.Contains(code, "yield return new Musoq.Evaluator.Tests.IR.ExecutionCSharpRendererTests.TestNameDto((string)__musoqShapeRow.Name);");
        Assert.IsFalse(code.Contains("private Table ComputeTable_compiled_0(", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("QueryRows.FromTable<ResultRow0>", StringComparison.Ordinal));
    }

    public sealed record TestNameDto(string Name);
}
