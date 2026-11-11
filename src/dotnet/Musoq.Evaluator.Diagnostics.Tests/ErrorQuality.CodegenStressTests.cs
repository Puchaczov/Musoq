using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Error Message Quality Audit — Phase 2B: Binary/Text Code Generation Stress Tests.
///     These test extreme and edge-case scenarios that stress the code generation pipeline
///     for interpretation schemas.
///     Covers: E-BINGEN (binary codegen), E-TEXTGEN (text codegen), E-MIXQUERY (mixed queries).
/// </summary>
[global::Musoq.Tests.Common.CultureTestClass]
public class ErrorQualityCodegenStressTests : BasicEntityTestBase
{
    // ============================================================================
    // E-BINGEN: Binary Schema Code Generation Stress Tests
    // ============================================================================

    #region E-BINGEN: Many fields in a single schema

    [TestMethod]
    public void E_BINGEN_01_ManyFieldsBinarySchema()
    {

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

        using var compiled = Musoq.Converter.InstanceCreator.CompileForExecution(
            query, System.Guid.NewGuid().ToString(), CreateSchemaProvider(), LoggerResolver, TestCompilationOptions);
        using var table = compiled.Run(TestContext.CancellationToken);
        Assert.AreEqual(1, table.Count);


    }

    #endregion

    #region E-BINGEN: Deeply nested binary schemas

    [TestMethod]
    public void E_BINGEN_02_DeeplyNestedBinarySchemas()
    {

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

        using var compiled = Musoq.Converter.InstanceCreator.CompileForExecution(
            query, System.Guid.NewGuid().ToString(), CreateSchemaProvider(), LoggerResolver, TestCompilationOptions);
        using var table = compiled.Run(TestContext.CancellationToken);
        Assert.AreEqual(1, table.Count);


    }

    #endregion

    #region E-BINGEN: All primitive types in one schema

    [TestMethod]
    public void E_BINGEN_03_AllPrimitiveTypes()
    {

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

        using var compiled = Musoq.Converter.InstanceCreator.CompileForExecution(
            query, System.Guid.NewGuid().ToString(), CreateSchemaProvider(), LoggerResolver, TestCompilationOptions);
        using var table = compiled.Run(TestContext.CancellationToken);
        Assert.AreEqual(1, table.Count);


    }

    #endregion

    #region E-BINGEN: Mixed endianness in same schema

    [TestMethod]
    public void E_BINGEN_04_MixedEndianness()
    {

        var query = @"binary MixedEndian {
    LittleInt: int le,
    BigInt: int be,
    LittleShort: short le,
    BigShort: short be
};
select 1 from #A.Entities()";

