using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Optimization;
using Musoq.Targets.CSharpClr.Optimization.Codegen;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class AllocationRendererTests
{
    public static IEnumerable<object[]> Cases =>
    [
        ["class Query { void Run() { } }"],
        ["class Query { void Run() { int unused=0; int used=1; System.Console.WriteLine(used); } }"],
        ["class Query { void Run() { using var item=Open(); int effect=Next(); } }"],
        ["class Query { async System.Threading.Tasks.Task Run() { await using var item=Open(); var effect=await Next(); } }"],
        ["class Query { void Run() { int first=1, second=2; System.Console.WriteLine(first); { int nested=0; } } }"],
        ["#nullable enable\nclass Query { /// <summary>XML</summary>\n void Run() { int @escaped=0; System.Console.WriteLine(@escaped); } }"],
        ["class Query { FrozenSet<int> frozen; QueryRows rows; StructuralValue value; }"],
        ["class Query { QueryEnumerable<int> rows; StructuralTypeDescriptor type; StructuralFieldDescriptor field; StructuralDefaultDescriptor fallback; }"],
        ["class Query { private static void Helper() { int unused=0; } void Run() { Helper(); } }"]
    ];

    [TestMethod]
    [DynamicData(nameof(Cases))]
    public void Cleanup_ShouldMatchPreviousOutputAndCounters(string source)
    {
        var unit = AnnotateHelpers(SyntaxFactory.ParseCompilationUnit(source));
        var context = new OptimizationContext(OptimizationStage.CodegenReadability);
        var expected = new PreviousCleanupPass().Optimize(unit, context);

        var actual = new ExecutionCodegenOptimizationPass().Optimize(unit, context);

        Assert.AreEqual((expected.Plan.ToFullString(), expected.IsChanged, expected.Reason),
            (actual.Plan.ToFullString(), actual.IsChanged, actual.Reason));
    }

    [TestMethod]
    [DynamicData(nameof(Cases))]
    public void NamespaceScan_ShouldMatchPreviousMemberSearches(string source)
    {
        var members = SyntaxFactory.ParseCompilationUnit(source).Members.OfType<ClassDeclarationSyntax>()
            .Single().Members.Cast<SyntaxNode>().ToArray();
        var expected = PreviousNamespaces(members);

        var actual = CompiledQueryClassRenderer.DetectNamespaces(members);

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void Cleanup_ShouldPreserveHelperCandidateAnnotations()
    {
        var unit = AnnotateHelpers(SyntaxFactory.ParseCompilationUnit(
            "class Query { private static void Helper() { int unused=0; } void Run() { Helper(); } }"));
        var context = new OptimizationContext(OptimizationStage.CodegenReadability);
        var expected = new PreviousCleanupPass().Optimize(unit, context);

        var actual = new ExecutionCodegenOptimizationPass().Optimize(unit, context);

        CollectionAssert.AreEqual(Annotations(expected.Plan), Annotations(actual.Plan));
    }

    private static string[] Annotations(CompilationUnitSyntax unit) => unit.DescendantNodes()
        .OfType<MethodDeclarationSyntax>().Select(method => method.ToFullString() + ":" +
            string.Join(";", method.GetAnnotations(CodegenHelperExtractionMetadata.CandidateAnnotationKind)
                .Concat(method.GetAnnotations(HelperExtractionReadabilityPass.HelperExtractionAnnotationKind))
                .Select(annotation => annotation.Kind + "=" + annotation.Data))).ToArray();

    private static CompilationUnitSyntax AnnotateHelpers(CompilationUnitSyntax unit) => unit.ReplaceNodes(
        unit.DescendantNodes().OfType<MethodDeclarationSyntax>().Where(method => method.Identifier.ValueText == "Helper"),
        (original, _) => CodegenHelperExtractionMetadata.AnnotateCandidate(original,
            CodegenHelperExtractionRole.StoredTableBuild, "phase", "mutation"));

    private static int PreviousNamespaces(IReadOnlyList<SyntaxNode> members)
    {
        var frozen = members.Any(node => node.DescendantNodesAndSelf().OfType<GenericNameSyntax>().Any(name => name.Identifier.ValueText == "FrozenSet"));
        var rows = members.Any(node => node.DescendantNodesAndSelf().OfType<SimpleNameSyntax>().Any(name => name.Identifier.ValueText is "QueryRows" or "QueryTableEnumerable" or "QueryEnumerable"));
        var structural = members.Any(node => node.DescendantNodesAndSelf().OfType<SimpleNameSyntax>().Any(name => name.Identifier.ValueText is "StructuralTypeDescriptor" or "StructuralFieldDescriptor" or "StructuralDefaultDescriptor" or "StructuralValue"));
        return (frozen ? 1 : 0) | (rows ? 2 : 0) | (structural ? 4 : 0);
    }
}
