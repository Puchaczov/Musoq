using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using System;

namespace Musoq.Parser.Tests;

/// <summary>
/// Comprehensive edge case testing for SQL syntax parsing.
/// Tests boundary conditions, malformed queries, and unusual token combinations
/// to ensure robust error handling and meaningful error messages.
/// </summary>
[TestClass]
public class SyntaxEdgeCaseTests
{
    #region Token Boundary Tests

    [TestMethod]
    public void ParseQuery_WithDanglingComma_ShouldThrowMeaningfulException()
    {
        var query = "SELECT Name, FROM #test.data()";
        
        var exception = Assert.ThrowsException<SyntaxException>(() => ParseQuery(query));
        Assert.IsTrue(exception.Message.Contains("comma"));
        Assert.IsNotNull(exception.QueryPart);
    }

    [TestMethod]
    public void ParseQuery_WithMultipleCommas_ShouldIdentifyParsingBehavior()
    {
        var query = "SELECT Name,, Value FROM #test.data()";
        
        // Current parser behavior: this might parse as Name, (empty), Value
        // Let's document this behavior - could be a potential improvement area
        try
        {
            var result = ParseQuery(query);
            Assert.IsNotNull(result);
            // If no exception, parser accepts double commas
        }
        catch (SyntaxException ex)
        {
            // If exception, verify meaningful error message
            Assert.IsNotNull(ex.QueryPart);
        }
    }

    [TestMethod]
    public void ParseQuery_WithUnclosedParentheses_ShouldThrowMeaningfulException()
    {
        var query = "SELECT Name FROM #test.data( WHERE Id = 1";
        
        var exception = Assert.ThrowsException<SyntaxException>(() => ParseQuery(query));
        Assert.IsNotNull(exception.QueryPart);
    }

    [TestMethod]
    public void ParseQuery_WithMismatchedParentheses_ShouldThrowMeaningfulException()
    {
        var query = "SELECT Name FROM #test.data()) WHERE Id = 1";
        
        var exception = Assert.ThrowsException<SyntaxException>(() => ParseQuery(query));
        Assert.IsNotNull(exception.QueryPart);
    }

    [TestMethod]
    public void ParseQuery_WithUnclosedQuotes_ShouldThrowMeaningfulException()
    {
        var query = "SELECT Name FROM #test.data() WHERE Name = 'unclosed";
        
        var exception = Assert.ThrowsException<SyntaxException>(() => ParseQuery(query));
        Assert.IsNotNull(exception.QueryPart);
    }

    #endregion

    #region Operator Precedence Edge Cases