        using var compiled = Musoq.Converter.InstanceCreator.CompileForExecution(
            query, System.Guid.NewGuid().ToString(), CreateSchemaProvider(), LoggerResolver, TestCompilationOptions);
        using var table = compiled.Run(TestContext.CancellationToken);
        Assert.AreEqual(1, table.Count);


    }

    #endregion

    #region E-BINGEN: Binary schema with string and byte array

    [TestMethod]
    public void E_BINGEN_05_StringAndByteArrayFields()
    {

        var query = @"binary FileHeader {
    Magic: byte[4],
    NameLength: int le,
    Name: string[16] utf8,
    Payload: byte[32]
};
select 1 from #A.Entities()";

        using var compiled = Musoq.Converter.InstanceCreator.CompileForExecution(
            query, System.Guid.NewGuid().ToString(), CreateSchemaProvider(), LoggerResolver, TestCompilationOptions);
        using var table = compiled.Run(TestContext.CancellationToken);
        Assert.AreEqual(1, table.Count);


    }

    #endregion

    #region E-BINGEN: Binary schema with conditional 'when' clause

    [TestMethod]
    public void E_BINGEN_06_ConditionalWhenClause()
    {

        var query = @"binary Packet {
    Type: byte,
    Size: int le when Type = 1,
    Data: byte[4] when Type = 2
};
select 1 from #A.Entities()";

        using var compiled = Musoq.Converter.InstanceCreator.CompileForExecution(
            query, System.Guid.NewGuid().ToString(), CreateSchemaProvider(), LoggerResolver, TestCompilationOptions);
        using var table = compiled.Run(TestContext.CancellationToken);
        Assert.AreEqual(1, table.Count);


    }

    #endregion

    #region E-BINGEN: Binary schema with 'at' offset positioning

    [TestMethod]
    public void E_BINGEN_07_AtOffsetPositioning()
    {

        var query = @"binary FixedLayout {
    Magic: int le at 0,
    Version: short le at 4,
    Flags: byte at 6
};
select 1 from #A.Entities()";

        using var compiled = Musoq.Converter.InstanceCreator.CompileForExecution(
            query, System.Guid.NewGuid().ToString(), CreateSchemaProvider(), LoggerResolver, TestCompilationOptions);
        using var table = compiled.Run(TestContext.CancellationToken);
        Assert.AreEqual(1, table.Count);


    }

    #endregion

    #region E-BINGEN: Binary schema with bits fields

    [TestMethod]
    public void E_BINGEN_08_BitsFields()
    {

        var query = @"binary Flags {
    HighNibble: bits[4],
    LowNibble: bits[4]
};
select 1 from #A.Entities()";

        using var compiled = Musoq.Converter.InstanceCreator.CompileForExecution(
            query, System.Guid.NewGuid().ToString(), CreateSchemaProvider(), LoggerResolver, TestCompilationOptions);
        using var table = compiled.Run(TestContext.CancellationToken);
        Assert.AreEqual(1, table.Count);


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
        Assert.IsTrue(result is { HasErrors: false, IsParsed: true },
            "SQL keyword as schema name should parse successfully in schema context");
    }

    #endregion

    #region E-BINGEN: Field name collision with SQL keywords

    [TestMethod]
    public void E_BINGEN_10_FieldNameCollisionWithKeyword()
    {

        var analyzer = CreateAnalyzer();
        var query = @"binary Header {
    from: int le
};
select 1 from #A.Entities()";

        var result = analyzer.ValidateSyntax(query);


        Assert.IsTrue(result.IsParsed);
        DiagnosticContractTestAssertions.AssertNoErrors(result, nameof(E_BINGEN_10_FieldNameCollisionWithKeyword));
    }

    #endregion

    // ============================================================================
    // E-TEXTGEN: Text Schema Code Generation Stress Tests
    // ============================================================================

    #region E-TEXTGEN: Many fields in single text schema

    [TestMethod]
    public void E_TEXTGEN_01_ManyFieldsTextSchema()
    {

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

        using var compiled = Musoq.Converter.InstanceCreator.CompileForExecution(
            query, System.Guid.NewGuid().ToString(), CreateSchemaProvider(), LoggerResolver, TestCompilationOptions);
        using var table = compiled.Run(TestContext.CancellationToken);
        Assert.AreEqual(1, table.Count);


    }

    #endregion

    #region E-TEXTGEN: All extraction methods in one schema

    [TestMethod]
    public void E_TEXTGEN_02_AllExtractionMethods()
    {

        var query = @"text CompleteLine {
    Prefix: chars[5],
    Sep1: whitespace,
    Middle: until ':',
    Tag: between '[' ']',
    Final: rest trim
};
select 1 from #A.Entities()";

        using var compiled = Musoq.Converter.InstanceCreator.CompileForExecution(
            query, System.Guid.NewGuid().ToString(), CreateSchemaProvider(), LoggerResolver, TestCompilationOptions);
        using var table = compiled.Run(TestContext.CancellationToken);
        Assert.AreEqual(1, table.Count);


    }

    #endregion

    #region E-TEXTGEN: Text schema with literal field

    [TestMethod]
    public void E_TEXTGEN_03_LiteralField()
    {

        var query = @"text FixedFormat {
    Marker: literal 'START',
    Data: rest
};
select 1 from #A.Entities()";

        using var compiled = Musoq.Converter.InstanceCreator.CompileForExecution(
            query, System.Guid.NewGuid().ToString(), CreateSchemaProvider(), LoggerResolver, TestCompilationOptions);
        using var table = compiled.Run(TestContext.CancellationToken);
        Assert.AreEqual(1, table.Count);


    }

    #endregion

    #region E-TEXTGEN: Text schema with pattern (regex) field

    [TestMethod]
    public void E_TEXTGEN_04_PatternField()
    {

        var query = @"text LogEntry {
    Timestamp: pattern '\d{4}-\d{2}-\d{2}',
    Rest: rest
};
select 1 from #A.Entities()";

        using var compiled = Musoq.Converter.InstanceCreator.CompileForExecution(
            query, System.Guid.NewGuid().ToString(), CreateSchemaProvider(), LoggerResolver, TestCompilationOptions);
        using var table = compiled.Run(TestContext.CancellationToken);
        Assert.AreEqual(1, table.Count);


    }

    #endregion

    #region E-TEXTGEN: Text schema with token field

    [TestMethod]
    public void E_TEXTGEN_05_TokenField()
    {

        var query = @"text SimpleLine {
    Word: token,
    Remainder: rest
};
select 1 from #A.Entities()";

        using var compiled = Musoq.Converter.InstanceCreator.CompileForExecution(
            query, System.Guid.NewGuid().ToString(), CreateSchemaProvider(), LoggerResolver, TestCompilationOptions);
        using var table = compiled.Run(TestContext.CancellationToken);
        Assert.AreEqual(1, table.Count);


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
        Assert.IsTrue(result is { HasErrors: false, IsParsed: true },
            "SQL keyword as text schema name should parse successfully in schema context");
    }

    #endregion

    #region E-TEXTGEN: Text schema field name collision with SQL keyword

    [TestMethod]
    public void E_TEXTGEN_07_FieldNameCollisionWithKeyword()
    {

        var analyzer = CreateAnalyzer();
        var query = @"text LogLine {
    select: rest
};
select 1 from #A.Entities()";

        var result = analyzer.ValidateSyntax(query);


        Assert.IsTrue(result.IsParsed);
        DiagnosticContractTestAssertions.AssertNoErrors(result, nameof(E_TEXTGEN_07_FieldNameCollisionWithKeyword));
    }

    #endregion

    // ============================================================================
    // E-MIXQUERY: Mixed Interpretation Schema Query Tests
    // ============================================================================

    #region E-MIXQUERY: Binary schema + CTE combination

    [TestMethod]
    public void E_MIXQUERY_01_BinarySchemaWithCte()
    {

        var query = @"binary Header {
    Magic: int le,
    Version: short le
};
with source as (
    select Name from #A.Entities()
)
select s.Name from source s";

        using var compiled = Musoq.Converter.InstanceCreator.CompileForExecution(
            query, System.Guid.NewGuid().ToString(), CreateSchemaProvider(), LoggerResolver, TestCompilationOptions);
        using var table = compiled.Run(TestContext.CancellationToken);
        Assert.AreEqual(1, table.Count);


    }

    #endregion

    #region E-MIXQUERY: Text schema + GROUP BY combination

    [TestMethod]
    public void E_MIXQUERY_02_TextSchemaWithGroupBy()
    {

        var query = @"text LogLine {
    Level: until ' ',
    Message: rest
};
select City, Count(1) as Cnt from #A.Entities() group by City";

        using var compiled = Musoq.Converter.InstanceCreator.CompileForExecution(
            query, System.Guid.NewGuid().ToString(), CreateSchemaProvider(), LoggerResolver, TestCompilationOptions);
        using var table = compiled.Run(TestContext.CancellationToken);
        Assert.AreEqual(1, table.Count);


    }

    #endregion

    #region E-MIXQUERY: Multiple schemas + JOIN

    [TestMethod]
    public void E_MIXQUERY_03_MultipleSchemasWithJoin()
    {

        var query = @"binary Header {
    Magic: int le
};
text LogLine {
    Data: rest
};
select a.Name, b.Name from #A.Entities() a inner join #B.Entities() b on a.City = b.City";

        using var compiled = Musoq.Converter.InstanceCreator.CompileForExecution(
            query, System.Guid.NewGuid().ToString(), CreateSchemaProvider(), LoggerResolver, TestCompilationOptions);
        using var table = compiled.Run(TestContext.CancellationToken);
        Assert.AreEqual(0, table.Count);


    }

    #endregion

    #region E-MIXQUERY: Schema + set operation (UNION)

    [TestMethod]
    public void E_MIXQUERY_04_SchemaWithUnion()
    {

        var query = @"binary Header {
    Magic: int le
};
select Name from #A.Entities()
union (Name)
select Name from #B.Entities()";

        using var compiled = Musoq.Converter.InstanceCreator.CompileForExecution(
            query, System.Guid.NewGuid().ToString(), CreateSchemaProvider(), LoggerResolver, TestCompilationOptions);
        using var table = compiled.Run(TestContext.CancellationToken);
        Assert.AreEqual(1, table.Count);


    }

    #endregion

    #region E-MIXQUERY: Schema + ORDER BY + SKIP/TAKE

    [TestMethod]
    public void E_MIXQUERY_05_SchemaWithOrderBySkipTake()
    {

        var query = @"text LogLine {
    Data: rest
};
select Name, Population from #A.Entities() order by Population desc skip 0 take 10";

        using var compiled = Musoq.Converter.InstanceCreator.CompileForExecution(
            query, System.Guid.NewGuid().ToString(), CreateSchemaProvider(), LoggerResolver, TestCompilationOptions);
        using var table = compiled.Run(TestContext.CancellationToken);
        Assert.AreEqual(1, table.Count);


    }

    #endregion

    #region E-MIXQUERY: Schema + CASE expression

    [TestMethod]
    public void E_MIXQUERY_06_SchemaWithCaseExpression()
    {

        var query = @"binary Header {
    Magic: int le
};
select
    CASE WHEN Population > 100 THEN 'Large' ELSE 'Small' END as SizeCategory
from #A.Entities()";

        using var compiled = Musoq.Converter.InstanceCreator.CompileForExecution(
            query, System.Guid.NewGuid().ToString(), CreateSchemaProvider(), LoggerResolver, TestCompilationOptions);
        using var table = compiled.Run(TestContext.CancellationToken);
        Assert.AreEqual(1, table.Count);


    }

    #endregion

    #region E-MIXQUERY: Schema + HAVING clause

    [TestMethod]
    public void E_MIXQUERY_07_SchemaWithHaving()
    {

        var query = @"text LogLine {
    Data: rest
};
select City, Count(1) as Cnt from #A.Entities() group by City having Count(1) > 0";

        using var compiled = Musoq.Converter.InstanceCreator.CompileForExecution(
            query, System.Guid.NewGuid().ToString(), CreateSchemaProvider(), LoggerResolver, TestCompilationOptions);
        using var table = compiled.Run(TestContext.CancellationToken);
        Assert.AreEqual(1, table.Count);


    }

    #endregion

    #region Test Setup

    private static BasicSchemaProvider<BasicEntity> CreateSchemaProvider()
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

    #endregion
}
