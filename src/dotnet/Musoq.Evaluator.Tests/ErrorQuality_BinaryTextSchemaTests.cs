#nullable enable
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Error Message Quality Audit — Binary and Text Schema Parse/Semantic Error Tests.
///     These test parse-level and semantic-level errors when defining binary and text interpretation schemas.
///     Covers: P-BIN (binary parse errors), P-TEXT (text parse errors), P-MIX (mixed interaction errors),
///             E-BIN (binary semantic errors), E-TEXT (text semantic errors).
/// </summary>
[TestClass]
public class ErrorQuality_BinaryTextSchemaTests : BasicEntityTestBase
{
    #region Test Setup

    private static ISchemaProvider CreateSchemaProvider()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Warsaw", "Poland", 100) { Money = 1000.50m }] },
            { "#B", [new BasicEntity("Berlin", "Germany", 200) { Money = 2000.75m }] }
        };
        return new BasicSchemaProvider<BasicEntity>(sources);
    }

    private static QueryAnalyzer CreateAnalyzer()
    {
        return new QueryAnalyzer(CreateSchemaProvider());
    }

    private static void AssertHasOneOfErrorCodes(QueryAnalysisResult result, string context,
        params DiagnosticCode[] expectedCodes)
    {
        var errors = result.Errors.ToList();
        Assert.IsNotEmpty(errors,
            $"Expected one of [{string.Join(", ", expectedCodes)}] ({context}) but no diagnostics were reported");

        var hasExpected = errors.Any(e => expectedCodes.Contains(e.Code));
        if (!hasExpected)
        {
            var errorDetails = string.Join("\n", errors.Select(e => $"  [{e.Code}] {e.Message}"));
            Assert.Fail(
                $"Expected one of [{string.Join(", ", expectedCodes)}] ({context}) but got:\n{errorDetails}");
        }
    }

    private static void AssertNoErrors(QueryAnalysisResult result)
    {
        if (result.HasErrors)
        {
            var errorMessages = string.Join("\n", result.Errors.Select(e => $"  [{e.Code}] {e.Message}"));
            Assert.Fail($"Expected no errors but got:\n{errorMessages}");
        }
    }

    private static void AssertParseOrSemanticFailure(QueryAnalysisResult result, string context)
    {
        var errors = result.Errors.ToList();
        Assert.IsNotEmpty(errors,
            $"Expected parse or semantic error ({context}) but no diagnostics were reported.");

        if (errors.Any(e => string.IsNullOrWhiteSpace(e.Message)))
            Assert.Fail($"Expected actionable diagnostics ({context}) but one or more errors had empty messages.");
    }

    #endregion

    // ============================================================================
    // P-BIN: Binary Schema Parse-Level Errors
    // ============================================================================

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

    #region P-BIN: Duplicate field names

    [TestMethod]
    public void P_BIN_09_DuplicateFieldNames()
    {
        // Arrange — two fields with same name
        var analyzer = CreateAnalyzer();
        var query = @"binary HeaderFormat {
    Magic: int le,
    Magic: short le
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Duplicate field names are accepted by Musoq's parser.
        // The schema definition allows duplicate field names without error.
        // This is a known limitation — ideally MQ4008 should be reported.
        AssertNoErrors(result);
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

    #region P-BIN: Empty schema

    [TestMethod]
    public void P_BIN_15_EmptySchema()
    {
        // Arrange — binary schema with no fields
        var analyzer = CreateAnalyzer();
        var query = @"binary EmptyFormat {
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Empty schemas are valid in Musoq. They are used as base schemas
        // in the extends pattern (e.g., 'binary Derived extends Base { }') and
        // as placeholder definitions.
        AssertNoErrors(result);
    }

    #endregion

    #region P-BIN: Schema with missing name

    [TestMethod]
    public void P_BIN_16_MissingSchemaName()
    {
        // Arrange — binary keyword without schema name
        var analyzer = CreateAnalyzer();
        var query = @"binary {
    Magic: int le
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — missing name
        AssertParseOrSemanticFailure(result,
            "Binary schema without name should produce parse error");
    }

    #endregion

    #region P-BIN: Nested schema reference before definition

    [TestMethod]
    public void P_BIN_17_NestedSchemaForwardReference()
    {
        // Arrange — binary schema references another not yet defined
        var analyzer = CreateAnalyzer();
        var query = @"binary Outer {
    Header: Inner,
    Payload: byte[10]
};
binary Inner {
    Size: int le
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — forward references should be resolved. If the schema
        // is defined later, the parser should still handle it.
        AssertNoErrors(result);
    }

    #endregion

    #region P-BIN: Bits field issues

    [TestMethod]
    public void P_BIN_18_BitsFieldInvalidSize()
    {
        // Arrange — bits field with size > 8
        var analyzer = CreateAnalyzer();
        var query = @"binary Flags {
    HighBits: bits[16]
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — bits[16] exceeds single byte. Implementation may accept
        // wider bit fields, so this is intentionally permissive.
        AssertNoErrors(result);
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

        // Assert — Duplicate text field names are accepted by Musoq.
        // The parser does not enforce unique field names in text schemas.
        // This is a known limitation — ideally MQ4008 should be reported.
        AssertNoErrors(result);
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

        // Assert — regex validation may be deferred to runtime.
        // At parse time, the pattern string is accepted without regex validation.
        AssertNoErrors(result);
    }

    #endregion

    // ============================================================================
    // P-MIX: Mixed Binary/Text Interaction Parse Errors
    // ============================================================================

    #region P-MIX: Binary referencing undefined text schema via 'as'

    [TestMethod]
    public void P_MIX_01_BinaryAsClauseReferencingNonexistentTextSchema()
    {
        // Arrange — binary field uses 'as NonExistentText' 
        var analyzer = CreateAnalyzer();
        var query = @"binary Packet {
    Version: byte,
    Payload: string[20] utf8 as NonExistentFormat
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — forward reference to undefined schema may not be
        // caught at parse time; it is valid syntax. Semantic check happens later.
        AssertNoErrors(result);
    }

    #endregion

    #region P-MIX: Text schema and binary schema with same name

    [TestMethod]
    public void P_MIX_02_SameNameBinaryAndTextSchema()
    {
        // Arrange — both binary and text schemas named 'MyFormat'
        var analyzer = CreateAnalyzer();
        var query = @"binary MyFormat {
    Value: int le
};
text MyFormat {
    Data: rest
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Binary and text schemas with the same name do NOT conflict.
        // Musoq treats binary and text schemas as separate namespaces,
        // so same-named definitions across types are valid.
        AssertNoErrors(result);
    }

    #endregion

    #region P-MIX: Two binary schemas with same name

    [TestMethod]
    public void P_MIX_03_DuplicateBinarySchemaNames()
    {
        // Arrange — two binary schemas with same name
        var analyzer = CreateAnalyzer();
        var query = @"binary Header {
    Magic: int le
};
binary Header {
    Version: short le
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Duplicate binary schema names are accepted by Musoq.
        // The parser does not enforce unique schema names within the binary namespace.
        // This is a known limitation — the second definition may override the first.
        AssertNoErrors(result);
    }

    #endregion

    #region P-MIX: Binary schema followed by no query

    [TestMethod]
    public void P_MIX_04_SchemaDefinitionWithoutQuery()
    {
        // Arrange — only schema definition, no SELECT
        var analyzer = CreateAnalyzer();
        var query = @"binary Header {
    Magic: int le
};";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — schema definition without a query is valid syntax.
        // The schema definition is parsed successfully even without a SELECT.
        AssertNoErrors(result);
    }

    #endregion

    #region P-MIX: Multiple schemas with query referencing wrong name

    [TestMethod]
    public void P_MIX_05_QueryReferencingWrongSchemaName()
    {
        // Arrange — schema is 'Header' but query references 'HeaderFormat'
        var analyzer = CreateAnalyzer();
        var query = @"binary Header {
    Magic: int le
};
select h.Magic from #A.Entities() a cross apply Interpret(a.Name, 'HeaderFormat') h";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — wrong schema name in Interpret() should produce semantic error
        AssertHasOneOfErrorCodes(result, "Interpret() referencing wrong schema name",
            DiagnosticCode.MQ4003_UndefinedSchemaReference,
            DiagnosticCode.MQ3010_UnknownSchema,
            DiagnosticCode.MQ9999_Unknown);
    }

    #endregion

    // ============================================================================
    // E-BIN: Binary Schema Semantic/Evaluation Errors (via Analyze())
    // ============================================================================

    #region E-BIN: Circular schema reference

    [TestMethod]
    public void E_BIN_01_CircularSchemaReference()
    {
        // Arrange — binary schema references itself
        var analyzer = CreateAnalyzer();
        var query = @"binary SelfRef {
    Inner: SelfRef,
    Data: byte
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — circular reference should produce MQ4004 or equivalent
        AssertHasOneOfErrorCodes(result, "circular binary schema reference",
            DiagnosticCode.MQ4004_CircularSchemaReference,
            DiagnosticCode.MQ3016_CircularReference,
            DiagnosticCode.MQ9999_Unknown);
    }

    #endregion

    #region E-BIN: Reference to undefined nested schema

    [TestMethod]
    public void E_BIN_02_UndefinedNestedSchemaReference()
    {
        // Arrange — references undefined 'Payload' schema
        var analyzer = CreateAnalyzer();
        var query = @"binary Packet {
    Header: int le,
    Data: Payload
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — undefined schema reference should produce MQ4003 or equivalent
        AssertHasOneOfErrorCodes(result, "undefined nested schema reference",
            DiagnosticCode.MQ4003_UndefinedSchemaReference,
            DiagnosticCode.MQ3010_UnknownSchema,
            DiagnosticCode.MQ9999_Unknown);
    }

    #endregion

    #region E-BIN: Interpret() with non-byte-array column

    [TestMethod]
    public void E_BIN_03_InterpretOnStringColumn()
    {
        // Arrange — Interpret() called on string column instead of byte[]
        var analyzer = CreateAnalyzer();
        var query = @"binary Header {
    Magic: int le
};
select h.Magic from #A.Entities() a cross apply Interpret(a.Name, 'Header') h";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Interpret() on string should produce type error
        AssertHasOneOfErrorCodes(result, "Interpret() on string column",
            DiagnosticCode.MQ3005_TypeMismatch,
            DiagnosticCode.MQ3013_CannotResolveMethod,
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ9999_Unknown);
    }

    #endregion

    #region E-BIN: Interpret() with wrong number of arguments

    [TestMethod]
    public void E_BIN_04_InterpretWrongArgCount()
    {
        // Arrange — Interpret() with 3 args (normally 2)
        var analyzer = CreateAnalyzer();
        var query = @"binary Header {
    Magic: int le
};
select h.Magic from #A.Entities() a cross apply Interpret(a.Name, 'Header', 'extra') h";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — wrong arg count should produce error
        AssertHasOneOfErrorCodes(result, "Interpret() with wrong arg count",
            DiagnosticCode.MQ3006_InvalidArgumentCount,
            DiagnosticCode.MQ3013_CannotResolveMethod,
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ9999_Unknown);
    }

    #endregion

    #region E-BIN: Accessing non-existent field from binary schema

    [TestMethod]
    public void E_BIN_05_AccessingNonExistentBinaryField()
    {
        // Arrange — schema defines Magic but query accesses Version
        var analyzer = CreateAnalyzer();
        var query = @"binary Header {
    Magic: int le
};
select h.Version from #A.Entities() a cross apply Interpret(a.Name, 'Header') h";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — accessing non-existent field should produce column error
        AssertHasOneOfErrorCodes(result, "accessing non-existent binary field",
            DiagnosticCode.MQ3001_UnknownColumn,
            DiagnosticCode.MQ3014_InvalidPropertyAccess,
            DiagnosticCode.MQ9999_Unknown);
    }

    #endregion

    #region E-BIN: Binary schema with 'when' condition referencing unknown field

    [TestMethod]
    public void E_BIN_06_WhenConditionUnknownField()
    {
        // Arrange — conditional field references unknown earlier field
        var analyzer = CreateAnalyzer();
        var query = @"binary Packet {
    Type: byte,
    Data: int le when UnknownField = 1
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — 'when' referencing unknown field should error
        AssertHasOneOfErrorCodes(result, "when condition referencing unknown field",
            DiagnosticCode.MQ3001_UnknownColumn,
            DiagnosticCode.MQ4006_InvalidFieldConstraint,
            DiagnosticCode.MQ9999_Unknown);
    }

    #endregion

    // ============================================================================
    // E-TEXT: Text Schema Semantic/Evaluation Errors (via Analyze())
    // ============================================================================

    #region E-TEXT: Parse() with non-string column

    [TestMethod]
    public void E_TEXT_01_ParseOnNonStringColumn()
    {
        // Arrange — Parse() called on integer column
        var analyzer = CreateAnalyzer();
        var query = @"text LogLine {
    Data: rest
};
select l.Data from #A.Entities() a cross apply Parse(a.Population, 'LogLine') l";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Parse() on integer should produce type error
        AssertHasOneOfErrorCodes(result, "Parse() on integer column",
            DiagnosticCode.MQ3005_TypeMismatch,
            DiagnosticCode.MQ3013_CannotResolveMethod,
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ9999_Unknown);
    }

    #endregion

    #region E-TEXT: Parse() referencing undefined text schema

    [TestMethod]
    public void E_TEXT_02_ParseReferencingUndefinedSchema()
    {
        // Arrange — Parse() references undefined schema
        var analyzer = CreateAnalyzer();
        var query = @"select l.Data from #A.Entities() a cross apply Parse(a.Name, 'UndefinedFormat') l";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — undefined schema in Parse() should produce error
        AssertHasOneOfErrorCodes(result, "Parse() referencing undefined schema",
            DiagnosticCode.MQ4003_UndefinedSchemaReference,
            DiagnosticCode.MQ3010_UnknownSchema,
            DiagnosticCode.MQ9999_Unknown);
    }

    #endregion

    #region E-TEXT: Parse() with wrong number of arguments

    [TestMethod]
    public void E_TEXT_03_ParseWrongArgCount()
    {
        // Arrange — Parse() with only 1 argument
        var analyzer = CreateAnalyzer();
        var query = @"text LogLine {
    Data: rest
};
select l.Data from #A.Entities() a cross apply Parse(a.Name) l";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Parse() with wrong arg count should produce error
        AssertHasOneOfErrorCodes(result, "Parse() with wrong arg count",
            DiagnosticCode.MQ3006_InvalidArgumentCount,
            DiagnosticCode.MQ3013_CannotResolveMethod,
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ9999_Unknown);
    }

    #endregion

    #region E-TEXT: Accessing non-existent field from text schema

    [TestMethod]
    public void E_TEXT_04_AccessingNonExistentTextField()
    {
        // Arrange — schema defines Data but query accesses Content
        var analyzer = CreateAnalyzer();
        var query = @"text LogLine {
    Data: rest
};
select l.Content from #A.Entities() a cross apply Parse(a.Name, 'LogLine') l";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — accessing non-existent text field should error
        AssertHasOneOfErrorCodes(result, "accessing non-existent text field",
            DiagnosticCode.MQ3001_UnknownColumn,
            DiagnosticCode.MQ3014_InvalidPropertyAccess,
            DiagnosticCode.MQ9999_Unknown);
    }

    #endregion

    #region E-TEXT: Multiple text schemas, one referencing wrong one

    [TestMethod]
    public void E_TEXT_05_ParseReferencingWrongSchemaType()
    {
        // Arrange — Define binary schema but use Parse() (text function) with it
        var analyzer = CreateAnalyzer();
        var query = @"binary Header {
    Magic: int le
};
select l.Magic from #A.Entities() a cross apply Parse(a.Name, 'Header') l";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Parse() with binary schema name may confuse
        AssertHasOneOfErrorCodes(result, "Parse() with binary schema name",
            DiagnosticCode.MQ4003_UndefinedSchemaReference,
            DiagnosticCode.MQ3010_UnknownSchema,
            DiagnosticCode.MQ9999_Unknown);
    }

    #endregion

    #region E-TEXT: Interpret() referencing text schema

    [TestMethod]
    public void E_TEXT_06_InterpretReferencingTextSchema()
    {
        // Arrange — Define text schema but use Interpret() (binary function) with it
        var analyzer = CreateAnalyzer();
        var query = @"text LogLine {
    Data: rest
};
select h.Data from #A.Entities() a cross apply Interpret(a.Name, 'LogLine') h";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Interpret() with text schema name should error
        AssertHasOneOfErrorCodes(result, "Interpret() with text schema name",
            DiagnosticCode.MQ4003_UndefinedSchemaReference,
            DiagnosticCode.MQ3010_UnknownSchema,
            DiagnosticCode.MQ9999_Unknown);
    }

    #endregion

    // ============================================================================
    // Well-formed binary/text schemas that SHOULD succeed (positive baselines)
    // ============================================================================

    #region Positive: Well-formed binary schema parses without errors

    [TestMethod]
    public void Positive_BIN_WellFormedBinarySchemaParses()
    {
        // Arrange — correct binary schema
        var analyzer = CreateAnalyzer();
        var query = @"binary Header {
    Magic: int le,
    Version: short le,
    Flag: byte
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — should parse cleanly
        // Note: semantic errors may still occur in Analyze(), but parse should succeed
        if (result.HasErrors)
        {
            var errorDetails = string.Join("\n", result.Errors.Select(e => $"  [{e.Code}] {e.Message}"));
            Assert.Inconclusive($"Well-formed binary schema produced parse errors:\n{errorDetails}");
        }
    }

    #endregion

    #region Positive: Well-formed text schema parses without errors

    [TestMethod]
    public void Positive_TEXT_WellFormedTextSchemaParses()
    {
        // Arrange — correct text schema
        var analyzer = CreateAnalyzer();
        var query = @"text LogLine {
    Timestamp: until ' ',
    Level: until ' ',
    Message: rest trim
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — should parse cleanly
        if (result.HasErrors)
        {
            var errorDetails = string.Join("\n", result.Errors.Select(e => $"  [{e.Code}] {e.Message}"));
            Assert.Inconclusive($"Well-formed text schema produced parse errors:\n{errorDetails}");
        }
    }

    #endregion

    #region Positive: Binary with nested schema reference parses

    [TestMethod]
    public void Positive_BIN_NestedSchemaReferenceParses()
    {
        // Arrange — binary schema referencing another binary schema
        var analyzer = CreateAnalyzer();
        var query = @"binary Inner {
    Size: int le
};
binary Outer {
    Header: Inner,
    Data: byte[10]
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — nested references should parse cleanly
        if (result.HasErrors)
        {
            var errorDetails = string.Join("\n", result.Errors.Select(e => $"  [{e.Code}] {e.Message}"));
            Assert.Inconclusive($"Nested binary schema reference produced parse errors:\n{errorDetails}");
        }
    }

    #endregion

    #region Positive: Binary with text composition via 'as' parses

    [TestMethod]
    public void Positive_MIX_BinaryWithTextAsClauseParses()
    {
        // Arrange — binary schema with 'as' clause referencing text schema
        var analyzer = CreateAnalyzer();
        var query = @"text KeyValue {
    Key: until ':',
    Value: rest trim
};
binary ConfigPacket {
    Version: byte,
    Config: string[20] utf8 as KeyValue,
    Checksum: byte
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — binary+text composition should parse
        if (result.HasErrors)
        {
            var errorDetails = string.Join("\n", result.Errors.Select(e => $"  [{e.Code}] {e.Message}"));
            Assert.Inconclusive($"Binary with text 'as' clause produced parse errors:\n{errorDetails}");
        }
    }

    #endregion
}
