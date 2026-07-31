using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

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

    [TestMethod]
    public void SemanticMetadataSnapshot_ShouldCopyProviderOwnedColumnsIntoBoundContracts()
    {
        var providerColumn = new ProviderColumn("Value", 7, typeof(int));
        var frozen = MetadataSnapshotContractsFreezer.FreezeSchemaColumns(
            new[]
            {
                new KeyValuePair<string, IEnumerable<ISchemaColumn>>(
                    "source",
                    new[] { providerColumn })
            });

        var boundColumn = frozen["source"].Single();

        Assert.AreNotSame(providerColumn, boundColumn);
        Assert.IsInstanceOfType(boundColumn, typeof(BoundSchemaColumn));
        Assert.AreEqual(providerColumn.ColumnName, boundColumn.ColumnName);
        Assert.AreEqual(providerColumn.ColumnType, boundColumn.ColumnType);
    }

    [TestMethod]
    public void SemanticScopeArtifact_ShouldMaterializeProviderNeutralTableContracts()
    {
        var providerTable = new ProviderTable(
            [new ProviderColumn("Value", 0, typeof(int))],
            typeof(int));
        var providerSchema = new ProviderSchema(providerTable);
        var source = new Scope(null, -1, "Root");
        source.ScopeSymbolTable.AddSymbol(
            "items",
            new TableSymbol("items", providerSchema, providerTable, hasAlias: true));

        var artifact = SemanticScopeArtifact.Capture(source);
        var restored = artifact.CreateScope().ScopeSymbolTable.GetSymbol<TableSymbol>("items");
        var (schema, table, _) = restored.GetTableByAlias("items");

        Assert.AreNotSame(providerSchema, schema);
        Assert.AreNotSame(providerTable, table);
        Assert.IsInstanceOfType(schema, typeof(TransitionSchema));
        Assert.IsFalse(restored.FullTable.Columns.Any(column => column is ProviderColumn));
        Assert.AreEqual("Value", restored.GetColumnByAliasAndName("items", "Value")!.ColumnName);
    }

    [TestMethod]
    public void SemanticMetadataSnapshot_ShouldExposeSourceIdentityAndRequiredMemberContracts()
    {
        var lexer = new Musoq.Parser.Lexing.Lexer("select e.Name from #A.Entities() e", true);
        var root = new Musoq.Parser.Parser(lexer).ComposeAll();
        var visitor = new BuildMetadataAndInferTypesVisitor(
            new BasicSchemaProvider<BasicEntity>(
                new Dictionary<string, IEnumerable<BasicEntity>> { ["#A"] = [] }),
            new Dictionary<string, string[]>(),
            new NullLogger<BuildMetadataAndInferTypesVisitor>());

        root.Accept(new BuildMetadataAndInferTypesTraverseVisitor(visitor));
        var snapshot = visitor.CreateSemanticMetadataSnapshot();
        var source = snapshot.SourceContracts.Single();

        Assert.AreEqual("#A", source.Identity.SchemaName);
        Assert.AreEqual("Entities", source.Identity.MethodName);
        Assert.AreEqual("e", source.Identity.Alias);
        Assert.IsTrue(source.RequiredMethodSignature.Contains("#A.Entities", StringComparison.Ordinal));
        Assert.IsTrue(source.RequiredMemberSignatures.Any(signature => signature.StartsWith("Name:", StringComparison.Ordinal)));
        Assert.IsTrue(source.Columns.All(column => column is BoundSchemaColumn));
    }

    private sealed class ProviderSchema(ISchemaTable table)
        : SchemaBase("provider", new MethodsAggregator(new MethodsManager()))
    {
        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters) => table;
    }

    private sealed class ProviderTable(ISchemaColumn[] columns, Type entityType) : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } = columns;

        public SchemaTableMetadata Metadata { get; } = new(entityType);

        public ISchemaColumn? GetColumnByName(string name) =>
            Columns.SingleOrDefault(column => string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase));

        public ISchemaColumn[] GetColumnsByName(string name) =>
            Columns.Where(column => string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase)).ToArray();
    }

    private sealed class ProviderColumn(string name, int index, Type type) : ISchemaColumn
    {
        public string ColumnName { get; } = name;

        public int ColumnIndex { get; } = index;

        public Type ColumnType { get; } = type;
    }
}
