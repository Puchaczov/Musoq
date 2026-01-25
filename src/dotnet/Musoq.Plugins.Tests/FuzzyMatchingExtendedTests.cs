using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
///     Extended tests for fuzzy matching, soundex, and Levenshtein distance methods to improve branch coverage.
/// </summary>
[TestClass]
public class FuzzyMatchingExtendedTests : LibraryBaseBaseTests
{
    #region Soundex Tests

    [TestMethod]
    public void Soundex_Null_ReturnsNull()
    {
        Assert.IsNull(Library.Soundex(null));
    }

    [TestMethod]
    public void Soundex_ValidWord_ReturnsCode()
    {
        var result = Library.Soundex("Robert");
        Assert.IsNotNull(result);
        Assert.IsGreaterThanOrEqualTo(1, result.Length);
    }

    [TestMethod]
    public void Soundex_SimilarWords_ReturnSameCode()
    {
        var code1 = Library.Soundex("Robert");
        var code2 = Library.Soundex("Rupert");
        Assert.AreEqual(code1, code2);
    }

    #endregion

    #region HasFuzzyMatchedWord Tests

    [TestMethod]
    public void HasFuzzyMatchedWord_EmptyWord_ReturnsFalse()
    {
        Assert.IsFalse(Library.HasFuzzyMatchedWord("hello world", ""));
    }

    [TestMethod]
    public void HasFuzzyMatchedWord_NullWord_ReturnsFalse()
    {
        Assert.IsFalse(Library.HasFuzzyMatchedWord("hello world", null!));
    }

    [TestMethod]
    public void HasFuzzyMatchedWord_WhiteSpaceText_ReturnsFalse()
    {
        Assert.IsFalse(Library.HasFuzzyMatchedWord("   ", "hello"));
    }

    [TestMethod]
    public void HasFuzzyMatchedWord_ExactMatch_ReturnsTrue()
    {
        Assert.IsTrue(Library.HasFuzzyMatchedWord("hello world", "hello"));
    }

    [TestMethod]
    public void HasFuzzyMatchedWord_SimilarSoundingWord_ReturnsTrue()
    {
        Assert.IsTrue(Library.HasFuzzyMatchedWord("john smith", "jon"));
    }

    [TestMethod]
    public void HasFuzzyMatchedWord_NoMatch_ReturnsFalse()
    {
        Assert.IsFalse(Library.HasFuzzyMatchedWord("hello world", "xyz"));
    }

    [TestMethod]
    public void HasFuzzyMatchedWord_DefaultSeparator_Works()
    {
        Assert.IsTrue(Library.HasFuzzyMatchedWord("hello world", "hello"));
    }

    #endregion

    #region HasWordThatHasSmallerLevenshteinDistanceThan Tests

    [TestMethod]
    public void HasWordThatHasSmallerLevenshteinDistanceThan_EmptyWord_ReturnsFalse()
    {
        Assert.IsFalse(Library.HasWordThatHasSmallerLevenshteinDistanceThan("hello world", "", 2));
    }

    [TestMethod]
    public void HasWordThatHasSmallerLevenshteinDistanceThan_NullWord_ReturnsFalse()
    {
        Assert.IsFalse(Library.HasWordThatHasSmallerLevenshteinDistanceThan("hello world", null!, 2));
    }

    [TestMethod]
    public void HasWordThatHasSmallerLevenshteinDistanceThan_WhiteSpaceText_ReturnsFalse()
    {
        Assert.IsFalse(Library.HasWordThatHasSmallerLevenshteinDistanceThan("   ", "hello", 2));
    }

    [TestMethod]
    public void HasWordThatHasSmallerLevenshteinDistanceThan_ExactMatch_ReturnsTrue()
    {
        Assert.IsTrue(Library.HasWordThatHasSmallerLevenshteinDistanceThan("hello world", "hello", 0));
    }

    [TestMethod]
    public void HasWordThatHasSmallerLevenshteinDistanceThan_WithinDistance_ReturnsTrue()
    {
        Assert.IsTrue(Library.HasWordThatHasSmallerLevenshteinDistanceThan("hello world", "hallo", 2));
    }

    [TestMethod]
    public void HasWordThatHasSmallerLevenshteinDistanceThan_ExceedsDistance_ReturnsFalse()
    {
        Assert.IsFalse(Library.HasWordThatHasSmallerLevenshteinDistanceThan("hello world", "xyz", 1));
    }

