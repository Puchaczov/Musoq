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
        Assert.IsFalse(typeof(SemanticMetadataPhaseCoordinator).IsNested);
        Assert.IsFalse(typeof(SemanticRewritePhaseCoordinator).IsNested);

        var metadataMethod = typeof(SemanticMetadataPhaseCoordinator).GetMethod(nameof(
            SemanticMetadataPhaseCoordinator.Analyze), BindingFlags.Instance | BindingFlags.Public)!;
        var rewriteMethod = typeof(SemanticRewritePhaseCoordinator).GetMethod(nameof(
            SemanticRewritePhaseCoordinator.Rewrite), BindingFlags.Instance | BindingFlags.Public)!;

        Assert.AreEqual(typeof(SemanticMetadataPhaseResult), metadataMethod.ReturnType);
        Assert.AreEqual(typeof(RootNode), rewriteMethod.ReturnType);
        Assert.IsTrue(rewriteMethod.GetParameters().Any(parameter =>
            parameter.ParameterType == typeof(SemanticScopeArtifact)));
        Assert.IsFalse(rewriteMethod.GetParameters().Any(parameter =>
            parameter.ParameterType == typeof(Scope) || parameter.ParameterType == typeof(ScopeWalker)));
    }
}