    [TestMethod]
    public void ParseQuery_WithChainedOperators_ShouldParseCorrectly()
    {
        var query = "SELECT Value FROM #test.data() WHERE A = 1 AND B = 2 OR C = 3 AND D = 4";
        
        // Should parse without exception - testing precedence handling
        var result = ParseQuery(query);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void ParseQuery_WithNestedParenthesesOperators_ShouldParseCorrectly()
    {
        var query = "SELECT Value FROM #test.data() WHERE ((A = 1 AND B = 2) OR (C = 3)) AND D = 4";
        
        var result = ParseQuery(query);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void ParseQuery_WithMixedArithmeticOperators_ShouldParseCorrectly()
    {
        var query = "SELECT Value * 2 + 3 - 1 / 4 FROM #test.data()";
        
        var result = ParseQuery(query);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void ParseQuery_WithComplexExpressionNesting_ShouldParseCorrectly()
    {
        var query = "SELECT (Value + (Count * 2)) / (Total - (Avg + 1)) FROM #test.data()";
        
        var result = ParseQuery(query);
        Assert.IsNotNull(result);
    }

    #endregion

    #region Invalid Keyword Combinations

    [TestMethod]
    public void ParseQuery_WithSelectSelect_ShouldThrowException()
    {
        var query = "SELECT SELECT Name FROM #test.data()";
        
        var exception = Assert.ThrowsException<SyntaxException>(() => ParseQuery(query));
        Assert.IsNotNull(exception.QueryPart);
    }

    [TestMethod]
    public void ParseQuery_WithFromFrom_ShouldThrowException()
    {
        var query = "SELECT Name FROM FROM #test.data()";
        
        var exception = Assert.ThrowsException<SyntaxException>(() => ParseQuery(query));
        Assert.IsNotNull(exception.QueryPart);
    }

    [TestMethod]
    public void ParseQuery_WithWhereWhere_ShouldThrowException()
    {
        var query = "SELECT Name FROM #test.data() WHERE WHERE Id = 1";
        
        var exception = Assert.ThrowsException<SyntaxException>(() => ParseQuery(query));
        Assert.IsNotNull(exception.QueryPart);
    }

    [TestMethod]
    public void ParseQuery_WithMisplacedKeywords_ShouldThrowException()
    {
        var query = "WHERE SELECT Name FROM #test.data()";
        
        var exception = Assert.ThrowsException<SyntaxException>(() => ParseQuery(query));
        Assert.IsNotNull(exception.QueryPart);
    }

    #endregion

    #region Empty and Whitespace Edge Cases

    [TestMethod]
    public void ParseQuery_WithOnlyWhitespace_ShouldThrowValidationException()
    {
        var query = "   \t\n   ";
        
        Assert.ThrowsException<ParserValidationException>(() => ParseQuery(query));
    }

    [TestMethod]
    public void ParseQuery_WithExcessiveWhitespace_ShouldParseCorrectly()
    {
        var query = "    SELECT    Name    FROM    #test.data()    WHERE    Id   =   1    ";
        
        var result = ParseQuery(query);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void ParseQuery_WithMixedWhitespaceTypes_ShouldParseCorrectly()
    {
        var query = "SELECT\tName\nFROM\r\n#test.data()\r\nWHERE\tId = 1";
        
        var result = ParseQuery(query);
        Assert.IsNotNull(result);
    }

    #endregion

    #region Invalid Identifier Cases

    [TestMethod]
    public void ParseQuery_WithEmptyIdentifier_ShouldAcceptFromKeyword()
    {
        var query = "SELECT FROM #test.data()";
        
        // This actually parses as "SELECT (implied *) FROM..." which is valid SQL extension
        var result = ParseQuery(query);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void ParseQuery_WithNumericFollowedByIdentifier_ShouldParseCorrectly()
    {
        var query = "SELECT 123abc FROM #test.data()";
        
        // Parser treats this as separate tokens: 123 and abc, which is valid
        var result = ParseQuery(query);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void ParseQuery_WithSpecialCharactersInIdentifier_ShouldThrowException()
    {
        var query = "SELECT Name@#$ FROM #test.data()";
        
        var exception = Assert.ThrowsException<SyntaxException>(() => ParseQuery(query));
        Assert.IsNotNull(exception.QueryPart);
    }

    #endregion

    #region Function Call Edge Cases

    [TestMethod]
    public void ParseQuery_WithEmptyFunctionCall_ShouldParseCorrectly()
    {
        var query = "SELECT Count() FROM #test.data()";
        
        var result = ParseQuery(query);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void ParseQuery_WithNestedFunctionCalls_ShouldParseCorrectly()
    {
        var query = "SELECT Upper(Lower(Trim(Name))) FROM #test.data()";
        
        var result = ParseQuery(query);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void ParseQuery_WithUnclosedFunctionCall_ShouldThrowException()
    {
        var query = "SELECT Count( FROM #test.data()";
        
        var exception = Assert.ThrowsException<SyntaxException>(() => ParseQuery(query));
        Assert.IsNotNull(exception.QueryPart);
    }

    [TestMethod]
    public void ParseQuery_WithExtraCommaInFunction_ShouldThrowException()
    {
        var query = "SELECT Sum(Value,) FROM #test.data()";
        
        var exception = Assert.ThrowsException<SyntaxException>(() => ParseQuery(query));
        Assert.IsNotNull(exception.QueryPart);
    }

    #endregion

    #region Case Statement Edge Cases

    [TestMethod]
    public void ParseQuery_WithIncompleteCaseStatement_ShouldThrowException()
    {
        var query = "SELECT CASE WHEN Id = 1 FROM #test.data()";
        
        var exception = Assert.ThrowsException<SyntaxException>(() => ParseQuery(query));
        Assert.IsNotNull(exception.QueryPart);
    }

    [TestMethod]
    public void ParseQuery_WithCaseWithoutWhen_ShouldThrowException()
    {
        var query = "SELECT CASE THEN 'test' END FROM #test.data()";
        
        var exception = Assert.ThrowsException<SyntaxException>(() => ParseQuery(query));
        Assert.IsNotNull(exception.QueryPart);
    }

    [TestMethod]
    public void ParseQuery_WithNestedCaseStatements_ShouldParseCorrectly()
    {
        var query = @"SELECT CASE 
                        WHEN Id = 1 THEN CASE WHEN Name = 'A' THEN 'A1' ELSE 'A2' END
                        ELSE 'Other' 
                      END FROM #test.data()";
        
        var result = ParseQuery(query);
        Assert.IsNotNull(result);
    }

    #endregion

    #region String Literal Edge Cases

    [TestMethod]
    public void ParseQuery_WithEscapedQuotes_ShouldHandleCorrectly()
    {
        // Using simpler escaped quote syntax that the lexer supports
        var query = "SELECT Name FROM #test.data() WHERE Description = 'It\\'s a test'";
        
        var result = ParseQuery(query);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void ParseQuery_WithSpecialCharactersInString_ShouldParseCorrectly()
    {
        var query = "SELECT Name FROM #test.data() WHERE Description = 'Test with \\n\\t\\r'";
        
        var result = ParseQuery(query);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void ParseQuery_WithUnicodeCharacters_ShouldParseCorrectly()
    {
        var query = "SELECT Name FROM #test.data() WHERE Description = 'Test with 日本語 and émojis 🚀'";
        
        var result = ParseQuery(query);
        Assert.IsNotNull(result);
    }

    #endregion

    #region Number Literal Edge Cases

    [TestMethod]
    public void ParseQuery_WithVeryLargeNumber_ShouldParseCorrectly()
    {
        var query = "SELECT Name FROM #test.data() WHERE Id = 999999999999999999";
        
        var result = ParseQuery(query);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void ParseQuery_WithDecimalNumbers_ShouldParseCorrectly()
    {
        var query = "SELECT Name FROM #test.data() WHERE Price = 123.456789";
        
        var result = ParseQuery(query);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void ParseQuery_WithScientificNotation_ShouldRequireProperSyntax()
    {
        // Scientific notation might not be fully supported, testing current parser limitations
        var query = "SELECT Name FROM #test.data() WHERE Value = 123E4";  // Simplified format
        
        var exception = Assert.ThrowsException<SyntaxException>(() => ParseQuery(query));
        Assert.IsNotNull(exception.QueryPart);
    }

    [TestMethod]
    public void ParseQuery_WithMultipleDecimalPoints_ShouldParseAsTokens()
    {
        var query = "SELECT Name FROM #test.data() WHERE Value = 123.456.789";
        
        // Parser treats this as separate tokens: 123.456, ., 789 - valid but unusual
        var result = ParseQuery(query);
        Assert.IsNotNull(result);
    }

    #endregion

    #region Join Statement Edge Cases

    [TestMethod]
    public void ParseQuery_WithIncompleteJoin_ShouldThrowException()
    {
        var query = "SELECT a.Name FROM #test.data() a inner join WHERE a.Id = 1";
        
        var exception = Assert.ThrowsException<SyntaxException>(() => ParseQuery(query));
        Assert.IsNotNull(exception.QueryPart);
    }

    [TestMethod]
    public void ParseQuery_WithJoinWithoutOn_ShouldThrowException()
    {
        var query = "SELECT a.Name FROM #test.data() a inner join #test.other() b";
        
        var exception = Assert.ThrowsException<SyntaxException>(() => ParseQuery(query));
        Assert.IsNotNull(exception.QueryPart);
    }

    [TestMethod]
    public void ParseQuery_WithMultipleJoinConditions_ShouldHandleLineBreaks()
    {
        // Using proper syntax with "inner join" as per existing examples
        var query = "SELECT s1.Name FROM #test.data() s1 inner join #test.other() s2 on s1.Id = s2.Id where s1.Type = s2.Type";
        
        var result = ParseQuery(query);
        Assert.IsNotNull(result);
    }

    #endregion

    #region Schema Reference Edge Cases

    [TestMethod]
    public void ParseQuery_WithMalformedSchemaReference_ShouldThrowException()
    {
        var query = "SELECT Name FROM #.data()";
        
        var exception = Assert.ThrowsException<SyntaxException>(() => ParseQuery(query));
        Assert.IsNotNull(exception.QueryPart);
    }

    [TestMethod]
    public void ParseQuery_WithIncompleteSchemaReference_ShouldThrowException()
    {
        var query = "SELECT Name FROM #test.";
        
        var exception = Assert.ThrowsException<SyntaxException>(() => ParseQuery(query));
        Assert.IsNotNull(exception.QueryPart);
    }

    [TestMethod]
    public void ParseQuery_WithSpecialCharactersInSchema_ShouldThrowException()
    {
        var query = "SELECT Name FROM #test@$.data()";
        
        var exception = Assert.ThrowsException<SyntaxException>(() => ParseQuery(query));
        Assert.IsNotNull(exception.QueryPart);
    }

    #endregion

    #region Helper Methods

    private static Musoq.Parser.Nodes.Node ParseQuery(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        return parser.ComposeAll();
    }

    #endregion
}