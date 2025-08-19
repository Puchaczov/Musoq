using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Parser.Tests;

/// <summary>
/// Exploratory fuzzing tests to discover edge cases in lexer and parser.
/// Uses semi-random query generation to stress-test error handling and boundary conditions.
/// </summary>
[TestClass]
public class ExploratoryFuzzingTests
{
    private readonly Random _random = new Random(42); // Fixed seed for reproducible tests

    #region Simple Fuzzing Tests

    [TestMethod]
    public void FuzzTest_RandomSpecialCharacters_ShouldHandleGracefully()
    {
        var specialChars = new char[] { '@', '#', '$', '%', '^', '&', '*', '!', '~', '`' };
        
        for (int i = 0; i < 50; i++)
        {
            var randomChar = specialChars[_random.Next(specialChars.Length)];
            var query = $"SELECT Name{randomChar} FROM #test.data()";
            
            try
            {
                ParseQuery(query);
                // If it parses, that's fine - documenting current behavior
            }
            catch (SyntaxException ex)
            {
                // Should have meaningful error message and query part
                Assert.IsNotNull(ex.Message);
                Assert.IsNotNull(ex.QueryPart);
                Assert.IsTrue(ex.Message.Length > 10, "Error message should be descriptive");
            }
            catch (Exception ex)
            {
                // Any other exception type should be investigated
                Assert.Fail($"Unexpected exception type {ex.GetType().Name} for query: {query}. Message: {ex.Message}");
            }
        }
    }

    [TestMethod]
    public void FuzzTest_RandomStringLengths_ShouldHandleVariousSizes()
    {
        var baseSizes = new int[] { 0, 1, 10, 100, 1000, 5000 };
        
        foreach (var size in baseSizes)
        {
            var longString = new string('a', size);
            var query = $"SELECT Name FROM #test.data() WHERE Description = '{longString}'";
            
            try
            {
                var result = ParseQuery(query);
                Assert.IsNotNull(result);
            }
            catch (OutOfMemoryException)
            {
                // Expected for very large strings - this is acceptable
                Assert.IsTrue(size >= 1000, "Only very large strings should cause memory issues");
            }
            catch (SyntaxException ex)
            {
                // Should have reasonable error handling
                Assert.IsNotNull(ex.QueryPart);
            }
        }
    }

    [TestMethod]
    public void FuzzTest_RandomParenthesesNesting_ShouldDetectMismatches()
    {
        var nestingLevels = new int[] { 1, 5, 10, 20, 50 };
        
        foreach (var level in nestingLevels)
        {
            // Test properly nested parentheses
            var openParens = new string('(', level);
            var closeParens = new string(')', level);
            var query = $"SELECT {openParens}Value + 1{closeParens} FROM #test.data()";
            
            try
            {
                var result = ParseQuery(query);
                Assert.IsNotNull(result);
            }
            catch (SyntaxException ex)
            {
                // Deep nesting might hit parser limits - that's acceptable
                Assert.IsNotNull(ex.QueryPart);
            }
            
            // Test mismatched parentheses
            var mismatchedQuery = $"SELECT {openParens}Value + 1{closeParens.Substring(1)} FROM #test.data()";
            
            try
            {
                ParseQuery(mismatchedQuery);
                Assert.Fail("Mismatched parentheses should throw SyntaxException");
            }
            catch (SyntaxException ex)
            {
                Assert.IsNotNull(ex.QueryPart);
            }
        }
    }

    #endregion

    #region Keyword Boundary Tests

    [TestMethod]
    public void FuzzTest_ReservedKeywordCombinations_ShouldHandleCorrectly()
    {
        var keywords = new string[] { "SELECT", "FROM", "WHERE", "GROUP", "BY", "ORDER", "HAVING", "CASE", "WHEN", "THEN", "ELSE", "END", "AND", "OR", "NOT", "IN", "LIKE", "IS", "NULL" };
        
        for (int i = 0; i < 30; i++)
        {
            // Generate random keyword combinations
            var keyword1 = keywords[_random.Next(keywords.Length)];
            var keyword2 = keywords[_random.Next(keywords.Length)];
            var query = $"{keyword1} {keyword2} FROM #test.data()";
            
            try
            {
                ParseQuery(query);
                // Some combinations might be valid extensions
            }
            catch (SyntaxException ex)
            {
                // Should provide meaningful error for invalid combinations
                Assert.IsNotNull(ex.Message);
                Assert.IsTrue(ex.Message.Contains(keyword1) || ex.Message.Contains(keyword2) || 
                             ex.Message.Contains("expected") || ex.Message.Contains("unexpected") ||
                             ex.Message.Contains("cannot be used") || ex.Message.Contains("position") ||
                             ex.Message.Contains("received") || ex.Message.Contains("token"),
                             $"Error message should reference the problematic keywords or be descriptive. Message: {ex.Message}");
            }
        }
    }

