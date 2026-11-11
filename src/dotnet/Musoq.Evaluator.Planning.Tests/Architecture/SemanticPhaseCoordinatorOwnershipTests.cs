using Microsoft.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class SemanticPhaseCoordinatorOwnershipTests
{
    [TestMethod]
    public void PhaseCoordinators_ShouldBeTopLevelAndUseImmutableHandoffs()
    {
        Assert.IsFalse(CompilerContractSymbols.For(typeof(SemanticMetadataPhaseCoordinator)).IsNested);
        Assert.IsFalse(CompilerContractSymbols.For(typeof(SemanticRewritePhaseCoordinator)).IsNested);

        var metadataMethod = CompilerContractSymbols.For(typeof(SemanticMetadataPhaseCoordinator)).GetMethod(nameof(
            SemanticMetadataPhaseCoordinator.Analyze), BindingFlags.Instance | BindingFlags.Public)!;
        var rewriteMethod = CompilerContractSymbols.For(typeof(SemanticRewritePhaseCoordinator)).GetMethod(nameof(
            SemanticRewritePhaseCoordinator.Rewrite), BindingFlags.Instance | BindingFlags.Public)!;

        Assert.IsTrue(SymbolEqualityComparer.Default.Equals(CompilerContractSymbols.For(typeof(SemanticMetadataPhaseResult)), metadataMethod.ReturnType));
        Assert.IsTrue(SymbolEqualityComparer.Default.Equals(CompilerContractSymbols.For(typeof(RootNode)), rewriteMethod.ReturnType));
        Assert.IsTrue(rewriteMethod.GetParameters().Any(parameter =>
            SymbolEqualityComparer.Default.Equals(parameter.ParameterType, CompilerContractSymbols.For(typeof(SemanticScopeArtifact)))));
        Assert.IsFalse(rewriteMethod.GetParameters().Any(parameter =>
            SymbolEqualityComparer.Default.Equals(parameter.ParameterType, CompilerContractSymbols.For(typeof(Scope))) || SymbolEqualityComparer.Default.Equals(parameter.ParameterType, CompilerContractSymbols.For(typeof(ScopeWalker)))));
    }
}
