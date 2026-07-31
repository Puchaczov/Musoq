using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    private const string OrdinaryCteColumnListSampleFileName = "Q187_CteColumnListOrdinary.cs";

    private static readonly string[] RecursiveUnionAllSampleFileNames =
    [
        "Q188_RecursiveUnionAllCounter.cs",
        "Q189_RecursiveUnionAllPredicateTermination.cs",
        "Q190_RecursiveEmptyAnchor.cs",
        "Q191_RecursiveMultipleRoots.cs"
    ];

    private static readonly string[] RecursiveIdentitySampleFileNames =
    [
        "Q192_RecursiveUnionFullRowCycle.cs",
        "Q193_RecursiveUnionSingleKeyCycle.cs",
        "Q194_RecursiveUnionCompositeKey.cs",
        "Q195_RecursiveKeyedNonKeyPayload.cs",
        "Q196_RecursiveAnchorDuplicates.cs",
        "Q197_RecursiveDuplicateEdges.cs"
    ];

    public static IEnumerable<object[]> RecursiveUnionAllSampleData =>
        RecursiveUnionAllSampleFileNames.Select(static fileName => new object[] { fileName });

    public static IEnumerable<object[]> RecursiveIdentitySampleData =>
        RecursiveIdentitySampleFileNames.Select(static fileName => new object[] { fileName });

    [TestMethod]
    public void OrdinaryCteColumnListSample_ShouldUseExportedNamesAcrossEveryPlanLayer()
    {
        var sample = ReadSample(OrdinaryCteColumnListSampleFileName);
        var logical = ReadGeneratedSampleSection(sample.Content, "Logical Plan", "Physical Plan");
        var physical = ReadGeneratedSampleSection(sample.Content, "Physical Plan", "Execution Plan");
        var execution = ReadExecutionPlan(OrdinaryCteColumnListSampleFileName);
        var code = ReadGeneratedCode(OrdinaryCteColumnListSampleFileName);

        Assert.Contains("Project [ko3iko.City as Name, ko3iko.Country as Nation]", logical);
        Assert.Contains("PhysicalProject [ko3iko.City as Name, ko3iko.Country as Nation]", physical);
        Assert.Contains("Name: string <- field Name", execution);
        Assert.Contains("Nation: string <- field Nation", execution);
        Assert.Contains("new Column(\"Name\", typeof(string), 0)", code);
        Assert.Contains("new Column(\"Nation\", typeof(string), 1)", code);
    }

    [TestMethod]
    [DynamicData(nameof(RecursiveUnionAllSampleData))]
    public void RecursiveUnionAllSample_ShouldUseReusableTypedFrontiersAndDirectEmission(string fileName)
    {
        var sample = ReadSample(fileName);
        var logical = ReadGeneratedSampleSection(sample.Content, "Logical Plan", "Physical Plan");
        var physical = ReadGeneratedSampleSection(sample.Content, "Physical Plan", "Execution Plan");
        var execution = ReadExecutionPlan(fileName);
        var code = ReadGeneratedCode(fileName);
        var root = CSharpSyntaxTree.ParseText(code).GetRoot();
        var fixedPointLoops = root.DescendantNodes().OfType<WhileStatementSyntax>()
            .Where(static loop => loop.Condition.ToString().Contains("CurrentFrontier.Count > 0", StringComparison.Ordinal))
            .ToArray();
        var collectionCreations = root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>()
            .Where(static creation => IsRecursiveCollectionType(creation.Type.ToString()))
            .ToArray();

        Assert.Contains("RecursiveCte [", logical);
        Assert.Contains("PhysicalRecursiveCte [", physical);
        Assert.Contains("RecursiveCte [", execution);
        Assert.Contains("RecursiveAppend [", execution);
        Assert.Contains("private readonly struct Cte0Row0", code);
        Assert.AreEqual(3, collectionCreations.Count(static creation => creation.Type.ToString() == "List<Cte0Row0>"));
        Assert.HasCount(1, fixedPointLoops);
        Assert.IsFalse(fixedPointLoops[0].DescendantNodes().OfType<ObjectCreationExpressionSyntax>()
            .Any(static creation => IsRecursiveCollectionType(creation.Type.ToString())), fileName);
        Assert.Contains("cte0NextFrontier.Clear();", code);
        Assert.Contains("cte0CurrentFrontier = cte0NextFrontier;", code);
        Assert.Contains("cte0NextFrontier = __cte0FrontierSwap;", code);
        Assert.IsFalse(collectionCreations.Any(static creation =>
            creation.Type.ToString().StartsWith("HashSet<", StringComparison.Ordinal)), fileName);
        Assert.IsFalse(root.DescendantNodes().OfType<ArrayCreationExpressionSyntax>()
            .Any(static creation => creation.Type.ElementType.ToString() == "object"), fileName);
        Assert.IsFalse(ContainsInvocation(root, "Select"), fileName);
        Assert.IsFalse(ContainsInvocation(root, "Where"), fileName);
        Assert.IsFalse(execution.Contains("CreateTable [cte0CurrentFrontier", StringComparison.Ordinal), fileName);
        Assert.IsFalse(execution.Contains("CreateTable [cte0NextFrontier", StringComparison.Ordinal), fileName);
    }

    [TestMethod]
    [DynamicData(nameof(RecursiveIdentitySampleData))]
    public void RecursiveIdentitySample_ShouldUseOneTypedGlobalSeenSet(string fileName)
    {
        var sample = ReadSample(fileName);
        var execution = ReadExecutionPlan(fileName);
        var code = ReadGeneratedCode(fileName);
        var root = CSharpSyntaxTree.ParseText(code).GetRoot();
        var seenSets = root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>()
            .Where(static creation => creation.Type.ToString().StartsWith("HashSet<", StringComparison.Ordinal))
            .ToArray();
        var seenDeclarationIndex = code.IndexOf("var cte0Seen = new HashSet<", StringComparison.Ordinal);
        var loopIndex = code.IndexOf("while (cte0CurrentFrontier.Count > 0)", StringComparison.Ordinal);

        Assert.IsGreaterThanOrEqualTo(0, seenDeclarationIndex, fileName);
        Assert.IsGreaterThan(seenDeclarationIndex, loopIndex, fileName);
        Assert.HasCount(1, seenSets, fileName);
        Assert.AreEqual(2, CountText(code, "if (cte0Seen.Add("), fileName);
        Assert.Contains("private readonly struct Cte0Row0", code);
        Assert.Contains("identity", execution);
        Assert.IsFalse(seenSets[0].Type.ToString().Contains("object", StringComparison.Ordinal), fileName);
        Assert.IsFalse(seenSets[0].Type.ToString().Contains("Row", StringComparison.Ordinal), fileName);
    }

    [TestMethod]
    public void RecursiveCompositeIdentitySample_ShouldUseTypedValueTupleKey()
    {
        var sample = ReadSample("Q194_RecursiveUnionCompositeKey.cs");

        var code = ReadGeneratedCode(sample.FileName);
        Assert.Contains("new HashSet<ValueTuple<int, string>>()", code);
        Assert.Contains("cte0Seen.Add((__cte0CurrentFrontierCandidate0, __cte0CurrentFrontierCandidate1))", code);
        Assert.Contains("cte0Seen.Add((__cte0NextFrontierCandidate0, __cte0NextFrontierCandidate1))", code);
    }

    private static bool IsRecursiveCollectionType(string typeName) =>
        typeName.StartsWith("List<", StringComparison.Ordinal) ||
        typeName.StartsWith("HashSet<", StringComparison.Ordinal) ||
        typeName.StartsWith("Dictionary<", StringComparison.Ordinal);

    private static bool ContainsInvocation(Microsoft.CodeAnalysis.SyntaxNode root, string methodName) =>
        root.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Select(static invocation => invocation.Expression)
            .OfType<MemberAccessExpressionSyntax>()
            .Any(member => member.Name.Identifier.ValueText == methodName);

    private static int CountText(string text, string value)
    {
        return text.Split(value, StringSplitOptions.None).Length - 1;
    }
}
