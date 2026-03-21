using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class MultilingualTextTests
{
    #region Japanese Text Tests

    [TestMethod]
    public void WhenJapaneseHiraganaUsedAsLiteral_ShouldReturnCorrectValue()
    {
        var query = "select 'こんにちは世界' from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [new BasicEntity("test")]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("こんにちは世界", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenJapaneseKatakanaUsedAsLiteral_ShouldReturnCorrectValue()
    {
        var query = "select 'コンピュータ' from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [new BasicEntity("test")]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("コンピュータ", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenJapaneseKanjiInColumn_ShouldSelectCorrectly()
    {
        var query = "select Name from #A.entities() where Name = '東京'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("東京"),
                    new BasicEntity("大阪"),
                    new BasicEntity("京都")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("東京", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenJapaneseTextUsedWithLike_ShouldMatchCorrectly()
    {
        var query = "select Name from #A.entities() where Name like '%京%'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("東京"),
                    new BasicEntity("大阪"),
                    new BasicEntity("京都")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "東京"),
            "Row with '東京' not found");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "京都"),
            "Row with '京都' not found");
    }

    [TestMethod]
    public void WhenJapaneseTextGroupedByName_ShouldGroupCorrectly()
    {
        var query = "select Name, Count(Name) from #A.entities() group by Name having Count(Name) >= 2";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("東京"),
                    new BasicEntity("東京"),
                    new BasicEntity("大阪"),
                    new BasicEntity("東京"),
                    new BasicEntity("大阪")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "東京" && (int)row.Values[1] == 3),
            "Expected group '東京' with count 3");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "大阪" && (int)row.Values[1] == 2),
            "Expected group '大阪' with count 2");
    }

    [TestMethod]
    public void WhenMixedJapaneseScriptsSelected_ShouldReturnAll()
    {
        var query = "select Name from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("ひらがな"),
                    new BasicEntity("カタカナ"),
                    new BasicEntity("漢字")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "ひらがな"),
            "Hiragana row not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "カタカナ"),
            "Katakana row not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "漢字"),
            "Kanji row not found");
    }

    #endregion
    #region Chinese Text Tests

    [TestMethod]
    public void WhenChineseSimplifiedUsedAsLiteral_ShouldReturnCorrectValue()
    {
        var query = "select '你好世界' from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [new BasicEntity("test")]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("你好世界", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenChineseTraditionalUsedAsLiteral_ShouldReturnCorrectValue()
    {
        var query = "select '計算機科學' from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [new BasicEntity("test")]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("計算機科學", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenChineseTextInColumn_ShouldSelectCorrectly()
    {
        var query = "select Name from #A.entities() where Name = '北京'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("北京"),
                    new BasicEntity("上海"),
                    new BasicEntity("广州")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("北京", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenChineseTextUsedWithLike_ShouldMatchCorrectly()
    {
        var query = "select Name from #A.entities() where Name like '%京%'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("北京"),
                    new BasicEntity("南京"),
                    new BasicEntity("上海")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "北京"),
            "Row with '北京' not found");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "南京"),
            "Row with '南京' not found");
    }

    [TestMethod]
    public void WhenChineseTextGroupedByName_ShouldGroupCorrectly()
    {
        var query = "select Name, Count(Name) from #A.entities() group by Name having Count(Name) >= 2";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("北京"),
                    new BasicEntity("北京"),
                    new BasicEntity("上海"),
                    new BasicEntity("北京"),
                    new BasicEntity("上海")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "北京" && (int)row.Values[1] == 3),
            "Expected group '北京' with count 3");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "上海" && (int)row.Values[1] == 2),
            "Expected group '上海' with count 2");
    }

    #endregion
    #region Korean Text Tests

    [TestMethod]
    public void WhenKoreanTextUsedAsLiteral_ShouldReturnCorrectValue()
    {
        var query = "select '안녕하세요 세계' from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [new BasicEntity("test")]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("안녕하세요 세계", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenKoreanTextInColumn_ShouldSelectCorrectly()
    {
        var query = "select Name from #A.entities() where Name = '서울'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("서울"),
                    new BasicEntity("부산"),
                    new BasicEntity("인천")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("서울", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenKoreanTextUsedWithLike_ShouldMatchCorrectly()
    {
        var query = "select Name from #A.entities() where Name like '%프로그래밍%'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("컴퓨터 프로그래밍"),
                    new BasicEntity("데이터베이스"),
                    new BasicEntity("프로그래밍 언어")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "컴퓨터 프로그래밍"),
            "Row with '컴퓨터 프로그래밍' not found");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "프로그래밍 언어"),
            "Row with '프로그래밍 언어' not found");
    }

    [TestMethod]
    public void WhenKoreanTextGroupedByName_ShouldGroupCorrectly()
    {
        var query = "select Name, Count(Name) from #A.entities() group by Name having Count(Name) >= 2";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("서울"),
                    new BasicEntity("서울"),
                    new BasicEntity("부산"),
                    new BasicEntity("서울"),
                    new BasicEntity("부산")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "서울" && (int)row.Values[1] == 3),
            "Expected group '서울' with count 3");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "부산" && (int)row.Values[1] == 2),
            "Expected group '부산' with count 2");
    }

    #endregion
}
