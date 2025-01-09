using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Helpers;

namespace Musoq.Parser.Tests;

[TestClass]
public class EscapeHelpersTests
{
    [TestMethod]
    public void Unescape_EmptyString_ReturnsEmptyString()
    {
        Assert.AreEqual(string.Empty, string.Empty.Unescape());
    }

    [TestMethod]
    public void Unescape_NullString_ReturnsNull()
    {
        string input = null;
        Assert.IsNull(input.Unescape());
    }

    [TestMethod]
    public void Unescape_NoBackslashes_ReturnsSameString()
    {
        const string input = "simple text";
        Assert.AreEqual(input, input.Unescape());
    }

    [TestMethod]
    [DataRow(@"\\a", @"\a")]
    [DataRow(@"\\b\\c", @"\b\c")]
    [DataRow(@"\\text", @"\text")]
    [DataRow(@"prefix\\text", @"prefix\text")]
    [DataRow(@"\\text\\suffix", @"\text\suffix")]
    public void Unescape_SingleBackslash_RemovesBackslash(string input, string expected)
    {
        Assert.AreEqual(expected, input.Unescape());
    }

    [TestMethod]
    [DataRow("\\\\a", "\\a")]
    [DataRow("\\\\b\\\\c", "\\b\\c")]
    [DataRow("\\\\text", "\\text")]
    [DataRow("prefix\\\\text", "prefix\\text")]
    public void Unescape_DoubleBackslash_BecomesSingle(string input, string expected)
    {
        Assert.AreEqual(expected, input.Unescape());
    }

    [TestMethod]
    [DataRow(@"\\\\\\a", @"\\\a")]
    [DataRow(@"\\\\\\\\a", @"\\\\a")]
    [DataRow(@"\\\\\\\\\\a", @"\\\\\a")]
    [DataRow(@"\\\\\\\\\\\\a", @"\\\\\\a")]
    public void Unescape_MultipleBackslashes_HandledCorrectly(string input, string expected)
    {
        Assert.AreEqual(expected, input.Unescape());
    }

    [TestMethod]
    [DataRow(@"\n", "\n", "Newline")]
    [DataRow(@"\r", "\r", "Carriage return")]
    [DataRow(@"\t", "\t", "Tab")]
    [DataRow(@"\b", "\b", "Backspace")]
    [DataRow(@"\f", "\f", "Form feed")]
    [DataRow(@"\0", "\0", "Null character")]
    public void Unescape_ControlCharacters_ReturnsUnescaped(string input, string expected, string message)
    {
        Assert.AreEqual(expected, input.Unescape(), message);
    }

    [TestMethod]
    [DataRow("\\\\n", "\\n")]
    [DataRow("\\\\t", "\\t")]
    [DataRow("\\\\r", "\\r")]
    [DataRow("\\\\b", "\\b")]
    [DataRow("\\\\f", "\\f")]
    public void Unescape_EscapedControlCharacters_PreservesBackslash(string input, string expected)
    {
        Assert.AreEqual(expected, input.Unescape());
    }

    [TestMethod]
    public void Unescape_MixedEscapes_HandledCorrectly()
    {
        Assert.AreEqual("a\nb\\nc\\\nd", @"a\nb\\nc\\\nd".Unescape());
        Assert.AreEqual(@"\text\\text\\\text", @"\\text\\\\text\\\\\\text".Unescape());
        Assert.AreEqual("a\\b\tc\\d", "a\\\\b\\tc\\\\d".Unescape());
    }

