using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class ErrorQualityBinaryTextSchemaTests
{
    #region P-BIN: Missing braces, colons, semicolons

    [TestMethod]
    public void P_BIN_01_MissingOpenBrace()
    {
        // Arrange — binary schema without opening brace
        var analyzer = CreateAnalyzer();
        var query = @"binary HeaderFormat
    Magic: int le
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should report parse error for missing brace
        AssertParseOrSemanticFailure(result,
            "Missing opening brace should produce parse error");
    }

    [TestMethod]
    public void P_BIN_02_MissingCloseBrace()
    {
        // Arrange — binary schema without closing brace
        var analyzer = CreateAnalyzer();
        var query = @"binary HeaderFormat {
    Magic: int le
;
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should report parse error for missing brace
        AssertParseOrSemanticFailure(result,
            "Missing closing brace should produce parse error");
    }

    [TestMethod]
    public void P_BIN_03_MissingSemicolonAfterSchema()
    {
        // Arrange — binary schema without trailing semicolon
        var analyzer = CreateAnalyzer();
        var query = @"binary HeaderFormat {
    Magic: int le
}
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Semicolons are optional statement terminators in Musoq
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_BIN_04_MissingColonAfterFieldName()
    {
        // Arrange — field without colon separator
        var analyzer = CreateAnalyzer();
        var query = @"binary HeaderFormat {
    Magic int le
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should report parse error
        AssertParseOrSemanticFailure(result,
            "Missing colon after field name should produce parse error");
    }

    #endregion

    #region P-BIN: Missing or invalid endianness

    [TestMethod]
    public void P_BIN_05_MissingEndianness()
    {
        // Arrange — int field without le/be
        var analyzer = CreateAnalyzer();
        var query = @"binary HeaderFormat {
    Magic: int
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — int requires endianness: should error or misparse
        AssertParseOrSemanticFailure(result,
            "int without endianness should report error");
    }

    [TestMethod]
    public void P_BIN_06_InvalidEndiannessKeyword()
    {
        // Arrange — invalid endianness identifier
        var analyzer = CreateAnalyzer();
        var query = @"binary HeaderFormat {
    Magic: int middle
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — 'middle' is not a valid endianness
        AssertParseOrSemanticFailure(result,
            "Invalid endianness 'middle' should produce error");
    }

    #endregion

    #region P-BIN: Unknown field types

    [TestMethod]
    public void P_BIN_07_UnknownPrimitiveType()
    {
        // Arrange — field with nonsense type
        var analyzer = CreateAnalyzer();
        var query = @"binary HeaderFormat {
    Magic: complex128 le
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — 'complex128' is not a known binary type
        AssertParseOrSemanticFailure(result,
            "Unknown binary type 'complex128' should produce error");
    }

    [TestMethod]
    public void P_BIN_08_ByteWithEndianness()
    {
        // Arrange — byte doesn't need endianness
        var analyzer = CreateAnalyzer();
        var query = @"binary HeaderFormat {
    Flag: byte le
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — byte with endianness is actually rejected by the parser.
        // Even though byte is single-byte, specifying endianness produces an error.
        AssertParseOrSemanticFailure(result,
            "byte with endianness produces a parse error");
    }

    #endregion

    #region P-BIN: Invalid array syntax

    [TestMethod]
    public void P_BIN_10_ByteArrayMissingSize()
    {
        // Arrange — byte array without size
        var analyzer = CreateAnalyzer();
        var query = @"binary HeaderFormat {
    Data: byte[]
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — byte[] needs size
        AssertParseOrSemanticFailure(result,
            "byte[] without size should produce parse error");
    }

    [TestMethod]
    public void P_BIN_11_ByteArrayNegativeSize()
    {
        // Arrange — byte array with negative size
        var analyzer = CreateAnalyzer();
        var query = @"binary HeaderFormat {
    Data: byte[-5]
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — negative size
        AssertParseOrSemanticFailure(result,
            "byte[-5] should produce parse error");
    }

    [TestMethod]
    public void P_BIN_12_ByteArrayZeroSize()
    {
        // Arrange — byte array with zero size
        var analyzer = CreateAnalyzer();
        var query = @"binary HeaderFormat {
    Data: byte[0]
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — byte[0] is accepted by the parser without error.
        // Zero-size arrays are a valid construct in Musoq's schema definitions.
        AssertNoErrors(result);
    }

    #endregion

    #region P-BIN: String field issues

    [TestMethod]
    public void P_BIN_13_StringFieldMissingEncoding()
    {
        // Arrange — string field without encoding
        var analyzer = CreateAnalyzer();
        var query = @"binary HeaderFormat {
    Name: string[20]
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — string fields should require encoding for clarity.
        // Without encoding, the parser may either default or error.
        AssertParseOrSemanticFailure(result,
            "string[20] without encoding should require explicit encoding");
    }

    [TestMethod]
    public void P_BIN_14_StringFieldInvalidEncoding()
    {
        // Arrange — string field with unknown encoding
        var analyzer = CreateAnalyzer();
        var query = @"binary HeaderFormat {
    Name: string[20] klingon
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — 'klingon' is not a valid encoding
        AssertParseOrSemanticFailure(result,
            "Invalid encoding 'klingon' should produce error");
    }

    #endregion

    // ============================================================================
    // P-TEXT: Text Schema Parse-Level Errors
    // ============================================================================

    #region P-TEXT: Missing braces, colons, semicolons

    [TestMethod]
    public void P_TEXT_01_MissingOpenBrace()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = @"text LogLine
    Timestamp: until ' '
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert
        AssertParseOrSemanticFailure(result,
            "Missing opening brace should produce parse error");
    }

    [TestMethod]
    public void P_TEXT_02_MissingCloseBrace()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = @"text LogLine {
    Timestamp: until ' '
