using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class DiffTests : LibraryBaseBaseTests
{
    #region Diff String Output Tests - Basic Functionality

    [TestMethod]
    [DataRow("to jest TEXT krotki", "to jest LABA krotka", "full", "to jest [-TEXT][+LABA] krotk[-i][+a]")]
    [DataRow("abc", "abc", "full", "abc")]
    [DataRow("abc", "xyz", "full", "[-abc][+xyz]")]
    [DataRow("abc", "", "full", "[-abc]")]
    [DataRow("", "xyz", "full", "[+xyz]")]
    [DataRow("aXb", "aYb", "full", "a[-X][+Y]b")]
    public void Diff_FullMode_ReturnsExpectedOutput(string first, string second, string mode, string expected)
    {
        var result = Library.Diff(first, second, mode);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("to jest TEXT krotki", "to jest LABA krotka", "compact", "[=8][-TEXT][+LABA][=6][-i][+a]")]
    [DataRow("abc", "abc", "compact", "[=3]")]
    [DataRow("abc", "xyz", "compact", "[-abc][+xyz]")]
    [DataRow("aXb", "aYb", "compact", "[=1][-X][+Y][=1]")]
    [DataRow("hello...world", "hello...world", "compact", "[=13]")]
    public void Diff_CompactMode_ReturnsExpectedOutput(string first, string second, string mode, string expected)
    {
        var result = Library.Diff(first, second, mode);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("abcXdef", "abcYdef", "full:5", "abc[-X][+Y]def")]
    [DataRow("abcdefghijXklmnopqrst", "abcdefghijYklmnopqrst", "full:5", "[=10][-X][+Y][=10]")]
    public void Diff_FullThresholdMode_ReturnsExpectedOutput(string first, string second, string mode, string expected)
    {
        var result = Library.Diff(first, second, mode);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Diff_FullThresholdMode_MixedRegions_ReturnsExpectedOutput()
    {
        var result = Library.Diff("aaXbbbbbbbbbY", "aaZbbbbbbbbbW", "full:5");
        Assert.AreEqual("aa[-X][+Z][=9][-Y][+W]", result);
    }

    #endregion

    #region Diff String Output Tests - Null and Empty Handling

    [TestMethod]
    public void Diff_BothNull_ReturnsNull()
    {
        var result = Library.Diff(null, null);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Diff_FirstNull_ReturnsInserted()
    {
        var result = Library.Diff(null, "xyz");
        Assert.AreEqual("[+xyz]", result);
    }

    [TestMethod]
    public void Diff_SecondNull_ReturnsDeleted()
    {
        var result = Library.Diff("abc", null);
        Assert.AreEqual("[-abc]", result);
    }

    [TestMethod]
    public void Diff_BothEmpty_ReturnsEmpty()
    {
        var result = Library.Diff("", "");
        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void Diff_FirstEmpty_ReturnsInserted()
    {
        var result = Library.Diff("", "xyz");
        Assert.AreEqual("[+xyz]", result);
    }

    [TestMethod]
    public void Diff_SecondEmpty_ReturnsDeleted()
    {
        var result = Library.Diff("abc", "");
        Assert.AreEqual("[-abc]", result);
    }

    [TestMethod]
    public void Diff_NullVsEmpty_AreDifferent()
    {
        var resultNullFirst = Library.Diff(null, "test");
        Assert.AreEqual("[+test]", resultNullFirst);


        var resultEmptyFirst = Library.Diff("", "test");
        Assert.AreEqual("[+test]", resultEmptyFirst);


        var resultBothNull = Library.Diff(null, null);
        Assert.IsNull(resultBothNull);
    }

    #endregion

    #region Diff String Output Tests - Mode Validation

    [TestMethod]
    [DataRow("abc", "xyz", "banana", null)]
    [DataRow("abc", "xyz", "FULL", null)]
    [DataRow("abc", "xyz", "", null)]
    [DataRow("abc", "xyz", "full:-5", null)]
    [DataRow("abc", "xyz", "full:abc", null)]
    [DataRow("abc", "xyz", "full:", null)]
    [DataRow("abc", "xyz", "Compact", null)]
    [DataRow("abc", "xyz", "full:10:20", null)]
    [DataRow("abc", "xyz", "full:0", null)]
    public void Diff_InvalidMode_ReturnsNull(string first, string second, string mode, string? expected)
    {
        var result = Library.Diff(first, second, mode);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Diff_NullMode_UsesDefaultFull()
    {
        var result = Library.Diff("abc", "xyz", null);
        Assert.AreEqual("[-abc][+xyz]", result);
    }

    [TestMethod]
    public void Diff_NoModeSpecified_UsesDefaultFull()
    {
        var result = Library.Diff("abc", "xyz");
        Assert.AreEqual("[-abc][+xyz]", result);
    }

    [TestMethod]
    public void Diff_FullMode_Works()
    {
        var result = Library.Diff("abc", "xyz");
        Assert.AreEqual("[-abc][+xyz]", result);
    }

    [TestMethod]
    public void Diff_CompactMode_Works()
    {
        var result = Library.Diff("abc", "xyz", "compact");
        Assert.AreEqual("[-abc][+xyz]", result);
    }

    [TestMethod]
    public void Diff_FullThresholdOne_Works()
    {
        var result = Library.Diff("aXb", "aYb", "full:1");

        Assert.AreEqual("a[-X][+Y]b", result);
    }

    [TestMethod]
    public void Diff_VeryLargeThreshold_EffectivelySameAsFull()
    {
        var result = Library.Diff("abc", "xyz", "full:1000");
        Assert.AreEqual("[-abc][+xyz]", result);
    }

    #endregion

    #region Diff String Output Tests - Special Content

    [TestMethod]
    public void Diff_StringsContainingBrackets_HandlesCorrectly()
    {
        // Note: Special characters are NOT escaped per spec
        var result = Library.Diff("a[b]c", "a[d]c");
        Assert.AreEqual("a[[-b][+d]]c", result);
    }

    [TestMethod]
    public void Diff_StringsWithWhitespaceDifferences_HandlesCorrectly()
    {
        var result = Library.Diff("hello world", "hello  world");
        Assert.AreEqual("hello [+ ]world", result);
    }

    [TestMethod]
    public void Diff_StringsWithNewlines_HandlesCorrectly()
    {
        var result = Library.Diff("hello\nworld", "hello\n\nworld");
        Assert.AreEqual("hello\n[+\n]world", result);
    }

    [TestMethod]
    public void Diff_UnicodeCharacters_HandlesCorrectly()
    {
        var result = Library.Diff("héllo", "hëllo");
        Assert.AreEqual("h[-é][+ë]llo", result);
    }

    [TestMethod]
    public void Diff_VeryLongStrings_HandlesCorrectly()
    {
        var first = new string('a', 500) + "X" + new string('b', 500);
        var second = new string('a', 500) + "Y" + new string('b', 500);
        var result = Library.Diff(first, second, "compact");
        Assert.AreEqual("[=500][-X][+Y][=500]", result);
    }

    #endregion

    #region Diff String Output Tests - Single Character Differences

    [TestMethod]
    public void Diff_SingleCharDifferenceAtStart_HandlesCorrectly()
    {
        var result = Library.Diff("Xbc", "Ybc");
        Assert.AreEqual("[-X][+Y]bc", result);
    }

    [TestMethod]
    public void Diff_SingleCharDifferenceAtMiddle_HandlesCorrectly()
    {
        var result = Library.Diff("aXc", "aYc");
        Assert.AreEqual("a[-X][+Y]c", result);
    }

    [TestMethod]
    public void Diff_SingleCharDifferenceAtEnd_HandlesCorrectly()
    {
        var result = Library.Diff("abX", "abY");
        Assert.AreEqual("ab[-X][+Y]", result);
    }

    #endregion

    #region Diff String Output Tests - Multiple Differences

    [TestMethod]
    public void Diff_MultipleScatteredDifferences_HandlesCorrectly()
    {
        var result = Library.Diff("aXbYc", "aAbBc");
        Assert.AreEqual("a[-X][+A]b[-Y][+B]c", result);
    }

    [TestMethod]
    public void Diff_ConsecutiveDifferences_HandlesCorrectly()
    {
        var result = Library.Diff("aXYZb", "aABCb");
        Assert.AreEqual("a[-XYZ][+ABC]b", result);
    }

    #endregion

    #region DiffSegments Tests - Basic Functionality

    [TestMethod]
    public void DiffSegments_SimpleChange_ReturnsCorrectSegments()
    {
        var result = Library.DiffSegments("aXb", "aYb").ToList();

        Assert.HasCount(4, result);

        Assert.AreEqual("a", result[0].Text);
        Assert.AreEqual("Unchanged", result[0].Kind);
        Assert.AreEqual(0, result[0].Position);
        Assert.AreEqual(1, result[0].Length);

        Assert.AreEqual("X", result[1].Text);
        Assert.AreEqual("Deleted", result[1].Kind);
        Assert.AreEqual(1, result[1].Position);
        Assert.AreEqual(1, result[1].Length);

        Assert.AreEqual("Y", result[2].Text);
        Assert.AreEqual("Inserted", result[2].Kind);
        Assert.AreEqual(1, result[2].Position);
        Assert.AreEqual(1, result[2].Length);

        Assert.AreEqual("b", result[3].Text);
        Assert.AreEqual("Unchanged", result[3].Kind);
        Assert.AreEqual(2, result[3].Position);
        Assert.AreEqual(1, result[3].Length);
    }

    [TestMethod]
    public void DiffSegments_IdenticalStrings_ReturnsSingleUnchangedSegment()
    {
        var result = Library.DiffSegments("abc", "abc").ToList();

        Assert.HasCount(1, result);
        Assert.AreEqual("abc", result[0].Text);
        Assert.AreEqual("Unchanged", result[0].Kind);
        Assert.AreEqual(0, result[0].Position);
        Assert.AreEqual(3, result[0].Length);
    }

    [TestMethod]
    public void DiffSegments_CompletelyDifferent_ReturnsDeletedAndInserted()
    {
        var result = Library.DiffSegments("abc", "xyz").ToList();

        Assert.HasCount(2, result);

        Assert.AreEqual("abc", result[0].Text);
        Assert.AreEqual("Deleted", result[0].Kind);
        Assert.AreEqual(0, result[0].Position);
        Assert.AreEqual(3, result[0].Length);

        Assert.AreEqual("xyz", result[1].Text);
        Assert.AreEqual("Inserted", result[1].Kind);
        Assert.AreEqual(0, result[1].Position);
        Assert.AreEqual(3, result[1].Length);
    }

    #endregion

    #region DiffSegments Tests - Null and Empty Handling

    [TestMethod]
    public void DiffSegments_BothNull_ReturnsEmpty()
    {
        var result = Library.DiffSegments(null, null).ToList();
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void DiffSegments_FirstNull_ReturnsSingleInsertedSegment()
    {
        var result = Library.DiffSegments(null, "xyz").ToList();

        Assert.HasCount(1, result);
        Assert.AreEqual("xyz", result[0].Text);
        Assert.AreEqual("Inserted", result[0].Kind);
        Assert.AreEqual(0, result[0].Position);
        Assert.AreEqual(3, result[0].Length);
    }

    [TestMethod]
    public void DiffSegments_SecondNull_ReturnsSingleDeletedSegment()
    {
        var result = Library.DiffSegments("abc", null).ToList();

        Assert.HasCount(1, result);
        Assert.AreEqual("abc", result[0].Text);
        Assert.AreEqual("Deleted", result[0].Kind);
        Assert.AreEqual(0, result[0].Position);
        Assert.AreEqual(3, result[0].Length);
    }

    [TestMethod]
    public void DiffSegments_BothEmpty_ReturnsEmpty()
    {
        var result = Library.DiffSegments("", "").ToList();
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void DiffSegments_FirstEmpty_ReturnsSingleInsertedSegment()
    {
        var result = Library.DiffSegments("", "xyz").ToList();

        Assert.HasCount(1, result);
        Assert.AreEqual("xyz", result[0].Text);
        Assert.AreEqual("Inserted", result[0].Kind);
    }

    [TestMethod]
    public void DiffSegments_SecondEmpty_ReturnsSingleDeletedSegment()
    {
        var result = Library.DiffSegments("abc", "").ToList();

        Assert.HasCount(1, result);
        Assert.AreEqual("abc", result[0].Text);
        Assert.AreEqual("Deleted", result[0].Kind);
    }

    #endregion

    #region DiffSegments Tests - Segment Properties

    [TestMethod]
    public void DiffSegments_SegmentLengthMatchesTextLength()
    {
        var result = Library.DiffSegments("abcXYZdef", "abcABdef").ToList();

        foreach (var segment in result) Assert.AreEqual(segment.Text.Length, segment.Length);
    }

    [TestMethod]
    public void DiffSegments_AllKindsAreValid()
    {
        var result = Library.DiffSegments("aXb", "aYb").ToList();
        var validKinds = new[] { "Unchanged", "Deleted", "Inserted" };

        foreach (var segment in result)
            Assert.IsTrue(validKinds.Contains(segment.Kind), $"Invalid kind: {segment.Kind}");
    }

    #endregion

    #region Cross-Validation Tests

    [TestMethod]
    public void CrossValidation_ConcatenatingNonDeletedSegments_EqualsSecondString()
    {
        var first = "hello world";
        var second = "hello brave world";

        var segments = Library.DiffSegments(first, second).ToList();
        var reconstructed = string.Concat(segments
            .Where(s => s.Kind != "Deleted")
            .Select(s => s.Text));

        Assert.AreEqual(second, reconstructed);
    }

    [TestMethod]
    public void CrossValidation_ConcatenatingNonInsertedSegments_EqualsFirstString()
    {
        var first = "hello world";
        var second = "hello brave world";

        var segments = Library.DiffSegments(first, second).ToList();
        var reconstructed = string.Concat(segments
            .Where(s => s.Kind != "Inserted")
            .Select(s => s.Text));

        Assert.AreEqual(first, reconstructed);
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void Diff_SingleCharacterStrings_HandlesCorrectly()
    {
        Assert.AreEqual("a", Library.Diff("a", "a"));
        Assert.AreEqual("[-a][+b]", Library.Diff("a", "b"));
    }

    [TestMethod]
    public void Diff_AllCharactersDeleted_HandlesCorrectly()
    {
        Assert.AreEqual("[-abc]", Library.Diff("abc", ""));
    }

    [TestMethod]
    public void Diff_AllCharactersInserted_HandlesCorrectly()
    {
        Assert.AreEqual("[+abc]", Library.Diff("", "abc"));
    }

    [TestMethod]
    public void DiffSegments_AlternatingChanges_HandlesCorrectly()
    {
        var result = Library.DiffSegments("aXbYcZ", "aAbBcC").ToList();


        Assert.IsNotEmpty(result);


        Assert.AreEqual("a", result[0].Text);
        Assert.AreEqual("Unchanged", result[0].Kind);
    }

    #endregion
}