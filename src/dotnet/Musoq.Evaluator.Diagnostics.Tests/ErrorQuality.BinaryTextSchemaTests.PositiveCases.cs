using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class ErrorQualityBinaryTextSchemaTests
{
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
        var errorDetails = string.Join("\n", result.Errors.Select(e => $"  [{e.Code}] {e.Message}"));
        Assert.IsFalse(result.HasErrors, $"Well-formed binary schema produced parse errors:\n{errorDetails}");
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
        var errorDetails = string.Join("\n", result.Errors.Select(e => $"  [{e.Code}] {e.Message}"));
        Assert.IsFalse(result.HasErrors, $"Well-formed text schema produced parse errors:\n{errorDetails}");
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
        var errorDetails = string.Join("\n", result.Errors.Select(e => $"  [{e.Code}] {e.Message}"));
        Assert.IsFalse(result.HasErrors, $"Nested binary schema reference produced parse errors:\n{errorDetails}");
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
        var errorDetails = string.Join("\n", result.Errors.Select(e => $"  [{e.Code}] {e.Message}"));
        Assert.IsFalse(result.HasErrors, $"Binary with text 'as' clause produced parse errors:\n{errorDetails}");
    }

    #endregion
}