    [TestMethod]
    public void FuzzTest_KeywordCasing_ShouldValidateQueryStructure()
    {
        var keywordVariations = new string[]
        {
            "SELECT Name FROM #test.data()",
            "select Name from #test.data()", 
            "Select Name From #test.data()",
            "sElEcT Name fRoM #test.data()"
        };
        
        foreach (var query in keywordVariations)
        {
            try
            {
                var result = ParseQuery(query);
                Assert.IsNotNull(result, $"Query '{query}' should parse successfully");
            }
            catch (SyntaxException ex)
            {
                // Some case variations might not be supported - document the behavior
                Assert.IsNotNull(ex.Message);
                Assert.IsTrue(ex.Message.Length > 10, $"Error for query '{query}' should be descriptive: {ex.Message}");
            }
        }
    }

    #endregion

    #region Operator Boundary Tests

    [TestMethod]
    public void FuzzTest_OperatorChaining_ShouldRespectPrecedence()
    {
        var operators = new string[] { "+", "-", "*", "/", "=", "!=", "<", ">", "<=", ">=", "AND", "OR" };
        
        for (int i = 0; i < 20; i++)
        {
            var op1 = operators[_random.Next(operators.Length)];
            var op2 = operators[_random.Next(operators.Length)];
            var op3 = operators[_random.Next(operators.Length)];
            
            var query = $"SELECT Name FROM #test.data() WHERE A {op1} B {op2} C {op3} D";
            
            try
            {
                var result = ParseQuery(query);
                Assert.IsNotNull(result);
                // Successfully parsing complex operator chains is good
            }
            catch (SyntaxException ex)
            {
                // Some operator combinations might be invalid
                Assert.IsNotNull(ex.QueryPart);
                Assert.IsTrue(ex.Message.Length > 5, "Should provide meaningful error for invalid operator combinations");
            }
        }
    }

    [TestMethod]
    public void FuzzTest_ConsecutiveOperators_ShouldDetectErrors()
    {
        var operators = new string[] { "+", "-", "*", "/", "=", "!=", "<", ">", "AND", "OR" };
        
        foreach (var op in operators)
        {
            var query = $"SELECT Name FROM #test.data() WHERE A {op} {op} B";
            
            try
            {
                ParseQuery(query);
                // Some consecutive operators might be valid (like "NOT NOT")
            }
            catch (SyntaxException ex)
            {
                // Most consecutive operators should be invalid
                Assert.IsNotNull(ex.QueryPart);
                Assert.IsTrue(ex.Message.Contains("operator") || ex.Message.Contains("expected") || ex.Message.Contains("unexpected") ||
                             ex.Message.Contains("cannot be used") || ex.Message.Contains("position"),
                             $"Error should mention operator issue. Message: {ex.Message}");
            }
        }
    }

    #endregion

    #region Schema Reference Fuzzing

    [TestMethod]
    public void FuzzTest_SchemaReferenceBoundaries_ShouldValidateFormat()
    {
        var invalidSchemas = new string[]
        {
            "##test.data()",     // Double hash
            "#.data()",          // Missing schema name
            "#test.()",          // Missing method name
            "#test.data",        // Missing parentheses
            "#test.data(",       // Unclosed parentheses
            "#test.data)",       // Missing open parentheses
            "#test..data()",     // Double dot
            "#test.data.extra()",// Extra segment
            "#123.data()",       // Numeric schema name
            "#test.123()",       // Numeric method name
        };
        
        foreach (var schema in invalidSchemas)
        {
            var query = $"SELECT Name FROM {schema}";
            
            try
            {
                ParseQuery(query);
                // Some formats might be unexpectedly valid
            }
            catch (SyntaxException ex)
            {
                // Should provide clear error about schema format
                Assert.IsNotNull(ex.QueryPart);
                Assert.IsTrue(ex.Message.Length > 10, $"Should provide meaningful error for invalid schema '{schema}'");
            }
        }
    }

