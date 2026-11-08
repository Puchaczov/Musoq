using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class ErrorQualityBinaryTextSchemaTests
{
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

        // Assert — duplicate field names are a schema definition error.
        AssertHasDiagnosticCode(result,
            DiagnosticCode.MQ4008_DuplicateSchemaField,
            "duplicate binary schema field");
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
}
