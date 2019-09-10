using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests
{
    [TestClass]
    public class StringsTests : LibraryBaseBaseTests
    {
        [TestMethod]
        public void SubstrTest()
        {
            Assert.AreEqual("lorem", Library.Substring("lorem ipsum dolor", 0, 5));
            Assert.AreEqual("lorem ipsum dolor", Library.Substring("lorem ipsum dolor", 0, 150));
            Assert.AreEqual(string.Empty, Library.Substring("lorem ipsum dolor", 0, 0));
            Assert.AreEqual(null, Library.Substring(null, 0, 5));

            Assert.AreEqual(string.Empty, Library.Substring("lorem ipsum dolor", 0));
            Assert.AreEqual("lorem", Library.Substring("lorem ipsum dolor", 5));
            Assert.AreEqual("lorem ipsum dolor", Library.Substring("lorem ipsum dolor", 150));
            Assert.AreEqual(null, Library.Substring(null, 150));
        }

        [TestMethod]
        public void ConcatTest()
        {
            Assert.AreEqual("lorem ipsum dolor", Library.Concat("lorem ", "ipsum ", "dolor"));
            Assert.AreEqual("lorem dolor", Library.Concat("lorem ", null, "dolor"));
            Assert.AreEqual("this is 1", Library.Concat("this ", "is ", 1));
        }

        [TestMethod]
        public void ContainsTest()
        {
            Assert.AreEqual(true, Library.Contains("lorem ipsum dolor", "ipsum"));
            Assert.AreEqual(true, Library.Contains("lorem ipsum dolor", "IPSUM"));
            Assert.AreEqual(false, Library.Contains("lorem ipsum dolor", "ratatata"));
        }

        [TestMethod]
        public void IndexOfTest()
        {
            Assert.AreEqual(6, Library.IndexOf("lorem ipsum dolor", "ipsum"));
            Assert.AreEqual(-1, Library.IndexOf("lorem ipsum dolor", "tatarata"));
        }

        [TestMethod]
        public void SoundexTest()
        {
            Assert.AreEqual("W355", Library.Soundex("Woda Mineralna"));
            Assert.AreEqual("T221", Library.Soundex("This is very long text that have to be soundexed"));
        }

        [TestMethod]
        public void ToHexTest()
        {
            Assert.AreEqual("01,05,07,09,0B,0D,0F,11,19", Library.ToHex(new byte[] { 1, 5, 7, 9, 11, 13, 15, 17, 25 }, ","));
        }

        [TestMethod]
        public void LongestCommonSubstringTest()
        {
            Assert.AreEqual("vwtgw", Library.LongestCommonSubstring("svfvwtgwdsd", "vwtgw"));
            Assert.AreEqual("vwtgw", Library.LongestCommonSubstring("vwtgwdsd", "vwtgw"));
            Assert.AreEqual("vwtgw", Library.LongestCommonSubstring("svfvwtgw", "vwtgw"));
        }

        [TestMethod]
        public void SplitTest()
        {
            var values = Library.Split("test,test2", ",");
            Assert.AreEqual("test", values[0]);
            Assert.AreEqual("test2", values[1]);

            values = Library.Split("test,test2;test3", ",", ";");
            Assert.AreEqual("test", values[0]);
            Assert.AreEqual("test2", values[1]);
            Assert.AreEqual("test3", values[2]);
        }
    }
}
