using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;

namespace Musoq.Parser.Tests;

[TestClass]
public class UnknownTokenExceptionTests
{
    [TestMethod]
    public void Constructor_WithAllParameters_ShouldSetProperties()
    {
        // Arrange
        var position = 10;
        var unknownChar = '`';
        var remainingQuery = "unknown_table FROM";
        var surroundingContext = "SELECT * FROM `unknown_table";

        // Act
        var exception = new UnknownTokenException(position, unknownChar, remainingQuery, surroundingContext);

        // Assert
        Assert.AreEqual(position, exception.Position);
        Assert.AreEqual(unknownChar, exception.UnknownCharacter);
        Assert.AreEqual(remainingQuery, exception.RemainingQuery);
        Assert.AreEqual(surroundingContext, exception.SurroundingContext);
    }

    [TestMethod]
    public void Constructor_ShouldGenerateAppropriateMessage()
    {
        // Arrange
        var position = 15;
        var unknownChar = '`';
        var remainingQuery = "table_name FROM users";
        var surroundingContext = "SELECT * FROM `table_name";

        // Act
        var exception = new UnknownTokenException(position, unknownChar, remainingQuery, surroundingContext);

        // Assert
        Assert.IsTrue(exception.Message.Contains($"Unrecognized character '{unknownChar}'"));
        Assert.IsTrue(exception.Message.Contains($"at position {position}"));
        Assert.IsTrue(exception.Message.Contains($"Near: '{surroundingContext}'"));
        Assert.IsTrue(exception.Message.Contains("Remaining query:"));
        Assert.IsTrue(exception.Message.Contains("Please check your SQL syntax"));
    }

    [TestMethod]
    public void Constructor_WithBacktick_ShouldProvideSuggestions()
    {
        // Arrange
        var position = 10;
        var unknownChar = '`';
        var remainingQuery = "table";

        // Act
        var exception = new UnknownTokenException(position, unknownChar, remainingQuery);

        // Assert
        Assert.IsTrue(exception.Message.Contains("Did you mean:"));
        Assert.IsTrue(exception.Message.Contains("Use double quotes"));
        Assert.IsTrue(exception.Message.Contains("Use single quotes"));
    }

    [TestMethod]
    public void Constructor_WithBrackets_ShouldProvideSuggestions()
    {
        // Arrange
        var position = 10;
        var unknownChar = '[';
        var remainingQuery = "table]";

        // Act
        var exception = new UnknownTokenException(position, unknownChar, remainingQuery);

        // Assert
        Assert.IsTrue(exception.Message.Contains("Did you mean:"));
        Assert.IsTrue(exception.Message.Contains("Use double quotes"));
        Assert.IsTrue(exception.Message.Contains("instead of brackets"));
    }

    [TestMethod]
    public void Constructor_WithCurlyBraces_ShouldProvideSuggestions()
    {
        // Arrange
        var position = 10;
        var unknownChar = '{';
        var remainingQuery = "expression}";

        // Act
        var exception = new UnknownTokenException(position, unknownChar, remainingQuery);

        // Assert
        Assert.IsTrue(exception.Message.Contains("Did you mean:"));
        Assert.IsTrue(exception.Message.Contains("Use parentheses"));
        Assert.IsTrue(exception.Message.Contains("for grouping"));
    }

    [TestMethod]
    public void Constructor_WithSemicolon_ShouldProvideSuggestions()
    {
        // Arrange
        var position = 25;
        var unknownChar = ';';
        var remainingQuery = "";

        // Act
        var exception = new UnknownTokenException(position, unknownChar, remainingQuery);

        // Assert
        Assert.IsTrue(exception.Message.Contains("Did you mean:"));
        Assert.IsTrue(exception.Message.Contains("Semicolon is not required"));
    }

    [TestMethod]
    public void Constructor_WithBackslash_ShouldProvideSuggestions()
    {
        // Arrange
        var position = 15;
        var unknownChar = '\\';
        var remainingQuery = " 2";

        // Act
        var exception = new UnknownTokenException(position, unknownChar, remainingQuery);

        // Assert
        Assert.IsTrue(exception.Message.Contains("Did you mean:"));
        Assert.IsTrue(exception.Message.Contains("Use forward slash"));
        Assert.IsTrue(exception.Message.Contains("for division"));
    }

    [TestMethod]
    public void Constructor_WithQuestionMark_ShouldProvideSuggestions()
    {
        // Arrange
        var position = 20;
        var unknownChar = '?';
        var remainingQuery = "param";

        // Act
        var exception = new UnknownTokenException(position, unknownChar, remainingQuery);

        // Assert
        Assert.IsTrue(exception.Message.Contains("Did you mean:"));
        Assert.IsTrue(exception.Message.Contains("Use parameters with @ or # prefix"));
    }

