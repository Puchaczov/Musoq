using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Optimization;
using Musoq.Targets.CSharpClr.Optimization.Codegen;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class CodegenReadabilityOptimizerTests
{
    [TestMethod]
    public void Optimize_WhenDefaultReadabilityPassesRun_ShouldReturnInitialCodeAsOptimizedCode()
    {
        var initial = SyntaxFactory.CompilationUnit();

        var result = new CodegenReadabilityOptimizer().Optimize(initial);

        Assert.AreSame(initial, result.InitialCode);
        Assert.AreSame(initial, result.OptimizedCode);
        Assert.HasCount(6, result.Trace.Entries);
        Assert.AreEqual("DeterministicMemberOrdering", result.Trace.Entries[0].PassName);
        Assert.AreEqual("LocalDeclarationNormalization", result.Trace.Entries[1].PassName);
        Assert.AreEqual("DeadTemporaryCleanup", result.Trace.Entries[2].PassName);
        Assert.AreEqual("ControlFlowNormalization", result.Trace.Entries[3].PassName);
        Assert.AreEqual("HelperExtractionReadability", result.Trace.Entries[4].PassName);
        Assert.AreEqual("ReadabilityDecisionTrace", result.Trace.Entries[5].PassName);
        Assert.IsFalse(result.Trace.Entries.Any(entry => entry.IsChanged));
        AssertTraceEntriesAreMeaningful(result.Trace.Entries);
    }

    [TestMethod]
    public void DeterministicMemberOrdering_WhenRun_ShouldOrderClassMembersByVisibilityGroupAndName()
    {
        var initial = SyntaxFactory.ParseCompilationUnit(
            "public class Generated { private sealed class Nested {} private static void StaticHelper() {} private void ComputeTable_compiled_0() {} public event System.EventHandler Changed; public void Run() {} public string Name { get; } private int value; public Generated() {} protected void ProtectedHook() {} }");

        var result = new DeterministicMemberOrderingPass().Optimize(
            initial,
            new OptimizationContext(OptimizationStage.CodegenReadability));

        Assert.IsTrue(result.IsChanged);
        var members = result.Plan
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Single(static declaration => declaration.Identifier.ValueText == "Generated")
            .Members
            .Select(static member => member switch
            {
                FieldDeclarationSyntax field => field.Declaration.Variables[0].Identifier.ValueText,
                ConstructorDeclarationSyntax constructor => constructor.Identifier.ValueText,
                PropertyDeclarationSyntax property => property.Identifier.ValueText,
                EventFieldDeclarationSyntax eventField => eventField.Declaration.Variables[0].Identifier.ValueText,
                MethodDeclarationSyntax method => method.Identifier.ValueText,
                ClassDeclarationSyntax nestedClass => nestedClass.Identifier.ValueText,
                _ => member.Kind().ToString()
            })
            .ToArray();

        CollectionAssert.AreEqual(
            new[] { "value", "Generated", "Name", "Changed", "Run", "ProtectedHook", "ComputeTable_compiled_0", "StaticHelper", "Nested" },
            members);
    }

    [TestMethod]
    public void LocalDeclarationNormalization_WhenRun_ShouldSplitSafeMultiVariableDeclarations()
    {
        var initial = SyntaxFactory.ParseCompilationUnit(
            "public class Generated { public void Run() { int alpha = 1, beta = 2; System.Console.WriteLine(alpha + beta); } }");

        var result = new LocalDeclarationNormalizationPass().Optimize(
            initial,
            new OptimizationContext(OptimizationStage.CodegenReadability));

        Assert.IsTrue(result.IsChanged);
        var locals = result.Plan
            .DescendantNodes()
            .OfType<LocalDeclarationStatementSyntax>()
            .ToArray();

        Assert.HasCount(2, locals);
        Assert.IsTrue(locals.All(static local => local.Declaration.Variables.Count == 1));
        CollectionAssert.AreEqual(
            new[] { "alpha", "beta" },
            locals
                .Select(static local => local.Declaration.Variables.Single().Identifier.ValueText)
                .ToArray());
    }

    [TestMethod]
    public void DeadTemporaryCleanup_WhenRun_ShouldRemoveOnlyUnusedLiteralOrDefaultLocals()
    {
        var initial = SyntaxFactory.ParseCompilationUnit(
            "public class Generated { public void Run() { int unused = 1; int used = 2; var kept = Make(); System.Console.WriteLine(used); } private object Make() => new object(); }");

        var result = new DeadTemporaryCleanupPass().Optimize(
            initial,
            new OptimizationContext(OptimizationStage.CodegenReadability));

        Assert.IsTrue(result.IsChanged);
        var text = result.Plan.ToFullString();

        Assert.IsFalse(text.Contains("unused", System.StringComparison.Ordinal));
        Assert.IsTrue(text.Contains("int used = 2", System.StringComparison.Ordinal));
        Assert.IsTrue(text.Contains("var kept = Make()", System.StringComparison.Ordinal));
    }

    [TestMethod]
    public void ControlFlowNormalization_WhenRun_ShouldRemoveEmptyBlockStatements()
    {
        var initial = SyntaxFactory.ParseCompilationUnit(
            "public class Generated { public void Run() { ; System.Console.WriteLine(1); ; } }");

        var result = new ControlFlowNormalizationPass().Optimize(
            initial,
            new OptimizationContext(OptimizationStage.CodegenReadability));

        Assert.IsTrue(result.IsChanged);
        Assert.IsFalse(result.Plan.DescendantNodes().OfType<EmptyStatementSyntax>().Any());
    }

    [TestMethod]
    public void HelperExtractionReadability_WhenRun_ShouldApproveMetadataBackedHelpers()
    {
        var initial = SyntaxFactory.ParseCompilationUnit(
            "public class Generated { private void ComputeTable_compiled_0() { AppendRows(); } private static void AppendRows() { } private void OnPhaseChanged() { } }");
        var helper = initial
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single(static method => method.Identifier.ValueText == "AppendRows");
        initial = initial.ReplaceNode(
            helper,
            CodegenHelperExtractionMetadata.AnnotateCandidate(
                helper,
                CodegenHelperExtractionRole.HashJoinProbe,
                "same-phase",
                "hash-and-append-targets"));
        var invocation = initial
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Single(static node => node.Expression.ToString() == "AppendRows");
        initial = initial.ReplaceNode(
            invocation,
            CodegenHelperExtractionMetadata.AnnotateCallSite(
                invocation,
                CodegenHelperExtractionRole.HashJoinProbe,
                "AppendRows",
                "same-phase",
                "hash-and-append-targets"));

        var result = new HelperExtractionReadabilityPass().Optimize(
            initial,
            new OptimizationContext(OptimizationStage.CodegenReadability));

        Assert.IsTrue(result.IsChanged);
        Assert.AreEqual(initial.ToFullString(), result.Plan.ToFullString());
        Assert.Contains("Approved 1 metadata-backed helper extraction candidate(s)", result.Reason);
        Assert.Contains("1 candidate call site(s)", result.Reason);
        Assert.AreEqual(
            2,
            result.Plan
                .DescendantNodes()
                .Count(static node => node.HasAnnotations(HelperExtractionReadabilityPass.HelperExtractionAnnotationKind)));
    }

    [TestMethod]
    public void HelperExtractionReadability_WhenInlineBlockHasNoMetadata_ShouldNotInventHelper()
    {
        var initial = SyntaxFactory.ParseCompilationUnit(
            "public class Generated { public void Run() { var value = 1; if (value > 0) { value = value + 1; value = value + 2; } System.Console.WriteLine(value); } }");

        var result = new HelperExtractionReadabilityPass().Optimize(
            initial,
            new OptimizationContext(OptimizationStage.CodegenReadability));

        Assert.IsFalse(result.IsChanged);
        Assert.AreEqual(initial.ToFullString(), result.Plan.ToFullString());
        Assert.Contains("No metadata-approved helper extraction candidates", result.Reason);
        Assert.IsFalse(result.Plan.DescendantNodes().Any(static node =>
            node.HasAnnotations(HelperExtractionReadabilityPass.HelperExtractionAnnotationKind)));
    }

    [TestMethod]
    public void HelperExtractionReadability_WhenPrivateStaticHelperHasNoCandidateMetadata_ShouldSkipIt()
    {
        var initial = SyntaxFactory.ParseCompilationUnit(
            "public class Generated { private void ComputeTable_compiled_0() { AppendRows(); } private static void AppendRows() { } }");

        var result = new HelperExtractionReadabilityPass().Optimize(
            initial,
            new OptimizationContext(OptimizationStage.CodegenReadability));

        Assert.IsFalse(result.IsChanged);
        Assert.Contains("none carried helper extraction candidate metadata", result.Reason);
        Assert.IsFalse(result.Plan.DescendantNodes().Any(static node =>
            node.HasAnnotations(HelperExtractionReadabilityPass.HelperExtractionAnnotationKind)));
    }

    [TestMethod]
    public void HelperExtractionReadability_WhenInlineBlockHasApprovedMetadata_ShouldExtractHelper()
    {
        var initial = SyntaxFactory.ParseCompilationUnit(
            "public class Generated { public void Run() { System.Console.WriteLine(1); } }");
        var runBody = initial
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single(static method => method.Identifier.ValueText == "Run")
            .Body!;
        initial = initial.ReplaceNode(runBody, CodegenHelperExtractionMetadata.AnnotateInlineCandidate(
            runBody,
            CodegenHelperExtractionRole.HashJoinProbe,
            "AppendHotRows",
            new CodegenHelperExtractionBoundaries(
                "same-phase",
                "no-mutation",
                "no-movement",
                "no-movement",
                "no-movement",
                "none",
                "void",
                "AppendHotRows")));

        var result = new HelperExtractionReadabilityPass().Optimize(
            initial,
            new OptimizationContext(OptimizationStage.CodegenReadability));

        Assert.IsTrue(result.IsChanged);
        Assert.Contains("extracted 1 metadata-approved inline helper block", result.Reason);
        var methods = result.Plan.DescendantNodes().OfType<MethodDeclarationSyntax>().ToArray();
        var run = methods.Single(static method => method.Identifier.ValueText == "Run");
        var helper = methods.Single(static method => method.Identifier.ValueText == "AppendHotRows");
        Assert.Contains("AppendHotRows();", run.Body!.ToFullString());
        Assert.Contains("System.Console.WriteLine(1);", helper.Body!.ToFullString());
        Assert.IsTrue(helper.HasAnnotations(HelperExtractionReadabilityPass.HelperExtractionAnnotationKind));
        Assert.IsTrue(result.Plan.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(static invocation =>
            invocation.Expression.ToString() == "AppendHotRows" &&
            invocation.HasAnnotations(HelperExtractionReadabilityPass.HelperExtractionAnnotationKind)));
    }

    [TestMethod]
    public void HelperExtractionReadability_WhenInlineHelperNameAlreadyExists_ShouldReserveUniqueName()
    {
        var initial = SyntaxFactory.ParseCompilationUnit(
            "public class Generated { public void Run() { System.Console.WriteLine(1); } private static void AppendHotRows() { } }");
        var runBody = initial
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single(static method => method.Identifier.ValueText == "Run")
            .Body!;
        initial = initial.ReplaceNode(runBody, CodegenHelperExtractionMetadata.AnnotateInlineCandidate(
            runBody,
            CodegenHelperExtractionRole.HashJoinProbe,
            "AppendHotRows",
            new CodegenHelperExtractionBoundaries(
                "same-phase",
                "no-mutation",
                "no-movement",
                "no-movement",
                "no-movement",
                "none",
                "void",
                "AppendHotRows")));

        var result = new HelperExtractionReadabilityPass().Optimize(
            initial,
            new OptimizationContext(OptimizationStage.CodegenReadability));

        Assert.IsTrue(result.IsChanged);
        var methods = result.Plan.DescendantNodes().OfType<MethodDeclarationSyntax>().ToArray();
        Assert.IsTrue(methods.Any(static method => method.Identifier.ValueText == "AppendHotRows"));
        Assert.IsTrue(methods.Any(static method => method.Identifier.ValueText == "AppendHotRows_1"));
        var run = methods.Single(static method => method.Identifier.ValueText == "Run");
        Assert.Contains("AppendHotRows_1();", run.Body!.ToFullString());
    }

    [TestMethod]
    public void HelperExtractionReadability_WhenInlineBlockInvokesLifecycleMethod_ShouldSkipExtraction()
    {
        var initial = SyntaxFactory.ParseCompilationUnit(
            "public class Generated { public void Run() { OnPhaseChanged(); } private void OnPhaseChanged() { } }");
        var runBody = initial
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single(static method => method.Identifier.ValueText == "Run")
            .Body!;
        initial = initial.ReplaceNode(runBody, CodegenHelperExtractionMetadata.AnnotateInlineCandidate(
            runBody,
            CodegenHelperExtractionRole.HashJoinProbe,
            "AppendHotRows",
            new CodegenHelperExtractionBoundaries(
                "same-phase",
                "no-mutation",
                "no-movement",
                "no-movement",
                "no-movement",
                "none",
                "void",
                "AppendHotRows")));

        var result = new HelperExtractionReadabilityPass().Optimize(
            initial,
            new OptimizationContext(OptimizationStage.CodegenReadability));

        Assert.IsFalse(result.IsChanged);
        Assert.Contains("1 skipped inline candidate block", result.Reason);
        Assert.IsFalse(result.Plan.DescendantNodes().OfType<MethodDeclarationSyntax>().Any(static method =>
            method.Identifier.ValueText == "AppendHotRows"));
    }

    [TestMethod]
    public void HelperExtractionReadability_WhenInlineBlockContainsControlFlow_ShouldSkipExtraction()
    {
        var initial = SyntaxFactory.ParseCompilationUnit(
            "public class Generated { public void Run() { return; } }");
        var runBody = initial
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single(static method => method.Identifier.ValueText == "Run")
            .Body!;
        initial = initial.ReplaceNode(runBody, CodegenHelperExtractionMetadata.AnnotateInlineCandidate(
            runBody,
            CodegenHelperExtractionRole.HashJoinProbe,
            "AppendHotRows",
            new CodegenHelperExtractionBoundaries(
                "same-phase",
                "no-mutation",
                "no-movement",
                "no-movement",
                "no-movement",
                "none",
                "void",
                "AppendHotRows")));

        var result = new HelperExtractionReadabilityPass().Optimize(
            initial,
            new OptimizationContext(OptimizationStage.CodegenReadability));

        Assert.IsFalse(result.IsChanged);
        Assert.Contains("1 skipped inline candidate block", result.Reason);
        Assert.IsFalse(result.Plan.DescendantNodes().OfType<MethodDeclarationSyntax>().Any(static method =>
            method.Identifier.ValueText == "AppendHotRows"));
    }

    [TestMethod]
    public void HelperExtractionReadability_WhenInlineBlockHasCaptures_ShouldSkipExtraction()
    {
        var initial = SyntaxFactory.ParseCompilationUnit(
            "public class Generated { public void Run() { var value = 1; System.Console.WriteLine(value); } }");
        var runBody = initial
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single(static method => method.Identifier.ValueText == "Run")
            .Body!;
        initial = initial.ReplaceNode(runBody, CodegenHelperExtractionMetadata.AnnotateInlineCandidate(
            runBody,
            CodegenHelperExtractionRole.HashJoinProbe,
            "AppendHotRows",
            new CodegenHelperExtractionBoundaries(
                "same-phase",
                "no-mutation",
                "no-movement",
                "no-movement",
                "no-movement",
                "explicit",
                "void",
                "AppendHotRows")));

        var result = new HelperExtractionReadabilityPass().Optimize(
            initial,
            new OptimizationContext(OptimizationStage.CodegenReadability));

        Assert.IsFalse(result.IsChanged);
        Assert.Contains("1 skipped inline candidate block", result.Reason);
        Assert.IsFalse(result.Plan.DescendantNodes().OfType<MethodDeclarationSyntax>().Any(static method =>
            method.Identifier.ValueText == "AppendHotRows"));
    }

    [TestMethod]
    public void HelperExtractionMetadata_WhenAnnotated_ShouldRoundTripWithoutChangingCodeText()
    {
        var method = SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                "BuildHash")
            .WithBody(SyntaxFactory.Block());
        var invocation = SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("BuildHash"));

        var annotatedMethod = CodegenHelperExtractionMetadata.AnnotateCandidate(
            method,
            CodegenHelperExtractionRole.HashJoinBuild,
            "same-phase",
            "hash-target");
        var annotatedInvocation = CodegenHelperExtractionMetadata.AnnotateCallSite(
            invocation,
            CodegenHelperExtractionRole.HashJoinBuild,
            "BuildHash",
            "same-phase",
            "hash-target");

        Assert.AreEqual(method.ToFullString(), annotatedMethod.ToFullString());
        Assert.AreEqual(invocation.ToFullString(), annotatedInvocation.ToFullString());
        Assert.IsTrue(CodegenHelperExtractionMetadata.TryGetCandidate(annotatedMethod, out var helperInfo));
        Assert.IsTrue(CodegenHelperExtractionMetadata.TryGetCallSite(annotatedInvocation, out var callInfo));
        Assert.AreEqual(CodegenHelperExtractionRole.HashJoinBuild, helperInfo.Role);
        Assert.AreEqual(CodegenHelperExtractionCandidateKind.ExistingHelper, helperInfo.CandidateKind);
        Assert.AreEqual("BuildHash", helperInfo.HelperName);
        Assert.AreEqual("same-phase", helperInfo.PhaseBoundary);
        Assert.AreEqual("hash-target", helperInfo.MutationBoundary);
        Assert.AreEqual("no-movement", helperInfo.CancellationBoundary);
        Assert.AreEqual("no-movement", helperInfo.ProgressBoundary);
        Assert.AreEqual("no-movement", helperInfo.QueryStatisticsBoundary);
        Assert.AreEqual("explicit", helperInfo.CaptureBoundary);
        Assert.AreEqual("existing-helper", helperInfo.ReturnBoundary);
        Assert.AreEqual("BuildHash", helperInfo.OrderingKey);
        Assert.AreEqual(helperInfo, callInfo);
    }

    [TestMethod]
    public void HelperExtractionMetadata_WhenInlineCandidateAnnotated_ShouldRoundTripWithoutChangingCodeText()
    {
        var block = SyntaxFactory.Block(SyntaxFactory.ParseStatement("System.Console.WriteLine(1);"));
        var boundaries = new CodegenHelperExtractionBoundaries(
            "same-phase",
            "no-mutation",
            "no-movement",
            "no-movement",
            "no-movement",
            "none",
            "void",
            "AppendHotRows");

        var annotated = CodegenHelperExtractionMetadata.AnnotateInlineCandidate(
            block,
            CodegenHelperExtractionRole.HashJoinProbe,
            "AppendHotRows",
            boundaries);

        Assert.AreEqual(block.ToFullString(), annotated.ToFullString());
        Assert.IsTrue(CodegenHelperExtractionMetadata.TryGetInlineCandidate(annotated, out var info));
        Assert.AreEqual(CodegenHelperExtractionRole.HashJoinProbe, info.Role);
        Assert.AreEqual(CodegenHelperExtractionCandidateKind.InlineBlock, info.CandidateKind);
        Assert.AreEqual("AppendHotRows", info.HelperName);
        Assert.AreEqual("same-phase", info.PhaseBoundary);
        Assert.AreEqual("no-mutation", info.MutationBoundary);
        Assert.AreEqual("no-movement", info.CancellationBoundary);
        Assert.AreEqual("no-movement", info.ProgressBoundary);
        Assert.AreEqual("no-movement", info.QueryStatisticsBoundary);
        Assert.AreEqual("none", info.CaptureBoundary);
        Assert.AreEqual("void", info.ReturnBoundary);
        Assert.AreEqual("AppendHotRows", info.OrderingKey);
    }

    private static void AssertTraceEntriesAreMeaningful(
        IReadOnlyList<OptimizationTraceEntry> entries)
    {
        Assert.IsTrue(entries.All(static entry => entry.Stage == OptimizationStage.CodegenReadability));
        Assert.IsTrue(entries.All(static entry => !string.IsNullOrWhiteSpace(entry.PassName)));
        Assert.IsTrue(entries.All(static entry => !string.IsNullOrWhiteSpace(entry.Outcome)));
        Assert.IsTrue(entries.All(static entry => !string.IsNullOrWhiteSpace(entry.Reason)));
        Assert.IsTrue(entries.All(static entry =>
            string.Equals(entry.Outcome, entry.IsChanged ? "Changed" : "NoChange", System.StringComparison.Ordinal)));
    }
}
