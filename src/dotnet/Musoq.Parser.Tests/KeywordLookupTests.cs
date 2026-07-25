using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class KeywordLookupTests
{
    [TestMethod]
    public void SqlKeyword_StringAndSpanLookups_ShouldAgreeAcrossCasing()
    {
        foreach (var (text, expectedType) in KeywordCollisionCatalog.SqlKeywords)
        {
            AssertLookupParity(text, expectedType, KeywordLookup.TryGetKeyword, KeywordLookup.TryGetKeyword, $"SQL keyword '{text}'");
            AssertLookupParity(
                text.ToUpperInvariant(),
                expectedType,
                KeywordLookup.TryGetKeyword,
                KeywordLookup.TryGetKeyword,
                $"SQL keyword '{text}' uppercase");
            AssertLookupParity(
                ToMixedCase(text),
                expectedType,
                KeywordLookup.TryGetKeyword,
                KeywordLookup.TryGetKeyword,
                $"SQL keyword '{text}' mixed case");
        }
    }

    [TestMethod]
    public void SchemaKeyword_StringAndSpanLookups_ShouldAgreeAcrossCasing()
    {
        foreach (var (text, expectedType) in KeywordCollisionCatalog.SchemaKeywords)
        {
            AssertLookupParity(text, expectedType, KeywordLookup.TryGetSchemaKeyword, KeywordLookup.TryGetSchemaKeyword, $"schema keyword '{text}'");
            AssertLookupParity(
                text.ToUpperInvariant(),
                expectedType,
                KeywordLookup.TryGetSchemaKeyword,
                KeywordLookup.TryGetSchemaKeyword,
                $"schema keyword '{text}' uppercase");
            AssertLookupParity(
                ToMixedCase(text),
                expectedType,
                KeywordLookup.TryGetSchemaKeyword,
                KeywordLookup.TryGetSchemaKeyword,
                $"schema keyword '{text}' mixed case");

            Assert.AreEqual(expectedType, KeywordLookup.GetSchemaKeywordType(text),
                $"schema keyword '{text}' string get mismatch.");
            Assert.AreEqual(expectedType, KeywordLookup.GetSchemaKeywordType(text.ToUpperInvariant()),
                $"schema keyword '{text}' uppercase string get mismatch.");

            Assert.IsTrue(KeywordLookup.IsSchemaKeyword(text), $"Schema membership missing '{text}'.");
            Assert.IsTrue(KeywordLookup.IsSchemaKeyword(text.ToUpperInvariant()),
                $"Schema membership is not case-insensitive for '{text}'.");
        }
    }

    [TestMethod]
    [DataRow("column_name")]
    [DataRow("bit")]
    [DataRow("term")]
    [DataRow("utf16")]
    public void UnknownKeyword_ShouldReturnWordFromEveryLookup(string text)
    {
        Assert.IsFalse(KeywordLookup.TryGetKeyword(text, out var sqlStringType));
        Assert.AreEqual(TokenType.Word, sqlStringType);
        Assert.IsFalse(KeywordLookup.TryGetKeyword(text.AsSpan(), out var sqlSpanType));
        Assert.AreEqual(TokenType.Word, sqlSpanType);

        Assert.IsFalse(KeywordLookup.TryGetSchemaKeyword(text, out var schemaStringType));
        Assert.AreEqual(TokenType.Word, schemaStringType);
        Assert.IsFalse(KeywordLookup.TryGetSchemaKeyword(text.AsSpan(), out var schemaSpanType));
        Assert.AreEqual(TokenType.Word, schemaSpanType);
        Assert.AreEqual(TokenType.Word, KeywordLookup.GetSchemaKeywordType(text));
        Assert.IsFalse(KeywordLookup.IsSchemaKeyword(text));
    }

    private static void AssertLookupParity(
        string text,
        TokenType expectedType,
        KeywordLookupStringLookup stringLookup,
        KeywordLookupSpanLookup spanLookup,
        string description)
    {
        Assert.IsTrue(stringLookup(text, out var stringType), $"{description} string lookup failed.");
        Assert.AreEqual(expectedType, stringType, $"{description} string token type mismatch.");

        Assert.IsTrue(spanLookup(text.AsSpan(), out var spanType), $"{description} span lookup failed.");
        Assert.AreEqual(expectedType, spanType, $"{description} span token type mismatch.");
    }

    private static string ToMixedCase(string text)
    {
        var chars = text.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
            chars[i] = i % 2 == 0 ? char.ToUpperInvariant(chars[i]) : chars[i];

        return new string(chars);
    }

    private delegate bool KeywordLookupStringLookup(string text, out TokenType tokenType);
    private delegate bool KeywordLookupSpanLookup(ReadOnlySpan<char> text, out TokenType tokenType);
}
