using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class StringCultureConsistencyTests
{
    #region Soundex - InvariantCulture (Fixed from CurrentCulture ToUpper)

    [TestMethod]
    public void Soundex_BasicWord_ReturnsSoundexCode()
    {
        Assert.AreEqual("R163", Library.Soundex("Robert"));
    }

    [TestMethod]
    public void Soundex_SameSound_ReturnsSameCode()
    {
        Assert.AreEqual(Library.Soundex("Robert"), Library.Soundex("Rupert"));
    }

    [TestMethod]
    public void Soundex_DifferentSound_ReturnsDifferentCode()
    {
        Assert.AreNotEqual(Library.Soundex("Robert"), Library.Soundex("Smith"));
    }

    [TestMethod]
    public void Soundex_NullInput_ReturnsNull()
    {
        Assert.IsNull(Library.Soundex(null));
    }

    [TestMethod]
    public void Soundex_LowercaseInput_WorksCorrectly()
    {
        Assert.AreEqual(Library.Soundex("Robert"), Library.Soundex("robert"));
    }

    [TestMethod]
    public void Soundex_MixedCaseInput_WorksCorrectly()
    {
        Assert.AreEqual(Library.Soundex("ROBERT"), Library.Soundex("rObErT"));
    }

    #endregion

    #region Cross-Function Consistency Checks

    [TestMethod]
    public void AllSearchFunctions_CaseInsensitive_Consistent()
    {
        var text = "Hello World";


        Assert.IsTrue(Library.Contains(text, "hello"), "Contains should be case-insensitive");
        Assert.AreEqual(0, Library.IndexOf(text, "hello"), "IndexOf should be case-insensitive");
        Assert.IsTrue(Library.StartsWith(text, "hello"), "StartsWith should be case-insensitive");


        Assert.IsTrue(Library.Contains(text, "WORLD"), "Contains should find WORLD");
        Assert.AreEqual(6, Library.IndexOf(text, "WORLD"), "IndexOf should find WORLD");
        Assert.IsTrue(Library.EndsWith(text, "WORLD"), "EndsWith should be case-insensitive");
    }

    [TestMethod]
    public void IndexOf_And_NthIndexOf_ConsistentBehavior()
    {
        var text = "Hello World Hello";


        var indexOfResult = Library.IndexOf(text, "hello");


        var nthResult = Library.NthIndexOf(text, "hello", 0);

        Assert.AreEqual(indexOfResult, nthResult,
            "IndexOf and NthIndexOf(0) should return the same position");
    }

    [TestMethod]
    public void IndexOf_And_LastIndexOf_ConsistentBehavior()
    {
        var text = "test";


        var indexOfResult = Library.IndexOf(text, "TEST");
        var lastIndexOfResult = Library.LastIndexOf(text, "TEST");

        Assert.AreEqual(indexOfResult, lastIndexOfResult,
            "IndexOf and LastIndexOf should agree for single occurrence");
    }

    [TestMethod]
    public void Replace_And_Contains_ConsistentBehavior()
    {
        var text = "Hello World";


        Assert.IsTrue(Library.Contains(text, "world"));
        var replaced = Library.Replace(text, "world", "Earth");
        Assert.AreEqual("Hello Earth", replaced);
    }

    [TestMethod]
    public void StartsWith_And_RemovePrefix_ConsistentBehavior()
    {
        var text = "HelloWorld";


        Assert.IsTrue(Library.StartsWith(text, "HELLO"));
        var result = Library.RemovePrefix(text, "HELLO");
        Assert.AreEqual("World", result);
    }

    [TestMethod]
    public void EndsWith_And_RemoveSuffix_ConsistentBehavior()
    {
        var text = "HelloWorld";


        Assert.IsTrue(Library.EndsWith(text, "WORLD"));
        var result = Library.RemoveSuffix(text, "WORLD");
        Assert.AreEqual("Hello", result);
    }

    #endregion

    #region Unicode and Multilingual

    [TestMethod]
    public void Contains_Unicode_Polish_ShouldWork()
    {
        Assert.IsTrue(Library.Contains("Zażółć gęślą jaźń", "gęślą"));
    }

    [TestMethod]
    public void IndexOf_Unicode_Russian_ShouldWork()
    {
        Assert.AreEqual(7, Library.IndexOf("Привет мир", "мир"));
    }

    [TestMethod]
    public void NthIndexOf_Unicode_Japanese_ShouldWork()
    {
        var input = "東京 大阪 東京 名古屋";
        Assert.AreEqual(0, Library.NthIndexOf(input, "東京", 0));
        Assert.AreEqual(6, Library.NthIndexOf(input, "東京", 1));
    }

    [TestMethod]
    public void LastIndexOf_Unicode_Chinese_ShouldWork()
    {
        var result = Library.LastIndexOf("北京 上海 北京", "北京");
        Assert.AreEqual(6, result);
    }

    [TestMethod]
    public void Replace_Unicode_Korean_ShouldWork()
    {
        var result = Library.Replace("서울은 한국의 수도입니다", "서울", "부산");
        Assert.AreEqual("부산은 한국의 수도입니다", result);
    }

    [TestMethod]
    public void StartsWith_Unicode_Arabic_ShouldWork()
    {
        Assert.IsTrue(Library.StartsWith("مرحبا بالعالم", "مرحبا"));
    }

    [TestMethod]
    public void EndsWith_Unicode_Thai_ShouldWork()
    {
        Assert.IsTrue(Library.EndsWith("สวัสดีครับ", "ครับ"));
    }

    [TestMethod]
    public void RemovePrefix_Unicode_German_ShouldWork()
    {
        Assert.AreEqual(" München", Library.RemovePrefix("Grüße München", "Grüße"));
    }

    [TestMethod]
    public void RemoveSuffix_Unicode_French_ShouldWork()
    {
        Assert.AreEqual("Château de ", Library.RemoveSuffix("Château de Versailles", "Versailles"));
    }

    [TestMethod]
    public void ToUpper_Unicode_Greek_ShouldWork()
    {
        Assert.AreEqual("ΑΘΉΝΑ", Library.ToUpper("Αθήνα"));
    }

    [TestMethod]
    public void ToLower_Unicode_Ukrainian_ShouldWork()
    {
        Assert.AreEqual("київ", Library.ToLower("КИЇВ"));
    }

    #endregion

    #region Emoji Support

    [TestMethod]
    public void Contains_Emoji_ShouldWork()
    {
        Assert.IsTrue(Library.Contains("Hello 🌍 World", "🌍"));
    }

    [TestMethod]
    public void Replace_Emoji_ShouldWork()
    {
        var result = Library.Replace("I ❤️ coding", "❤️", "💙");
        Assert.AreEqual("I 💙 coding", result);
    }

    [TestMethod]
    public void IndexOf_Emoji_ShouldWork()
    {
        var result = Library.IndexOf("abc 🎉 def", "🎉");
        Assert.IsNotNull(result);
        Assert.IsGreaterThanOrEqualTo(0, result.Value);
    }

    #endregion
}
