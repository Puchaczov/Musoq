using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void EnumQueryScopedSample_WhenCheckedIn_ShouldUsePrimitiveHotPaths()
    {
        var sample = ReadSample("Q324_EnumQueryScopedRows.cs").Content;
        var generated = ExtractGeneratedCodeSection(sample);

        Assert.Contains("reader.Read<short?>(", generated);
        Assert.Contains("reader.Read<uint?>(", generated);
        Assert.Contains("new global::Musoq.Schema.EnumTypeDescriptor", generated);
        Assert.Contains("& 3u", generated);
        Assert.Contains("20 => \"Running\"", generated);

        string[] forbiddenRuntimeApis =
        [
            "Enum.Parse",
            "Enum.ToObject",
            "Convert.ChangeType",
            "System.Reflection",
            "DynamicInvoke",
            "reader.Read<object>"
        ];
        foreach (var forbidden in forbiddenRuntimeApis)
            Assert.IsFalse(generated.Contains(forbidden, StringComparison.Ordinal), forbidden);

        var root = CSharpSyntaxTree.ParseText(generated).GetRoot();
        var descriptorsInLoops = root.DescendantNodes()
            .OfType<ObjectCreationExpressionSyntax>()
            .Where(static creation => creation.Type.ToString().Contains(
                "EnumTypeDescriptor",
                StringComparison.Ordinal))
            .Where(static creation => creation.Ancestors().Any(static ancestor =>
                ancestor is ForStatementSyntax or ForEachStatementSyntax or
                    WhileStatementSyntax or DoStatementSyntax))
            .Select(static creation => creation.ToString())
            .ToArray();

        Assert.IsEmpty(
            descriptorsInLoops,
            "Enum descriptors must remain outside generated row loops.");
    }
}
