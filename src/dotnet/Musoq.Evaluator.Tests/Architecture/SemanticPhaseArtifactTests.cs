using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class SemanticPhaseArtifactTests : BasicEntityTestBase
{
    [TestMethod]
    public void QueryAnalyzer_ShouldExposeTheCompletedSemanticHandoff()
    {
        var analyzer = new QueryAnalyzer(
            new BasicSchemaProvider<BasicEntity>(
                new Dictionary<string, IEnumerable<BasicEntity>> { ["#A"] = [] }));

        var result = analyzer.Analyze("select Name from #A.Entities()");

        Assert.IsNotNull(result.SemanticArtifacts);
        Assert.IsNotNull(result.SemanticArtifacts.ParsedQuery);
        Assert.IsNotNull(result.SemanticArtifacts.NormalizedQuery);
        Assert.IsNotNull(result.SemanticArtifacts.MetadataQuery);
        Assert.IsNull(result.SemanticArtifacts.RewrittenQuery);
        Assert.IsNotNull(result.SemanticArtifacts.ResultShape);
        Assert.AreEqual(result.Diagnostics.Count, result.SemanticArtifacts.Diagnostics.Count);
    }

    [TestMethod]
    public void SemanticMetadataSnapshot_ShouldRejectMutationThroughReadOnlyCollections()
    {
        var lexer = new Musoq.Parser.Lexing.Lexer("select Name from #A.Entities()", true);
        var root = new Musoq.Parser.Parser(lexer).ComposeAll();
        var visitor = new BuildMetadataAndInferTypesVisitor(
            new BasicSchemaProvider<BasicEntity>(
                new Dictionary<string, IEnumerable<BasicEntity>> { ["#A"] = [] }),
            new Dictionary<string, string[]>(),
            new NullLogger<BuildMetadataAndInferTypesVisitor>());

        root.Accept(new BuildMetadataAndInferTypesTraverseVisitor(visitor));
        var snapshot = visitor.CreateSemanticMetadataSnapshot();

        Assert.Throws<NotSupportedException>(() =>
            ((IDictionary<string, IReadOnlyList<ISchemaColumn>>)snapshot.InferredColumnsByAlias).Clear());

        var columns = snapshot.InferredColumns.Values.First();
        Assert.Throws<NotSupportedException>(() =>
            ((IList<ISchemaColumn>)columns).Add(columns[0]));

        Assert.Throws<InvalidOperationException>(() => visitor.CreateSemanticMetadataSnapshot());
    }

    [TestMethod]
    public void RewritePhaseInput_ShouldRejectAnInvalidPhaseTransition()
    {
        var input = new RewriteQueryPhaseInput(null!, null!);

        Assert.Throws<ArgumentNullException>(() =>
            new RewriteQueryVisitor(input));
    }

    [TestMethod]
    public void SemanticScopeArtifact_ShouldMaterializeIndependentSnapshots()
    {
        var source = new Scope(null, -1, "Root");
        source["phase"] = "metadata";
        source.AddScope("Query");
        source.ScopeSymbolTable.AddOrGetSymbol<AliasesSymbol>("aliases").AddAlias("original");
        var artifact = SemanticScopeArtifact.Capture(source);

        source["phase"] = "mutated";
        source.AddScope("AfterHandoff");

        var first = artifact.CreateScope();
        var second = artifact.CreateScope();
        first.AddScope("OnlyFirst");
        first.ScopeSymbolTable.GetSymbol<AliasesSymbol>("aliases").AddAlias("only-first");

        Assert.AreEqual("metadata", first["phase"]);
        Assert.AreEqual(2, first.Child.Count);
        Assert.AreEqual(1, second.Child.Count);
        Assert.IsFalse(second.Child.Any(scope => scope.Name == "OnlyFirst"));
        Assert.IsFalse(second.ScopeSymbolTable.GetSymbol<AliasesSymbol>("aliases").ContainsAlias("only-first"));
    }

    [TestMethod]
    public void SemanticMetadataSnapshotBuilder_ShouldDependOnTypedInputsOnly()
    {
        var visitorType = typeof(BuildMetadataAndInferTypesVisitor);
        var builderType = typeof(SemanticMetadataSnapshotBuilder);

        Assert.IsTrue(builderType.GetMethods(System.Reflection.BindingFlags.Instance |
                                             System.Reflection.BindingFlags.Public |
                                             System.Reflection.BindingFlags.NonPublic)
            .Any(method => method.Name == "Build"));
        Assert.IsFalse(builderType.GetMethods(System.Reflection.BindingFlags.Instance |
                                              System.Reflection.BindingFlags.Static |
                                              System.Reflection.BindingFlags.Public |
                                              System.Reflection.BindingFlags.NonPublic)
            .SelectMany(method => method.GetParameters())
            .Any(parameter => parameter.ParameterType == visitorType));
    }
}
