using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Helpers;
using System.Linq;

namespace Musoq.Parser.Tests;

/// <summary>
/// Tests for string similarity helper used in keyword suggestions.
/// </summary>
[TestClass]
public class StringSimilarityTests
{
    [TestMethod]
    public void LevenshteinDistance_IdenticalStrings_ShouldReturnZero()
    {
        var distance = StringSimilarity.LevenshteinDistance("select", "select");
        Assert.AreEqual(0, distance);
    }

    [TestMethod]
    public void LevenshteinDistance_OneCharDifference_ShouldReturnOne()
    {
        var distance = StringSimilarity.LevenshteinDistance("select", "selact");
        Assert.AreEqual(1, distance);
    }

    [TestMethod]
    public void LevenshteinDistance_Insertion_ShouldCalculateCorrectly()
    {
        var distance = StringSimilarity.LevenshteinDistance("select", "seelct");
        Assert.AreEqual(2, distance, "'select' to 'seelct' requires 2 edits (insert 'e' after position 1, delete 'e' at position 3)");
    }

    [TestMethod]
    public void LevenshteinDistance_Deletion_ShouldCalculateCorrectly()
    {
        var distance = StringSimilarity.LevenshteinDistance("select", "selct");
        Assert.AreEqual(1, distance, "Removing one 'e' should be distance of 1");
    }

    [TestMethod]
    public void LevenshteinDistance_Substitution_ShouldCalculateCorrectly()
    {
        var distance = StringSimilarity.LevenshteinDistance("select", "salect");
        Assert.AreEqual(1, distance, "Changing 'e' to 'a' should be distance of 1");
    }

    [TestMethod]
    public void LevenshteinDistance_MultipleEdits_ShouldCalculateCorrectly()
    {
        var distance = StringSimilarity.LevenshteinDistance("select", "salact");
        Assert.AreEqual(2, distance, "Two substitutions should be distance of 2");
    }

    [TestMethod]
    public void LevenshteinDistance_EmptyStrings_ShouldReturnZero()
    {
        var distance = StringSimilarity.LevenshteinDistance("", "");
        Assert.AreEqual(0, distance);
    }

    [TestMethod]
    public void LevenshteinDistance_OneEmptyString_ShouldReturnLength()
    {
        var distance = StringSimilarity.LevenshteinDistance("select", "");
        Assert.AreEqual(6, distance);

        distance = StringSimilarity.LevenshteinDistance("", "select");
        Assert.AreEqual(6, distance);
    }

    [TestMethod]
    public void FindClosestMatches_ExactMatch_ShouldReturnExactMatch()
    {
        var keywords = new[] { "select", "from", "where" };
        var matches = StringSimilarity.FindClosestMatches("select", keywords);

        Assert.AreEqual(1, matches.Count);
        Assert.AreEqual("select", matches[0]);
    }

    [TestMethod]
    public void FindClosestMatches_CloseMatch_ShouldReturnBestMatch()
    {
        var keywords = new[] { "select", "from", "where", "insert", "delete" };
        var matches = StringSimilarity.FindClosestMatches("seelct", keywords);

        Assert.IsTrue(matches.Count > 0);
        Assert.AreEqual("select", matches[0], "Should suggest 'select' for typo 'seelct'");
    }

    [TestMethod]
    public void FindClosestMatches_MultipleCloseMatches_ShouldReturnTopThree()
    {
        var keywords = new[] { "select", "delete", "reject", "detect", "inspect" };
        var matches = StringSimilarity.FindClosestMatches("selact", keywords, maxDistance: 3);

        Assert.IsTrue(matches.Count <= 3, "Should return at most 3 suggestions");
        Assert.IsTrue(matches.Contains("select"), "Should include 'select' as it's very close");
    }

    [TestMethod]
    public void FindClosestMatches_NoCloseMatches_ShouldReturnEmpty()
    {
        var keywords = new[] { "select", "from", "where" };
        var matches = StringSimilarity.FindClosestMatches("xyzabc", keywords, maxDistance: 2);

        Assert.AreEqual(0, matches.Count, "Should return no matches for completely different word");
    }

    [TestMethod]
    public void FindClosestMatches_CaseInsensitive_ShouldMatch()
    {
        var keywords = new[] { "SELECT", "FROM", "WHERE" };
        var matches = StringSimilarity.FindClosestMatches("seelct", keywords);

        Assert.IsTrue(matches.Count > 0);
        Assert.AreEqual("SELECT", matches[0], "Should match regardless of case");
    }

    [TestMethod]
    public void FindClosestMatches_EmptyInput_ShouldReturnEmpty()
    {
        var keywords = new[] { "select", "from", "where" };
        var matches = StringSimilarity.FindClosestMatches("", keywords);

        Assert.AreEqual(0, matches.Count);
    }

    [TestMethod]
    public void FindClosestMatches_NullInput_ShouldReturnEmpty()
    {
        var keywords = new[] { "select", "from", "where" };
        var matches = StringSimilarity.FindClosestMatches(null, keywords);

        Assert.AreEqual(0, matches.Count);
    }

    [TestMethod]
    public void FindClosestMatches_NullCandidates_ShouldReturnEmpty()
    {
        var matches = StringSimilarity.FindClosestMatches("select", null);

        Assert.AreEqual(0, matches.Count);
    }

    [TestMethod]
    public void FindClosestMatches_OrderedByDistance_ShouldReturnBestFirst()
    {
        var keywords = new[] { "select", "delete", "reject", "collect", "elect" };
        var matches = StringSimilarity.FindClosestMatches("selct", keywords);

        Assert.IsTrue(matches.Count > 0);
        // "select" should be first as it's closest (distance 1)
        Assert.AreEqual("select", matches[0]);
    }

    [TestMethod]
    public void FindClosestMatches_RespectMaxDistance_ShouldFilterByThreshold()
    {
        var keywords = new[] { "select", "from", "where" };
        var matches = StringSimilarity.FindClosestMatches("xyz", keywords, maxDistance: 1);

        // "xyz" is too different from any keyword with max distance 1
        Assert.AreEqual(0, matches.Count);
    }

    [TestMethod]
    public void FindClosestMatches_CommonTypos_ShouldSuggestCorrectKeyword()
    {
        var keywords = new[] { "select", "from", "where", "group by", "order by" };

        // Test common typos
        var testCases = new[]
        {
            ("seelct", "select"),
            ("selct", "select"),
            ("froom", "from"),
            ("frm", "from"),
            ("whre", "where"),
            ("wher", "where")
        };

        foreach (var (typo, expected) in testCases)
        {
            var matches = StringSimilarity.FindClosestMatches(typo, keywords);
            Assert.IsTrue(matches.Count > 0, $"No matches for typo '{typo}'");
            Assert.AreEqual(expected, matches[0], $"Expected '{expected}' for typo '{typo}', got '{matches[0]}'");
        }
    }
}
