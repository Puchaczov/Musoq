#nullable enable
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Error Message Quality Audit — Phase 2B: Binary/Text Code Generation Stress Tests.
///     These test extreme and edge-case scenarios that stress the code generation pipeline
///     for interpretation schemas.
///     Covers: E-BINGEN (binary codegen), E-TEXTGEN (text codegen), E-MIXQUERY (mixed queries).
/// </summary>
[TestClass]
public class ErrorQuality_Phase2B_CodegenStressTests : BasicEntityTestBase
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

    private static void AssertHasErrorCode(QueryAnalysisResult result, DiagnosticCode expectedCode, string context)
    {
        Assert.IsTrue(result.HasErrors || !result.IsParsed,
            $"Expected error code {expectedCode} ({context}) but query succeeded. IsParsed: {result.IsParsed}");

        if (result.HasErrors)
        {
            var errorDetails = string.Join("\n", result.Errors.Select(e => $"  [{e.Code}] {e.Message}"));
            Assert.IsTrue(
                result.Errors.Any(e => e.Code == expectedCode),
                $"Expected error code {expectedCode} ({context}) but got:\n{errorDetails}");
        }
    }

    private static void AssertHasOneOfErrorCodes(QueryAnalysisResult result, string context,
        params DiagnosticCode[] expectedCodes)
    {
        Assert.IsTrue(result.HasErrors || !result.IsParsed,
            $"Expected one of [{string.Join(", ", expectedCodes)}] ({context}) but query succeeded");

        if (result.HasErrors)
        {
            var hasExpected = result.Errors.Any(e => expectedCodes.Contains(e.Code));
            if (!hasExpected)
            {
                var errorDetails = string.Join("\n", result.Errors.Select(e => $"  [{e.Code}] {e.Message}"));
                Assert.Fail(
                    $"Expected one of [{string.Join(", ", expectedCodes)}] ({context}) but got:\n{errorDetails}");
            }
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

    private static void DocumentBehavior(QueryAnalysisResult result, string expectedBehavior, bool shouldHaveErrors)
    {
        if (shouldHaveErrors)
            Assert.IsTrue(result.HasErrors || !result.IsParsed,
                $"Behavior documentation: {expectedBehavior} - but query succeeded");
    }

    #endregion

    // ============================================================================
    // E-BINGEN: Binary Schema Code Generation Stress Tests
    // ============================================================================

    #region E-BINGEN: Many fields in a single schema

    [TestMethod]
    public void E_BINGEN_01_ManyFieldsBinarySchema()
    {
        // Arrange — binary schema with large number of fields
        var analyzer = CreateAnalyzer();
        var query = @"binary WideFormat {
    F1: byte,
    F2: byte,
    F3: short le,
    F4: short le,
    F5: int le,
    F6: int le,
    F7: long le,
    F8: long le,
    F9: float le,
    F10: double le,
    F11: byte,
    F12: byte,
    F13: short le,
    F14: short le,
    F15: int le,
    F16: int le
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Many fields should be handled
        // Assert  binary schema with 16 fields should be handled`n        AssertNoErrors(result);
    }

    #endregion

    #region E-BINGEN: Deeply nested binary schemas

    [TestMethod]
    public void E_BINGEN_02_DeeplyNestedBinarySchemas()
    {
        // Arrange — 3 levels of nesting
        var analyzer = CreateAnalyzer();
        var query = @"binary Inner {
    Value: int le
};
binary Middle {
    Prefix: byte,
    Nested: Inner
};
binary Outer {
    Header: byte,
    Content: Middle,
    Footer: byte
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — 3-level nesting should work
        // Assert  3-level nested binary schemas should be handled`n        AssertNoErrors(result);
    }

    #endregion

    #region E-BINGEN: All primitive types in one schema

    [TestMethod]
    public void E_BINGEN_03_AllPrimitiveTypes()
    {
        // Arrange — every primitive type
        var analyzer = CreateAnalyzer();
        var query = @"binary AllTypes {
    A: byte,
    B: sbyte,
    C: short le,
    D: ushort le,
    E: int le,
    F: uint le,
    G: long le,
    H: ulong le,
    I: float le,
    J: double le
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — all types should parse and analyze
        // Assert  all primitive types in binary schema`n        AssertNoErrors(result);
    }

    #endregion

    #region E-BINGEN: Mixed endianness in same schema

    [TestMethod]
    public void E_BINGEN_04_MixedEndianness()
    {
        // Arrange — some le, some be in same schema
        var analyzer = CreateAnalyzer();
        var query = @"binary MixedEndian {
    LittleInt: int le,
    BigInt: int be,
    LittleShort: short le,
    BigShort: short be
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — mixed endianness should be valid
        // Assert  mixed endianness in single schema should be valid`n        AssertNoErrors(result);
    }

    #endregion

    #region E-BINGEN: Binary schema with string and byte array

    [TestMethod]
    public void E_BINGEN_05_StringAndByteArrayFields()
    {
        // Arrange — combining string, byte[], and primitives
        var analyzer = CreateAnalyzer();
        var query = @"binary FileHeader {
    Magic: byte[4],
    NameLength: int le,
    Name: string[16] utf8,
    Payload: byte[32]
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — mixed field types should work
        // Assert  binary schema with string and byte array fields`n        AssertNoErrors(result);
    }

    #endregion

    #region E-BINGEN: Binary schema with conditional 'when' clause

    [TestMethod]
    public void E_BINGEN_06_ConditionalWhenClause()
    {
        // Arrange — conditional field
        var analyzer = CreateAnalyzer();
        var query = @"binary Packet {
    Type: byte,
    Size: int le when Type = 1,
    Data: byte[4] when Type = 2
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — when clauses should be handled
        // Assert  binary schema with when conditional fields`n        AssertNoErrors(result);
    }

    #endregion

    #region E-BINGEN: Binary schema with 'at' offset positioning

    [TestMethod]
    public void E_BINGEN_07_AtOffsetPositioning()
    {
        // Arrange — explicit offset positioning
        var analyzer = CreateAnalyzer();
        var query = @"binary FixedLayout {
    Magic: int le at 0,
    Version: short le at 4,
    Flags: byte at 6
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — 'at' offset should be handled
        // Assert  binary schema with 'at' offset positioning`n        AssertNoErrors(result);
    }

    #endregion

    #region E-BINGEN: Binary schema with bits fields

    [TestMethod]
    public void E_BINGEN_08_BitsFields()
    {
        // Arrange — bits extraction
        var analyzer = CreateAnalyzer();
        var query = @"binary Flags {
    HighNibble: bits[4],
    LowNibble: bits[4]
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — bits fields should be handled
        // Assert  binary schema with bits fields`n        AssertNoErrors(result);
    }

    #endregion

    #region E-BINGEN: Schema name collision with SQL keywords

    [TestMethod]
    public void E_BINGEN_09_SchemaNameCollisionWithKeyword()
    {
        // Arrange — schema name that is also a SQL keyword
        var analyzer = CreateAnalyzer();
        var query = @"binary Select {
    Value: int le
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — keyword as schema name is now allowed (context is unambiguous)
        Assert.IsTrue(!result.HasErrors && result.IsParsed,
            "SQL keyword as schema name should parse successfully in schema context");
    }

    #endregion

    #region E-BINGEN: Field name collision with SQL keywords

    [TestMethod]
    public void E_BINGEN_10_FieldNameCollisionWithKeyword()
    {
        // Arrange — field named 'from' which is a keyword
        var analyzer = CreateAnalyzer();
        var query = @"binary Header {
    from: int le
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — keyword as field name may be rejected
        // Assert  SQL keyword as field name may be rejected`n        AssertNoErrors(result);
    }

    #endregion

    // ============================================================================
    // E-TEXTGEN: Text Schema Code Generation Stress Tests
    // ============================================================================

    #region E-TEXTGEN: Many fields in single text schema

    [TestMethod]
    public void E_TEXTGEN_01_ManyFieldsTextSchema()
    {
        // Arrange — text schema with many fields
        var analyzer = CreateAnalyzer();
        var query = @"text LogLine {
    Field1: until ' ',
    Field2: until ' ',
    Field3: until ' ',
    Field4: until ' ',
    Field5: until ' ',
    Field6: until ' ',
    Field7: until ' ',
    Field8: rest
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — many fields should be handled
        // Assert  text schema with 8 fields should be handled`n        AssertNoErrors(result);
    }

    #endregion

    #region E-TEXTGEN: All extraction methods in one schema

    [TestMethod]
    public void E_TEXTGEN_02_AllExtractionMethods()
    {
        // Arrange — using all extraction methods
        var analyzer = CreateAnalyzer();
        var query = @"text CompleteLine {
    Prefix: chars[5],
    Sep1: whitespace,
    Middle: until ':',
    Tag: between '[' ']',
    Final: rest trim
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — all extraction methods should be handled
        // Assert  all text extraction methods in one schema`n        AssertNoErrors(result);
    }

    #endregion

    #region E-TEXTGEN: Text schema with literal field

    [TestMethod]
    public void E_TEXTGEN_03_LiteralField()
    {
        // Arrange — literal text matching
        var analyzer = CreateAnalyzer();
        var query = @"text FixedFormat {
    Marker: literal 'START',
    Data: rest
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — literal field should be handled
        // Assert  text schema with literal field`n        AssertNoErrors(result);
    }

    #endregion

    #region E-TEXTGEN: Text schema with pattern (regex) field

    [TestMethod]
    public void E_TEXTGEN_04_PatternField()
    {
        // Arrange — regex pattern extraction
        var analyzer = CreateAnalyzer();
        var query = @"text LogEntry {
    Timestamp: pattern '\d{4}-\d{2}-\d{2}',
    Rest: rest
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — pattern field should be handled
        // Assert  text schema with regex pattern field`n        AssertNoErrors(result);
    }

    #endregion

    #region E-TEXTGEN: Text schema with token field

    [TestMethod]
    public void E_TEXTGEN_05_TokenField()
    {
        // Arrange — token extraction
        var analyzer = CreateAnalyzer();
        var query = @"text SimpleLine {
    Word: token,
    Remainder: rest
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — token field should be handled
        // Assert  text schema with token field`n        AssertNoErrors(result);
    }

    #endregion

    #region E-TEXTGEN: Text schema name collision with SQL keyword

    [TestMethod]
    public void E_TEXTGEN_06_SchemaNameCollisionWithKeyword()
    {
        // Arrange — schema named 'Where' (SQL keyword)
        var analyzer = CreateAnalyzer();
        var query = @"text Where {
    Data: rest
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — keyword as text schema name is now allowed (context is unambiguous)
        Assert.IsTrue(!result.HasErrors && result.IsParsed,
            "SQL keyword as text schema name should parse successfully in schema context");
    }

    #endregion

    #region E-TEXTGEN: Text schema field name collision with SQL keyword

    [TestMethod]
    public void E_TEXTGEN_07_FieldNameCollisionWithKeyword()
    {
        // Arrange — field named 'select'
        var analyzer = CreateAnalyzer();
        var query = @"text LogLine {
    select: rest
};
select 1 from #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — keyword as field name may be rejected
        // Assert  SQL keyword as text field name may be rejected`n        AssertNoErrors(result);
    }

    #endregion

    // ============================================================================
    // E-MIXQUERY: Mixed Interpretation Schema Query Tests
    // ============================================================================

    #region E-MIXQUERY: Binary schema + CTE combination

    [TestMethod]
    public void E_MIXQUERY_01_BinarySchemaWithCte()
    {
        // Arrange — binary schema definition + CTE + query
        var analyzer = CreateAnalyzer();
        var query = @"binary Header {
    Magic: int le,
    Version: short le
};
with source as (
    select Name from #A.Entities()
)
select s.Name from source s";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — binary schema + CTE should parse/analyze
        // Assert  binary schema with CTE combination`n        AssertNoErrors(result);
    }

    #endregion

    #region E-MIXQUERY: Text schema + GROUP BY combination

    [TestMethod]
    public void E_MIXQUERY_02_TextSchemaWithGroupBy()
    {
        // Arrange — text schema definition + GROUP BY query
        var analyzer = CreateAnalyzer();
        var query = @"text LogLine {
    Level: until ' ',
    Message: rest
};
select City, Count(1) as Cnt from #A.Entities() group by City";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — text schema + GROUP BY should work independently
        // Assert  text schema with GROUP BY combination`n        AssertNoErrors(result);
    }

    #endregion

    #region E-MIXQUERY: Multiple schemas + JOIN

    [TestMethod]
    public void E_MIXQUERY_03_MultipleSchemasWithJoin()
    {
        // Arrange — both binary and text schemas + JOIN query
        var analyzer = CreateAnalyzer();
        var query = @"binary Header {
    Magic: int le
};
text LogLine {
    Data: rest
};
select a.Name, b.Name from #A.Entities() a inner join #B.Entities() b on a.City = b.City";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — multiple schemas + JOIN should parse/analyze
        // Assert  multiple schemas with JOIN combination`n        AssertNoErrors(result);
    }

    #endregion

    #region E-MIXQUERY: Schema + set operation (UNION)

    [TestMethod]
    public void E_MIXQUERY_04_SchemaWithUnion()
    {
        // Arrange — binary schema + UNION
        var analyzer = CreateAnalyzer();
        var query = @"binary Header {
    Magic: int le
};
select Name from #A.Entities()
union (Name)
select Name from #B.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — schema + UNION should work
        // Assert  binary schema definition with UNION query`n        AssertNoErrors(result);
    }

    #endregion

    #region E-MIXQUERY: Schema + ORDER BY + SKIP/TAKE

    [TestMethod]
    public void E_MIXQUERY_05_SchemaWithOrderBySkipTake()
    {
        // Arrange — text schema + complex query features
        var analyzer = CreateAnalyzer();
        var query = @"text LogLine {
    Data: rest
};
select Name, Population from #A.Entities() order by Population desc skip 0 take 10";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — schema + ORDER BY + SKIP/TAKE should work
        // Assert  text schema with ORDER BY, SKIP, TAKE`n        AssertNoErrors(result);
    }

    #endregion

    #region E-MIXQUERY: Schema + CASE expression

    [TestMethod]
    public void E_MIXQUERY_06_SchemaWithCaseExpression()
    {
        // Arrange — binary schema + CASE
        var analyzer = CreateAnalyzer();
        var query = @"binary Header {
    Magic: int le
};
select 
    CASE WHEN Population > 100 THEN 'Large' ELSE 'Small' END as SizeCategory
from #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — schema + CASE should work
        // Assert  binary schema with CASE expression query`n        AssertNoErrors(result);
    }

    #endregion

    #region E-MIXQUERY: Schema + HAVING clause

    [TestMethod]
    public void E_MIXQUERY_07_SchemaWithHaving()
    {
        // Arrange — text schema + GROUP BY + HAVING
        var analyzer = CreateAnalyzer();
        var query = @"text LogLine {
    Data: rest
};
select City, Count(1) as Cnt from #A.Entities() group by City having Count(1) > 0";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — schema + HAVING should work
        // Assert  text schema with GROUP BY and HAVING`n        AssertNoErrors(result);
    }

    #endregion
}