    [TestMethod]
    public void HasWordThatHasSmallerLevenshteinDistanceThan_DefaultSeparator_Works()
    {
        Assert.IsTrue(Library.HasWordThatHasSmallerLevenshteinDistanceThan("hello world", "hello", 0));
    }

    #endregion

    #region HasWordThatSoundLike Tests

    [TestMethod]
    public void HasWordThatSoundLike_EmptyWord_ReturnsFalse()
    {
        Assert.IsFalse(Library.HasWordThatSoundLike("hello world", ""));
    }

    [TestMethod]
    public void HasWordThatSoundLike_NullWord_ReturnsFalse()
    {
        Assert.IsFalse(Library.HasWordThatSoundLike("hello world", null!));
    }

    [TestMethod]
    public void HasWordThatSoundLike_WhiteSpaceText_ReturnsFalse()
    {
        Assert.IsFalse(Library.HasWordThatSoundLike("   ", "hello"));
    }

    [TestMethod]
    public void HasWordThatSoundLike_SameSoundex_ReturnsTrue()
    {
        Assert.IsTrue(Library.HasWordThatSoundLike("rupert smith", "robert"));
    }

    [TestMethod]
    public void HasWordThatSoundLike_DifferentSoundex_ReturnsFalse()
    {
        Assert.IsFalse(Library.HasWordThatSoundLike("abc xyz", "hello"));
    }

    [TestMethod]
    public void HasWordThatSoundLike_DefaultSeparator_Works()
    {
        Assert.IsTrue(Library.HasWordThatSoundLike("hello world", "hello"));
    }

    #endregion

    #region HasTextThatSoundLikeSentence Tests

    [TestMethod]
    public void HasTextThatSoundLikeSentence_EmptySentence_ReturnsFalse()
    {
        Assert.IsFalse(Library.HasTextThatSoundLikeSentence("hello world", ""));
    }

    [TestMethod]
    public void HasTextThatSoundLikeSentence_NullSentence_ReturnsFalse()
    {
        Assert.IsFalse(Library.HasTextThatSoundLikeSentence("hello world", null!));
    }

    [TestMethod]
    public void HasTextThatSoundLikeSentence_WhiteSpaceText_ReturnsFalse()
    {
        Assert.IsFalse(Library.HasTextThatSoundLikeSentence("   ", "hello world"));
    }

    [TestMethod]
    public void HasTextThatSoundLikeSentence_AllWordsMatch_ReturnsTrue()
    {
        Assert.IsTrue(Library.HasTextThatSoundLikeSentence("hello world", "hello world"));
    }

    [TestMethod]
    public void HasTextThatSoundLikeSentence_SomeWordsMatch_ReturnsFalse()
    {
        Assert.IsFalse(Library.HasTextThatSoundLikeSentence("hello world", "hello xyz"));
    }

    [TestMethod]
    public void HasTextThatSoundLikeSentence_DefaultSeparator_Works()
    {
        Assert.IsTrue(Library.HasTextThatSoundLikeSentence("hello world", "hello world"));
    }

    #endregion

    #region LevenshteinDistance Tests

    [TestMethod]
    public void LevenshteinDistance_SameStrings_ReturnsZero()
    {
        Assert.AreEqual(0, Library.LevenshteinDistance("hello", "hello"));
    }

    [TestMethod]
    public void LevenshteinDistance_OneCharDiff_ReturnsOne()
    {
        Assert.AreEqual(1, Library.LevenshteinDistance("hello", "hallo"));
    }

    [TestMethod]
    public void LevenshteinDistance_FirstNull_ReturnsNull()
    {
        Assert.IsNull(Library.LevenshteinDistance(null, "hello"));
    }

    [TestMethod]
    public void LevenshteinDistance_SecondNull_ReturnsNull()
    {
        Assert.IsNull(Library.LevenshteinDistance("hello", null));
    }

    [TestMethod]
    public void LevenshteinDistance_FirstEmpty_ReturnsSecondLength()
    {
        Assert.AreEqual(5, Library.LevenshteinDistance("", "hello"));
    }

    [TestMethod]
    public void LevenshteinDistance_SecondEmpty_ReturnsFirstLength()
    {
        Assert.AreEqual(5, Library.LevenshteinDistance("hello", ""));
    }

    [TestMethod]
    public void LevenshteinDistance_CompletelyDifferent_ReturnsMax()
    {
        Assert.AreEqual(3, Library.LevenshteinDistance("abc", "xyz"));
    }

    #endregion

    #region ToUpper / ToLower Tests

