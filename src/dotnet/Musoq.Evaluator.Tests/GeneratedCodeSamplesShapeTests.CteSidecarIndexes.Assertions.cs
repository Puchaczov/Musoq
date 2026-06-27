using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    private static void AssertGeneratedCodeUsesTypedCteIndexResults(string generatedCSharpCode)
    {
        Assert.Contains("var _cteIndexResults = new CteIndexResults();", generatedCSharpCode);
        Assert.Contains("private sealed class CteIndexResults", generatedCSharpCode);
        Assert.IsFalse(generatedCSharpCode.Contains("private readonly CteIndexResults _cteIndexResults = new CteIndexResults();", StringComparison.Ordinal), generatedCSharpCode);
        Assert.IsFalse(generatedCSharpCode.Contains("_cteIndexResults = new object[", StringComparison.Ordinal), generatedCSharpCode);
        Assert.IsFalse(generatedCSharpCode.Contains("object[] _cteIndexResults", StringComparison.Ordinal), generatedCSharpCode);
    }

    private static void AssertGeneratedCodeUsesTypedCteRowResults(string generatedCSharpCode)
    {
        Assert.Contains("var _cteRowResults = new CteRowResults();", generatedCSharpCode);
        Assert.Contains("private sealed class CteRowResults", generatedCSharpCode);
        Assert.IsFalse(generatedCSharpCode.Contains("private readonly CteRowResults _cteRowResults = new CteRowResults();", StringComparison.Ordinal), generatedCSharpCode);
        Assert.IsFalse(generatedCSharpCode.Contains("Musoq.Evaluator.Tables.Table BuildCte", StringComparison.Ordinal), generatedCSharpCode);
        Assert.IsFalse(generatedCSharpCode.Contains("Musoq.Evaluator.Tables.Table[] _tableResults", StringComparison.Ordinal), generatedCSharpCode);
        Assert.IsFalse(generatedCSharpCode.Contains("_tableResults[", StringComparison.Ordinal), generatedCSharpCode);
        Assert.IsFalse(generatedCSharpCode.Contains("EvaluationHelper.CastGeneratedRows<", StringComparison.Ordinal), generatedCSharpCode);
    }

    private static void AssertGeneratedCodeDoesNotUseCteRowResults(string generatedCSharpCode)
    {
        Assert.IsFalse(generatedCSharpCode.Contains("private readonly CteRowResults _cteRowResults", StringComparison.Ordinal), generatedCSharpCode);
        Assert.IsFalse(generatedCSharpCode.Contains("private sealed class CteRowResults", StringComparison.Ordinal), generatedCSharpCode);
        Assert.IsFalse(generatedCSharpCode.Contains("_cteRowResults.", StringComparison.Ordinal), generatedCSharpCode);
        Assert.IsFalse(generatedCSharpCode.Contains("Musoq.Evaluator.Tables.Table BuildCte", StringComparison.Ordinal), generatedCSharpCode);
        Assert.IsFalse(generatedCSharpCode.Contains("Musoq.Evaluator.Tables.Table[] _tableResults", StringComparison.Ordinal), generatedCSharpCode);
        Assert.IsFalse(generatedCSharpCode.Contains("_tableResults[", StringComparison.Ordinal), generatedCSharpCode);
        Assert.IsFalse(generatedCSharpCode.Contains("EvaluationHelper.CastGeneratedRows<", StringComparison.Ordinal), generatedCSharpCode);
    }

    private static void AssertSampleUsesTypedCteIndexResults(string content)
    {
        Assert.Contains("var _cteIndexResults = new CteIndexResults();", content);
        Assert.Contains("private sealed class CteIndexResults", content);
        Assert.IsFalse(content.Contains("private readonly CteIndexResults _cteIndexResults = new CteIndexResults();", StringComparison.Ordinal), content);
        Assert.IsFalse(content.Contains("_cteIndexResults = new object[", StringComparison.Ordinal), content);
        Assert.IsFalse(content.Contains("object[] _cteIndexResults", StringComparison.Ordinal), content);
        Assert.IsFalse(content.Contains(")_cteIndexResults[", StringComparison.Ordinal), content);
    }

    private static void AssertSampleUsesTypedCteRowResults(string content)
    {
        var generatedCode = ExtractGeneratedCodeSection(content);

        Assert.Contains("var _cteRowResults = new CteRowResults();", generatedCode);
        Assert.Contains("private sealed class CteRowResults", generatedCode);
        Assert.IsFalse(generatedCode.Contains("private readonly CteRowResults _cteRowResults = new CteRowResults();", StringComparison.Ordinal), generatedCode);
        Assert.IsFalse(generatedCode.Contains("Musoq.Evaluator.Tables.Table BuildCte", StringComparison.Ordinal), generatedCode);
        Assert.IsFalse(generatedCode.Contains("Musoq.Evaluator.Tables.Table[] _tableResults", StringComparison.Ordinal), generatedCode);
        Assert.IsFalse(generatedCode.Contains("_tableResults[", StringComparison.Ordinal), generatedCode);
        Assert.IsFalse(generatedCode.Contains("EvaluationHelper.CastGeneratedRows<", StringComparison.Ordinal), generatedCode);
    }

    private static void AssertSampleDoesNotUseCteRowResults(string content)
    {
        AssertGeneratedCodeDoesNotUseCteRowResults(ExtractGeneratedCodeSection(content));
    }

    private static void AssertTextBefore(string expectedEarlier, string expectedLater, string text)
    {
        var earlier = text.IndexOf(expectedEarlier, StringComparison.Ordinal);
        var later = text.IndexOf(expectedLater, StringComparison.Ordinal);

        Assert.IsTrue(earlier >= 0, text);
        Assert.IsTrue(later >= 0, text);
        Assert.IsTrue(earlier < later, text);
    }
}
