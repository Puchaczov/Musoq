using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class ErrorQualityBinaryTextSchemaTests
{
    #region E-TEXT: Parse<LogLine>() with non-string column

    [TestMethod]
    public void E_TEXT_01_ParseOnNonStringColumn()
    {
        // Arrange — Parse() called on integer column
        var analyzer = CreateAnalyzer();
        var query = @"text LogLine {
    Data: rest
};
select l.Data from #A.Entities() a cross apply Parse(a.Population) l";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Parse() on integer should produce type error
        AssertHasOneOfErrorCodes(result, "Parse() on integer column",
            DiagnosticCode.MQ3005_TypeMismatch,
            DiagnosticCode.MQ3013_CannotResolveMethod,
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    #endregion

    #region E-TEXT: Parse<UndefinedFormat>() referencing undefined text schema

    [TestMethod]
    public void E_TEXT_02_ParseReferencingUndefinedSchema()
    {
        // Arrange — Parse() references undefined schema
        var analyzer = CreateAnalyzer();
        var query = @"select l.Data from #A.Entities() a cross apply Parse<NonExistentSchema>(a.Name) l";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — undefined schema in Parse() should produce error
        AssertHasOneOfErrorCodes(result, "Parse() referencing undefined schema",
            DiagnosticCode.MQ4003_UndefinedSchemaReference,
            DiagnosticCode.MQ3010_UnknownSchema,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
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
            DiagnosticCode.MQ2030_UnsupportedSyntax);
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
select l.Content from #A.Entities() a cross apply Parse<LogLine>(a.Name) l";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — accessing non-existent text field should error
        AssertHasOneOfErrorCodes(result, "accessing non-existent text field",
            DiagnosticCode.MQ3001_UnknownColumn,
            DiagnosticCode.MQ3014_InvalidPropertyAccess,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    #endregion

    #region E-TEXT: Multiple text schemas, one referencing wrong one

    [TestMethod]
    public void E_TEXT_05_ParseReferencingWrongSchemaType()
    {
        // Arrange — Define binary schema but use Parse<Header>() (text function) with it
        var analyzer = CreateAnalyzer();
        var query = @"binary Header {
    Magic: int le
};
select l.Magic from #A.Entities() a cross apply Parse(a.Name) l";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Parse() with binary schema name may confuse
        AssertHasOneOfErrorCodes(result, "Parse() with binary schema name",
            DiagnosticCode.MQ4003_UndefinedSchemaReference,
            DiagnosticCode.MQ3010_UnknownSchema,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    #endregion

    #region E-TEXT: Interpret<LogLine>() referencing text schema

    [TestMethod]
    public void E_TEXT_06_InterpretReferencingTextSchema()
    {
        // Arrange — Define text schema but use Interpret() (binary function) with it
        var analyzer = CreateAnalyzer();
        var query = @"text LogLine {
    Data: rest
};
select h.Data from #A.Entities() a cross apply Interpret<LogLine>(a.Name) h";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Interpret() with text schema name should error
        AssertHasOneOfErrorCodes(result, "Interpret() with text schema name",
            DiagnosticCode.MQ4003_UndefinedSchemaReference,
            DiagnosticCode.MQ3010_UnknownSchema,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    #endregion
}