    [TestMethod]
    public void Unescape_EscapeCharacterSequences_HandledCorrectly()
    {
        Assert.AreEqual("\u001B", "\\e".Unescape());
        Assert.AreEqual(@"\e", @"\\e".Unescape());
        Assert.AreEqual(@"\", @"\\\e".Unescape());
    }

    [TestMethod]
    public void Unescape_QuoteEscaping_HandledCorrectly()
    {
        Assert.AreEqual("'", "\\'".Unescape());
        Assert.AreEqual("\\'", "\\\\'".Unescape());
        Assert.AreEqual("\\'", "\\\\\\\'".Unescape());
    }

    [TestMethod]
    [DataRow(@"text\\", @"text\")]
    [DataRow(@"text\\\\", @"text\\")]
    [DataRow(@"text\\\\\\", @"text\\\")]
    [DataRow(@"text\\\\\\\\", @"text\\\\")]
    public void Unescape_BackslashesAtEnd_HandledCorrectly(string input, string expected)
    {
        Assert.AreEqual(expected, input.Unescape());
    }

    [TestMethod]
    public void Unescape_LargeString_SimpleEscapes()
    {
        // Arrange
        var input = new StringBuilder();
        const string pattern = "text\\simple\\pattern";
        for (int i = 0; i < 200000; i++)
        {
            input.Append(pattern);
        }

        // Act
        string result = input.ToString().Unescape();

        // Assert
        Assert.IsTrue(result.Length > 0);
        Assert.IsFalse(result.Contains(@"\\simple"));
    }

    [TestMethod]
    public void Unescape_LargeString_ComplexEscapes()
    {
        // Arrange
        var input = new StringBuilder();
        const string pattern = "text\\n\\\\text\\\\\\pattern\\t";
        for (int i = 0; i < 200000; i++)
        {
            input.Append(pattern);
        }

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        string result = input.ToString().Unescape();
        sw.Stop();

        // Assert
        Assert.IsTrue(result.Length > 0);
        Assert.IsTrue(result.Contains("\n"));
        Assert.IsTrue(result.Contains("\\text"));
        Assert.IsTrue(result.Contains("\t"));
        Assert.IsTrue(sw.ElapsedMilliseconds < 100, 
            $"Processing took too long: {sw.ElapsedMilliseconds}ms");
    }

    [TestMethod]
    public void Unescape_MemoryEfficiency()
    {
        // Arrange
        const int iterationCount = 10000;
        string input = "test\\complex\\\\pattern\\n\\\\\\text";
        long initialMemory = GC.GetTotalMemory(true);

        // Act
        for (int i = 0; i < iterationCount; i++)
        {
            _ = input.Unescape();
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        long finalMemory = GC.GetTotalMemory(true);

        // Assert
        long memoryDifference = finalMemory - initialMemory;
        Assert.IsTrue(memoryDifference < 1024 * 1024, 
            $"Memory usage grew by {memoryDifference} bytes");
    }
    
    [TestMethod]
    public void Unescape_NullAndEmpty()
    {
        Assert.IsNull(((string)null).Unescape());
        Assert.AreEqual(string.Empty, string.Empty.Unescape());
    }

    [TestMethod]
    public void Unescape_NoEscapeSequences()
    {
        Assert.AreEqual("simple text", "simple text".Unescape());
        Assert.AreEqual("12345", "12345".Unescape());
        Assert.AreEqual("!@#$%", "!@#$%".Unescape());
    }

    [TestMethod]
    [DataRow(@"\", @"\")] // Single backslash
    [DataRow(@"\\", @"\")] // Double backslash
    [DataRow(@"\\\", @"\\")] // Triple backslash
    [DataRow(@"\\\\", @"\\")] // Four backslashes
    [DataRow(@"text\", @"text\")] 
    [DataRow(@"text\\", @"text\")]
    [DataRow(@"text\\\", @"text\\")]
    [DataRow(@"text\\\\", @"text\\")]
    [DataRow(@"\text", "\text")]
    [DataRow(@"\\text", @"\text")]
    [DataRow(@"\\\text", "\\\text")]
    public void Unescape_BackslashSequences(string input, string expected)
    {
        Assert.AreEqual(expected, input.Unescape());
    }

    [TestMethod]
    [DataRow(@"\n", "\n")]
    [DataRow(@"\\n", @"\n")]
    [DataRow(@"\\\n", "\\\n")]
    [DataRow(@"\\\\n", @"\\n")]
    [DataRow(@"text\ntext", "text\ntext")]
    [DataRow(@"text\\ntext", @"text\ntext")]
    public void Unescape_NewlineSequences(string input, string expected)
    {
        Assert.AreEqual(expected, input.Unescape());
    }

    [TestMethod]
    [DataRow(@"\t", "\t")]
    [DataRow(@"\\t", @"\t")]
    [DataRow(@"\\\t", "\\\t")]
    [DataRow(@"\\\\t", @"\\t")]
    [DataRow(@"text\ttext", "text\ttext")]
    [DataRow(@"text\\ttext", @"text\ttext")]
    public void Unescape_TabSequences(string input, string expected)
    {
        Assert.AreEqual(expected, input.Unescape());
    }

    [TestMethod]
    [DataRow(@"\'", "'")]
    [DataRow(@"\\\'", @"\'")]
    [DataRow(@"\\\\'", @"\\'")] 
    [DataRow(@"\""", "\"")]
    [DataRow(@"\\""", @"\""")]
    [DataRow(@"\\\\""", @"\\""")]
    public void Unescape_QuoteSequences(string input, string expected)
    {
        Assert.AreEqual(expected, input.Unescape());
    }

    [TestMethod]
    [DataRow(@"\u0041", "A")] // Basic ASCII
    [DataRow(@"\\u0041", @"\u0041")] // Escaped unicode sequence
    [DataRow(@"\u00A9", "©")] // Copyright symbol
    [DataRow(@"\\u00A9", @"\u00A9")]
    [DataRow(@"\u0394", "Δ")] // Greek Delta
    [DataRow(@"\\u0394", @"\u0394")]
    [DataRow(@"\u2665", "♥")] // Heart symbol
    [DataRow(@"\\u2665", @"\u2665")]
    public void Unescape_UnicodeSequences(string input, string expected)
    {
        Assert.AreEqual(expected, input.Unescape());
    }

    [TestMethod]
    [DataRow(@"\x41", "A")] // Basic ASCII
    [DataRow(@"\\x41", @"\x41")] // Escaped hex sequence
    [DataRow(@"\x7E", "~")] // Tilde
    [DataRow(@"\\x7E", @"\x7E")]
    public void Unescape_HexSequences(string input, string expected)
    {
        Assert.AreEqual(expected, input.Unescape());
    }

    [TestMethod]
    public void Unescape_MixedSequences()
    {
        Assert.AreEqual("Hello\nWorld\tΔ♥", @"Hello\nWorld\t\u0394\u2665".Unescape());
        Assert.AreEqual(@"Hello\nWorld\tΔ♥", @"Hello\\nWorld\\t\u0394\u2665".Unescape());
        Assert.AreEqual("'\"Hello\n\tWorld'", @"\'\""Hello\n\tWorld\'".Unescape());
    }

    [TestMethod]
    public void Unescape_SpecialCharacters()
    {
        Assert.AreEqual("\0", @"\0".Unescape()); // Null character
        Assert.AreEqual("\b", @"\b".Unescape()); // Backspace
        Assert.AreEqual("\f", @"\f".Unescape()); // Form feed
        Assert.AreEqual("\u001B", @"\e".Unescape()); // Escape character
    }

    [TestMethod]
    [DataRow(@"\z", @"\z")] // Unknown escape sequence
    [DataRow(@"\\z", @"\z")] // Escaped unknown sequence
    [DataRow(@"\-", @"\-")]
    [DataRow(@"\\-", @"\-")]
    public void Unescape_UnknownSequences(string input, string expected)
    {
        Assert.AreEqual(expected, input.Unescape());
    }

    [TestMethod]
    public void Unescape_InvalidUnicodeSequences()
    {
        Assert.AreEqual("\\uZZZZ", @"\uZZZZ".Unescape()); // Invalid unicode
        Assert.AreEqual("\\xZZ", @"\xZZ".Unescape()); // Invalid hex
        Assert.AreEqual("\\u123", @"\u123".Unescape()); // Incomplete unicode
        Assert.AreEqual("\\x1", @"\x1".Unescape()); // Incomplete hex
    }

    [TestMethod]
    public void Unescape_LargeString()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < 1000; i++)
        {
            sb.Append(@"Hello\nWorld\t\u0394\u2665\\test");
        }
        var result = sb.ToString().Unescape();
        Assert.IsTrue(result.Contains("\n"));
        Assert.IsTrue(result.Contains("\t"));
        Assert.IsTrue(result.Contains("Δ"));
        Assert.IsTrue(result.Contains("♥"));
        Assert.IsTrue(result.Contains(@"\test"));
    }
}