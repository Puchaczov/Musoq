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
        AssertHasOneOfErrorCodes(result, "circular binary schema reference",
            DiagnosticCode.MQ4004_CircularSchemaReference,
            DiagnosticCode.MQ3016_CircularReference,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
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
            DiagnosticCode.MQ2030_UnsupportedSyntax);
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
        AssertHasOneOfErrorCodes(result, "Interpret() on string column",
            DiagnosticCode.MQ3005_TypeMismatch,
            DiagnosticCode.MQ3013_CannotResolveMethod,
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
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
        AssertHasOneOfErrorCodes(result, "Interpret() with wrong arg count",
            DiagnosticCode.MQ3006_InvalidArgumentCount,
            DiagnosticCode.MQ3013_CannotResolveMethod,
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
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
        AssertHasOneOfErrorCodes(result, "accessing non-existent binary field",
            DiagnosticCode.MQ3001_UnknownColumn,
            DiagnosticCode.MQ3014_InvalidPropertyAccess,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
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
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    #endregion
}