;
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert
        AssertParseOrSemanticFailure(result,
            "Missing closing brace should produce parse error");
    }

    [TestMethod]
    public void P_TEXT_03_MissingSemicolonAfterSchema()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = @"text LogLine {
    Timestamp: until ' '
}
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Semicolons are optional statement terminators in Musoq
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_TEXT_04_MissingColonAfterFieldName()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var query = @"text LogLine {
    Timestamp until ' '
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert
        AssertParseOrSemanticFailure(result,
            "Missing colon after field name should produce parse error");
    }

    #endregion

    #region P-TEXT: Missing delimiters for 'between'

    [TestMethod]
    public void P_TEXT_07_BetweenMissingEndDelimiter()
    {
        // Arrange — 'between' with only one delimiter
        var analyzer = CreateAnalyzer();
        var query = @"text LogLine {
    Data: between '['
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert
        AssertParseOrSemanticFailure(result,
            "between with missing end delimiter should produce parse error");
    }

    [TestMethod]
    public void P_TEXT_08_BetweenMissingBothDelimiters()
    {
        // Arrange — 'between' without any delimiters
        var analyzer = CreateAnalyzer();
        var query = @"text LogLine {
    Data: between
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert
        AssertParseOrSemanticFailure(result,
            "between without delimiters should produce parse error");
    }

    #endregion

    #region P-TEXT: chars field missing size

    [TestMethod]
    public void P_TEXT_09_CharsMissingSize()
    {
        // Arrange — 'chars' without bracket size
        var analyzer = CreateAnalyzer();
        var query = @"text LogLine {
    Code: chars
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert
        AssertParseOrSemanticFailure(result,
            "chars without size should produce parse error");
    }

    [TestMethod]
    public void P_TEXT_10_CharsNegativeSize()
    {
        // Arrange — 'chars[-5]'
        var analyzer = CreateAnalyzer();
        var query = @"text LogLine {
    Code: chars[-5]
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert
        AssertParseOrSemanticFailure(result,
            "chars[-5] should produce parse error");
    }

    #endregion
}
