using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Interpreters;

namespace Musoq.Schema.Tests;

/// <summary>
///     Tests for TextInterpreterBase helper methods to improve branch coverage.
///     Uses a test-specific interpreter class that exposes protected methods.
/// </summary>
[TestClass]
public partial class TextInterpreterBaseTests
{
    #region Test Interpreter

    /// <summary>
    ///     Test interpreter that exposes protected methods for testing.
    /// </summary>
    private sealed class TestTextInterpreter : TextInterpreterBase<string>
    {
        public override string SchemaName => "TestSchema";

        public override string ParseAt(ReadOnlySpan<char> text, int position)
        {
            ParsePosition = position;
            return ReadRest(text);
        }

        // Expose protected methods for testing
        public string TestReadUntil(ReadOnlySpan<char> text, string delimiter, bool trim = false,
            bool consumeDelimiter = true)
        {
            return ReadUntil(text, delimiter, trim, consumeDelimiter);
        }

        public string TestReadBetween(ReadOnlySpan<char> text, string open, string close, bool nested = false,
            bool trim = false, bool escaped = false)
        {
            return ReadBetween(text, open, close, nested, trim, escaped);
        }

        public string TestReadChars(ReadOnlySpan<char> text, int count, bool trim = false, bool ltrim = false,
            bool rtrim = false)
        {
            return ReadChars(text, count, trim, ltrim, rtrim);
        }

        public string TestReadToken(ReadOnlySpan<char> text, bool trim = false)
        {
            return ReadToken(text, trim);
        }

        public string TestReadRest(ReadOnlySpan<char> text, bool trim = false, bool ltrim = false, bool rtrim = false)
        {
            return ReadRest(text, trim, ltrim, rtrim);
        }

        public string TestReadPattern(ReadOnlySpan<char> text, string pattern, bool trim = false)
        {
            return ReadPattern(text, pattern, trim);
        }

        public void TestSkipWhitespace(ReadOnlySpan<char> text, bool required = false)
        {
            SkipWhitespace(text, required);
        }

        public void TestSkipOptionalWhitespace(ReadOnlySpan<char> text)
        {
            SkipOptionalWhitespace(text);
        }

        public void TestExpectLiteral(ReadOnlySpan<char> text, string literal)
        {
            ExpectLiteral(text, literal);
        }

        public void TestEnsureChars(ReadOnlySpan<char> text, int count)
        {
            EnsureChars(text, count);
        }

        public bool TestIsAtEnd(ReadOnlySpan<char> text)
        {
            return IsAtEnd(text);
        }

        public bool TestLookaheadMatches(ReadOnlySpan<char> text, string expected)
        {
            return LookaheadMatches(text, expected);
        }

        public bool TestLookaheadMatchesPattern(ReadOnlySpan<char> text, string pattern)
        {
            return LookaheadMatchesPattern(text, pattern);
        }

        public void TestValidate(bool condition, string fieldName, string message)
        {
            Validate(condition, fieldName, message);
        }

        public static string TestApplyModifiers(string value, bool ltrim = false, bool rtrim = false,
            bool lower = false, bool upper = false)
        {
            return ApplyModifiers(value, ltrim, rtrim, lower, upper);
        }

        public int GetPosition()
        {
            return ParsePosition;
        }

        public void SetPosition(int pos)
        {
            ParsePosition = pos;
        }
    }

    #endregion
}
