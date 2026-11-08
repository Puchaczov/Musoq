using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class ErrorQualityBinaryTextSchemaTests
{
    #region P-TEXT: Unknown extraction methods

    [TestMethod]
    public void P_TEXT_05_UnknownExtractionMethod()
    {
        // Arrange — 'gobble' is not a valid extraction method
        var analyzer = CreateAnalyzer();
        var query = @"text LogLine {
    Data: gobble ' '
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — 'gobble' is not a known extraction method
        AssertParseOrSemanticFailure(result,
            "Unknown extraction method 'gobble' should produce error");
    }

    #endregion

    #region P-TEXT: Missing delimiter for 'until'

    [TestMethod]
    public void P_TEXT_06_UntilMissingDelimiter()
    {
        // Arrange — 'until' without delimiter string
        var analyzer = CreateAnalyzer();
        var query = @"text LogLine {
    Timestamp: until
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert
        AssertParseOrSemanticFailure(result,
            "until without delimiter should produce parse error");
    }

    #endregion

    #region P-TEXT: Empty text schema

    [TestMethod]
    public void P_TEXT_11_EmptyTextSchema()
    {
        // Arrange — text schema with no fields
        var analyzer = CreateAnalyzer();
        var query = @"text EmptyFormat {
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Empty text schemas are valid in Musoq, similar to binary schemas.
        // They serve as base schemas in the extends pattern and as placeholder definitions.
        AssertNoErrors(result);
    }

    #endregion

    #region P-TEXT: Missing schema name

    [TestMethod]
    public void P_TEXT_12_MissingSchemaName()
    {
        // Arrange — text keyword without name
        var analyzer = CreateAnalyzer();
        var query = @"text {
    Data: rest
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert
        AssertParseOrSemanticFailure(result,
            "Text schema without name should produce parse error");
    }

    #endregion

    #region P-TEXT: Duplicate field names

    [TestMethod]
    public void P_TEXT_13_DuplicateFieldNames()
    {
        // Arrange — two fields with same name
        var analyzer = CreateAnalyzer();
        var query = @"text LogLine {
    Data: until ' ',
    Data: rest
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        var diagnostic = DiagnosticContractTestAssertions.AssertSingleError(
            result,
            DiagnosticCode.MQ4008_DuplicateSchemaField,
            "duplicate text field name");
        Assert.AreEqual(new Musoq.Parser.TextSpan(query.LastIndexOf("Data", StringComparison.Ordinal), 4),
            diagnostic.Span);
    }

    #endregion

    #region P-TEXT: Invalid trim modifier

    [TestMethod]
    public void P_TEXT_14_InvalidModifier()
    {
        // Arrange — 'rest' with unknown modifier
        var analyzer = CreateAnalyzer();
        var query = @"text LogLine {
    Data: rest squeeze
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert
        AssertParseOrSemanticFailure(result,
            "Unknown modifier 'squeeze' should produce error");
    }

    #endregion

    #region P-TEXT: Literal missing string value

    [TestMethod]
    public void P_TEXT_15_LiteralMissingString()
    {
        // Arrange — 'literal' without the expected text
        var analyzer = CreateAnalyzer();
        var query = @"text LogLine {
    Marker: literal
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert
        AssertParseOrSemanticFailure(result,
            "literal without string value should produce parse error");
    }

    #endregion

    #region P-TEXT: Pattern with invalid regex

    [TestMethod]
    public void P_TEXT_16_PatternWithInvalidRegex()
    {
        // Arrange — pattern with unclosed bracket
        var analyzer = CreateAnalyzer();
        var query = @"text LogLine {
    Data: pattern '[unclosed'
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        var diagnostic = DiagnosticContractTestAssertions.AssertSingleError(
            result,
            DiagnosticCode.MQ4002_InvalidTextSchemaField,
            "invalid text schema regex");
        const string patternLiteral = "'[unclosed'";
        Assert.AreEqual(
            new Musoq.Parser.TextSpan(query.IndexOf(patternLiteral, StringComparison.Ordinal), patternLiteral.Length),
            diagnostic.Span);
    }

    #endregion
}
