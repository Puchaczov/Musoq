using System;
using System.Linq;
using System.Text.Json;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Formatter.Tests.Verification;

namespace Musoq.Formatter.Tests;

[TestClass]
public sealed class CompilerAuditTests
{
    [TestMethod]
    public void AllSourceTypesAndMembers_ShouldHaveReachableTypedProjections()
    {
        ProjectionAudit.Verify(VerificationCompiler.Current.Tests, CoverageManifest.SourceTypes);
    }

    [TestMethod]
    public void FormatterOwnedCode_ShouldUseNoRuntimeReflectionOrDynamicAccess()
    {
        var violations = ReflectionAudit.Violations(VerificationCompiler.Current.Formatter)
            .Concat(ReflectionAudit.Violations(VerificationCompiler.Current.Tests)).ToArray();

        CollectionAssert.AreEqual(Array.Empty<string>(), violations, string.Join("\n", violations));
    }

    [TestMethod]
    [DataRow("Formatter")]
    [DataRow("Tests")]
    public void EvaluatedProjectsAndAssets_ShouldRespectTheParserOnlyBoundary(string project)
    {
        var permitted = new[] { "Musoq.Parser" }.ToHashSet(StringComparer.Ordinal);
        if (project == "Tests")
            permitted.Add("Musoq.Formatter");

        DependencyAudit.VerifyEvaluatedInputs(project, permitted);
    }

    [TestMethod]
    [DataRow("AstProjector.Expressions1.cs", "Write(value.Alias);", "Write(value.Name);")]
    [DataRow("AstProjector.Expressions1.cs", "Write(value.Arguments);", "Member(nameof(value.Arguments));")]
    [DataRow("AstProjector.Expressions1.cs", "private void Project(global::Musoq.Parser.Nodes.AddNode value)", "private void MissingProjection(global::Musoq.Parser.Nodes.AddNode value)")]
    [DataRow("AstProjector.Expressions1.cs", "writer.Write(item)", "writer.Null()")]
    [DataRow("AstProjector.Expressions1.cs", "writer.Write(pair.Then);", "writer.Write(pair.When);")]
    [DataRow("AstProjector.Expressions1.cs", "Member(nameof(value.Alias));", "Member(\"Alias\");")]
    [DataRow("AstProjector.Scripts1.cs", "Write(AstParsing.Parse(value.InnerQueryText));", "Write(value.InnerQueryText);")]
    [DataRow("AstProjector.Dispatch.cs", "\"Musoq.Parser.Nodes.AddNode\"", "\"Musoq.Parser.Nodes.WrongNode\"")]
    public void RemovedOrMiswiredProjection_ShouldFailTheCompilerAudit(string file, string original, string replacement)
    {
        var mutated = CompilerMutation.Replace(VerificationCompiler.Current.Tests, file, original, replacement);

        Assert.ThrowsExactly<InvalidOperationException>(() => ProjectionAudit.Verify(mutated, CoverageManifest.SourceTypes));
    }

    [TestMethod]
    [DataRow("NewNode")]
    [DataRow("NewSchemaHelper")]
    public void NewParserDeclaration_ShouldChangeTheReviewedInventory(string name)
    {
        var parser = VerificationCompiler.Current.Parser.AddSyntaxTrees(CSharpSyntaxTree.ParseText(
            "namespace Musoq.Parser.Nodes; public sealed class " + name + " {}"));
        var additions = VerificationCompiler.ParserDeclarations(parser).Except(CoverageManifest.Inventory).ToArray();

        CollectionAssert.AreEqual(new[] { "Musoq.Parser.Nodes." + name }, additions);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("public Musoq.Parser.TextSpan Span { get; }")]
    public void ReviewedProjectionAndSourceRanges_ShouldPassTheAudit(string declaration)
    {
        var compilation = CompilerMutation.Fixture(ProjectionFixture.Create(declaration));

        ProjectionAudit.Verify(compilation, new[] { "Musoq.Parser.Nodes.FixtureNode" });
    }

    [TestMethod]
    public void ReflectionMethodGroup_ShouldBeRejectedSemantically()
    {
        var compilation = CompilerMutation.Fixture("class Mutation { System.Func<string,System.Type> Factory = System.Type.GetType; }");

        Assert.IsNotEmpty(ReflectionAudit.Violations(compilation));
    }

    [TestMethod]
    [DataRow("public string SemanticValue { get; }")]
    [DataRow("public string SpanValue { get; }")]
    [DataRow("public string Span { get; }")]
    [DataRow("public bool HasSpan { get; }")]
    [DataRow("public object UnknownScalar { get; }")]
    [DataRow("public string this[int index] => \"value\";")]
    public void UnprojectedSemanticMember_ShouldFailEvenWithSourceLikeNames(string declaration)
    {
        var compilation = CompilerMutation.Fixture(ProjectionFixture.Create(declaration));

        Assert.ThrowsExactly<InvalidOperationException>(() => ProjectionAudit.Verify(compilation,
            new[] { "Musoq.Parser.Nodes.FixtureNode" }));
    }

    [TestMethod]
    [DataRow("System.Type.GetType(\"Example\")")]
    [DataRow("typeof(string).GetMembers()")]
    [DataRow("new object().GetType().GetProperties()")]
    [DataRow("System.Reflection.Assembly.Load(\"Example\")")]
    [DataRow("System.Activator.CreateInstance(typeof(object))")]
    [DataRow("((dynamic)new object()).Hidden()")]
    [DataRow("((dynamic)new object())[0]")]
    [DataRow("typeof(string).FullName")]
    [DataRow("((System.Delegate)null).DynamicInvoke()")]
    [DataRow("System.Enum.GetValues<System.DayOfWeek>()")]
    public void ReflectionOrDynamicMutation_ShouldBeRejectedSemantically(string expression)
    {
        var compilation = CompilerMutation.Fixture("class Mutation { object Run() => " + expression + "; }");

        Assert.IsNotEmpty(ReflectionAudit.Violations(compilation));
    }

    [TestMethod]
    public void OrdinaryTypeEquality_ShouldBePermitted()
    {
        var compilation = CompilerMutation.Fixture("class Allowed { bool Run(object value) => value.GetType() == typeof(string); }");

        CollectionAssert.AreEqual(Array.Empty<string>(), ReflectionAudit.Violations(compilation).ToArray());
    }

    [TestMethod]
    [DataRow("Musoq.Evaluator")]
    [DataRow("Musoq.Converter")]
    [DataRow("Microsoft.CodeAnalysis.CSharp")]
    public void ForbiddenPackageMutation_ShouldBeRejected(string package)
    {
        using var assets = JsonDocument.Parse("{\"libraries\":{\"" + package + "/1.0.0\":{}}}");

        Assert.ThrowsExactly<InvalidOperationException>(() => DependencyAudit.VerifyAssets(assets.RootElement,
            new[] { "Musoq.Parser" }.ToHashSet(StringComparer.Ordinal), "Formatter"));
    }
}
