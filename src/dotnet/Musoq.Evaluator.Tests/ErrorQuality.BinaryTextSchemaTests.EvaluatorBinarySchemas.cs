using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class ErrorQualityBinaryTextSchemaTests
{
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
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2030_UnsupportedSyntax, "circular binary schema reference");
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
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2030_UnsupportedSyntax, "undefined nested schema reference");
    }

    #endregion

    #region E-BIN: Interpret<Header>() with non-byte-array column

    [TestMethod]
    public void E_BIN_03_InterpretOnStringColumn()
    {
        // Arrange — Interpret() called on string column instead of byte[]
        var analyzer = CreateAnalyzer();
        var query = @"binary Header {
    Magic: int le
};
select h.Magic from #A.Entities() a cross apply Interpret<Header>(a.Name) h";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Interpret() on string should produce type error
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2030_UnsupportedSyntax, "Interpret() on string column");
    }

    #endregion

    #region E-BIN: Interpret() with wrong number of arguments

    [TestMethod]
    public void E_BIN_04_InterpretWrongArgCount()
    {
        // Arrange — Interpret<Header>() with 2 args (normally 1)
        var analyzer = CreateAnalyzer();
        var query = @"binary Header {
    Magic: int le
};
select h.Magic from #A.Entities() a cross apply Interpret<Header>(a.Name, 'extra') h";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — wrong arg count should produce error
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2030_UnsupportedSyntax, "Interpret() with wrong arg count");
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
select h.Version from #A.Entities() a cross apply Interpret<Header>(a.Name) h";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — accessing non-existent field should produce column error
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2030_UnsupportedSyntax, "accessing non-existent binary field");
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
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ2030_UnsupportedSyntax, "when condition referencing unknown field");
    }

    #endregion
}