    [TestMethod]
    public void ToUpper_LowercaseString_ReturnsUppercase()
    {
        Assert.AreEqual("HELLO", Library.ToUpper("hello"));
    }

    [TestMethod]
    public void ToUpper_WithCulture_ReturnsUppercase()
    {
        Assert.AreEqual("HELLO", Library.ToUpper("hello", "en-US"));
    }

    [TestMethod]
    public void ToUpperInvariant_LowercaseString_ReturnsUppercase()
    {
        Assert.AreEqual("HELLO", Library.ToUpperInvariant("hello"));
    }

    [TestMethod]
    public void ToLower_UppercaseString_ReturnsLowercase()
    {
        Assert.AreEqual("hello", Library.ToLower("HELLO"));
    }

    [TestMethod]
    public void ToLower_WithCulture_ReturnsLowercase()
    {
        Assert.AreEqual("hello", Library.ToLower("HELLO", "en-US"));
    }

    [TestMethod]
    public void ToLowerInvariant_UppercaseString_ReturnsLowercase()
    {
        Assert.AreEqual("hello", Library.ToLowerInvariant("HELLO"));
    }

    #endregion

    #region SplitAndTake Tests

    [TestMethod]
    public void SplitAndTake_Null_ReturnsNull()
    {
        Assert.IsNull(Library.SplitAndTake(null, ",", 0));
    }

    [TestMethod]
    public void SplitAndTake_NullDelimiter_ReturnsNull()
    {
        Assert.IsNull(Library.SplitAndTake("a,b,c", null, 0));
    }

    [TestMethod]
    public void SplitAndTake_ValidIndex_ReturnsElement()
    {
        Assert.AreEqual("b", Library.SplitAndTake("a,b,c", ",", 1));
    }

    [TestMethod]
    public void SplitAndTake_IndexOutOfRange_ReturnsNull()
    {
        Assert.IsNull(Library.SplitAndTake("a,b,c", ",", 5));
    }

    [TestMethod]
    public void SplitAndTake_NegativeIndex_ReturnsNull()
    {
        Assert.IsNull(Library.SplitAndTake("a,b,c", ",", -1));
    }

    #endregion

    #region PadLeft / PadRight Tests

    [TestMethod]
    public void PadLeft_Null_ReturnsNull()
    {
        Assert.IsNull(Library.PadLeft(null, 10));
    }

    [TestMethod]
    public void PadLeft_WithDefaultChar_PadsWithSpace()
    {
        Assert.AreEqual("   hello", Library.PadLeft("hello", 8));
    }

    [TestMethod]
    public void PadLeft_WithCustomChar_PadsWithChar()
    {
        Assert.AreEqual("000hello", Library.PadLeft("hello", 8, '0'));
    }

    [TestMethod]
    public void PadRight_Null_ReturnsNull()
    {
        Assert.IsNull(Library.PadRight(null, 10));
    }

    [TestMethod]
    public void PadRight_WithDefaultChar_PadsWithSpace()
    {
        Assert.AreEqual("hello   ", Library.PadRight("hello", 8));
    }

    [TestMethod]
    public void PadRight_WithCustomChar_PadsWithChar()
    {
        Assert.AreEqual("hello000", Library.PadRight("hello", 8, '0'));
    }

    #endregion

    #region RemoveDiacritics Tests

    [TestMethod]
    public void RemoveDiacritics_Null_ReturnsNull()
    {
        Assert.IsNull(Library.RemoveDiacritics(null));
    }

    [TestMethod]
    public void RemoveDiacritics_WithAccents_RemovesThem()
    {
        Assert.AreEqual("cafe", Library.RemoveDiacritics("café"));
    }

    [TestMethod]
    public void RemoveDiacritics_NoAccents_ReturnsUnchanged()
    {
        Assert.AreEqual("hello", Library.RemoveDiacritics("hello"));
    }

    [TestMethod]
    public void RemoveDiacritics_MultipleAccents_RemovesAll()
    {
        Assert.AreEqual("resume", Library.RemoveDiacritics("résumé"));
    }

    #endregion

    #region ReverseString Tests

    [TestMethod]
    public void ReverseString_Null_ReturnsNull()
    {
        Assert.IsNull(Library.ReverseString(null));
    }

    [TestMethod]
    public void ReverseString_ValidString_Reverses()
    {
        Assert.AreEqual("olleh", Library.ReverseString("hello"));
    }

    [TestMethod]
    public void ReverseString_Palindrome_ReturnsSame()
    {
        Assert.AreEqual("radar", Library.ReverseString("radar"));
    }

    #endregion
}