    [TestMethod]
    public void Constructor_WithUnknownCharacter_ShouldNotProvideSuggestions()
    {
        // Arrange
        var position = 10;
        var unknownChar = '@'; // Not in suggestions list
        var remainingQuery = "param";

        // Act
        var exception = new UnknownTokenException(position, unknownChar, remainingQuery);

        // Assert
        Assert.IsFalse(exception.Message.Contains("Did you mean:"));
    }

    [TestMethod]
    public void Constructor_WithLongRemainingQuery_ShouldTruncate()
    {
        // Arrange
        var position = 10;
        var unknownChar = '`';
        var remainingQuery = "this_is_a_very_long_query_that_should_be_truncated_because_it_exceeds_fifty_characters";

        // Act
        var exception = new UnknownTokenException(position, unknownChar, remainingQuery);

        // Assert
        Assert.IsTrue(exception.Message.Contains("..."));
        Assert.IsTrue(exception.Message.Contains("this_is_a_very_long_query_that_should_be_truncated"));
    }

    [TestMethod]
    public void Constructor_WithShortRemainingQuery_ShouldNotTruncate()
    {
        // Arrange
        var position = 10;
        var unknownChar = '`';
        var remainingQuery = "short";

        // Act
        var exception = new UnknownTokenException(position, unknownChar, remainingQuery);

        // Assert
        Assert.IsFalse(exception.Message.Contains("..."));
        Assert.IsTrue(exception.Message.Contains("short"));
    }

    [TestMethod]
    public void ForInvalidCharacter_ShouldCreateAppropriateException()
    {
        // Arrange
        var position = 14; // Position of the backtick in "SELECT * FROM `table_name` WHERE..."
        var character = '`';
        var fullQuery = "SELECT * FROM `table_name` WHERE condition = 'value'";

        // Act
        var exception = UnknownTokenException.ForInvalidCharacter(position, character, fullQuery);

        // Assert
        Assert.AreEqual(position, exception.Position);
        Assert.AreEqual(character, exception.UnknownCharacter);
        Assert.IsTrue(exception.RemainingQuery.StartsWith("`table_name"), $"Expected to start with '`table_name' but was '{exception.RemainingQuery}'");
        Assert.IsTrue(exception.SurroundingContext.Contains("FROM"), $"Expected to contain 'FROM' but was '{exception.SurroundingContext}'");
        Assert.IsTrue(exception.SurroundingContext.Contains("`table_nam"), $"Expected to contain '`table_nam' but was '{exception.SurroundingContext}'");
    }

    [TestMethod]
    public void ForInvalidCharacter_WithPositionNearStart_ShouldHandleEdgeCase()
    {
        // Arrange
        var position = 2;
        var character = '`';
        var fullQuery = "SE`LECT * FROM table";

        // Act
        var exception = UnknownTokenException.ForInvalidCharacter(position, character, fullQuery);

        // Assert
        Assert.AreEqual(position, exception.Position);
        Assert.AreEqual(character, exception.UnknownCharacter);
        Assert.IsTrue(exception.RemainingQuery.StartsWith("`LECT"));
        Assert.IsTrue(exception.SurroundingContext.StartsWith("SE"));
    }

    [TestMethod]
    public void ForInvalidCharacter_WithPositionNearEnd_ShouldHandleEdgeCase()
    {
        // Arrange
        var position = 19; // Position of the backtick in "SELECT * FROM table`"
        var character = '`';
        var fullQuery = "SELECT * FROM table`";

        // Act
        var exception = UnknownTokenException.ForInvalidCharacter(position, character, fullQuery);

        // Assert
        Assert.AreEqual(position, exception.Position);
        Assert.AreEqual(character, exception.UnknownCharacter);
        Assert.IsTrue(exception.RemainingQuery.StartsWith("`"));
        Assert.IsTrue(exception.SurroundingContext.Contains("FROM table`"));
    }

    [TestMethod]
    public void Constructor_WithNullParameters_ShouldHandleGracefully()
    {
        // Arrange
        var position = 10;
        var unknownChar = '`';

        // Act
        var exception = new UnknownTokenException(position, unknownChar, null, null);

        // Assert
        Assert.AreEqual(string.Empty, exception.RemainingQuery);
        Assert.AreEqual(string.Empty, exception.SurroundingContext);
        Assert.IsTrue(exception.Message.Contains($"'{unknownChar}'"));
        Assert.IsTrue(exception.Message.Contains($"position {position}"));
    }
}