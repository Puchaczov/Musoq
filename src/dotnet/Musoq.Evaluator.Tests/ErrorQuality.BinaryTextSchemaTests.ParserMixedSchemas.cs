using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class ErrorQualityBinaryTextSchemaTests
{
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
select h.Magic from #A.Entities() a cross apply Interpret<HeaderFormat>(a.Name) h";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — wrong schema name in Interpret() should produce semantic error
        AssertHasDiagnosticCode(result, DiagnosticCode.MQ3010_UnknownSchema, "Interpret() referencing wrong schema name");
    }

    #endregion
}