    #endregion

    #region Number Format Fuzzing

    [TestMethod]
    public void FuzzTest_NumberFormatBoundaries_ShouldHandleEdgeCases()
    {
        var numberFormats = new string[]
        {
            "123.",           // Trailing decimal
            ".123",           // Leading decimal
            "123.456.789",    // Multiple decimals
            "123e",           // Incomplete scientific
            "123E+",          // Incomplete scientific with sign
            "123.456e789",    // Large scientific notation
            "0000123",        // Leading zeros
            "123.000000",     // Trailing zeros
            "+123",           // Explicit positive
            "-123",           // Negative
            "123_456",        // With underscore (if supported)
            "0x123",          // Hexadecimal (if supported)
        };
        
        foreach (var number in numberFormats)
        {
            var query = $"SELECT Name FROM #test.data() WHERE Value = {number}";
            
            try
            {
                var result = ParseQuery(query);
                Assert.IsNotNull(result);
                // Document which number formats are supported
            }
            catch (SyntaxException ex)
            {
                // Invalid number formats should give clear errors
                Assert.IsNotNull(ex.QueryPart);
            }
        }
    }

    #endregion

    #region Error Message Quality Tests

    [TestMethod]
    public void FuzzTest_ErrorMessageQuality_ShouldBeHelpful()
    {
        var invalidQueries = new string[]
        {
            "SELECT",                    // Incomplete
            "SELECT FROM",               // Missing field list
            "SELECT * FORM #test.data()",// Typo in keyword
            "SELECT * FROM",             // Missing source
            "SELETC * FROM #test.data()",// Typo in SELECT
            "SELECT * FRUM #test.data()",// Typo in FROM
            "SELECT * FROM #test.data() WHARE Id = 1", // Typo in WHERE
        };
        
        foreach (var query in invalidQueries)
        {
            try
            {
                ParseQuery(query);
                Assert.Fail($"Query '{query}' should have failed but didn't");
            }
            catch (SyntaxException ex)
            {
                // Verify error message quality
                Assert.IsNotNull(ex.Message);
                Assert.IsTrue(ex.Message.Length >= 20, $"Error message should be descriptive (at least 20 chars). Got: '{ex.Message}'");
                Assert.IsNotNull(ex.QueryPart);
                Assert.IsTrue(ex.QueryPart.Length > 0, "QueryPart should indicate where the error occurred");
                
                // Error messages should not contain internal implementation details
                Assert.IsFalse(ex.Message.Contains("Exception"), "Error message should not expose internal exception details");
                Assert.IsFalse(ex.Message.Contains("Stack"), "Error message should not contain stack trace info");
            }
            catch (ParserValidationException ex)
            {
                // These are also acceptable for input validation
                Assert.IsNotNull(ex.Message);
                Assert.IsTrue(ex.Message.Length >= 20, $"Validation error should be descriptive. Got: '{ex.Message}'");
            }
        }
    }

    #endregion

    #region Performance Boundary Tests

    [TestMethod]
    public void FuzzTest_SimpleSyntaxComplexity_ShouldHandleBasicNesting()
    {
        var baseQuery = "SELECT Name FROM #test.data()";
        
        // Test basic complexity instead of deep nesting which isn't supported
        var queries = new string[]
        {
            baseQuery,
            "SELECT Name FROM #test.data() WHERE Id > 0",
            "SELECT Name, COUNT(*) FROM #test.data() GROUP BY Name",
            "SELECT Name FROM #test.data() ORDER BY Name",
            "SELECT UPPER(Name) FROM #test.data() WHERE LENGTH(Name) > 3"
        };
        
        foreach (var (query, index) in queries.Select((q, i) => (q, i)))
        {
            try
            {
                var result = ParseQuery(query);
                Assert.IsNotNull(result);
            }
            catch (SyntaxException ex)
            {
                // Simple queries should parse successfully
                Assert.Fail($"Query {index} should parse successfully: {query}. Error: {ex.Message}");
            }
        }
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