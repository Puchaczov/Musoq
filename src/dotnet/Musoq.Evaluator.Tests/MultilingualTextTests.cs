using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class MultilingualTextTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    #region Polish Text Tests

    [TestMethod]
    public void WhenPolishTextUsedAsLiteral_ShouldReturnCorrectValue()
    {
        var query = "select 'Zażółć gęślą jaźń' from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [new BasicEntity("test")]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Zażółć gęślą jaźń", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenPolishTextInColumn_ShouldSelectCorrectly()
    {
        var query = "select Name from #A.entities() where Name = 'Wrocław'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Wrocław"),
                    new BasicEntity("Kraków"),
                    new BasicEntity("Łódź")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Wrocław", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenPolishTextUsedWithLike_ShouldMatchCorrectly()
    {
        var query = "select Name from #A.entities() where Name like '%ółć%'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Zażółć"),
                    new BasicEntity("Gęślą"),
                    new BasicEntity("Jaźń")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Zażółć", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenPolishTextGroupedByName_ShouldGroupCorrectly()
    {
        var query = "select Name, Count(Name) from #A.entities() group by Name having Count(Name) >= 2";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Łódź"),
                    new BasicEntity("Łódź"),
                    new BasicEntity("Kraków"),
                    new BasicEntity("Łódź"),
                    new BasicEntity("Kraków")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "Łódź" && (int)row.Values[1] == 3),
            "Expected group 'Łódź' with count 3");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "Kraków" && (int)row.Values[1] == 2),
            "Expected group 'Kraków' with count 2");
    }

    #endregion

    #region Russian Text Tests

    [TestMethod]
    public void WhenRussianTextUsedAsLiteral_ShouldReturnCorrectValue()
    {
        var query = "select 'Привет мир' from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [new BasicEntity("test")]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Привет мир", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenRussianTextInColumn_ShouldSelectCorrectly()
    {
        var query = "select Name from #A.entities() where Name = 'Москва'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Москва"),
                    new BasicEntity("Санкт-Петербург"),
                    new BasicEntity("Новосибирск")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Москва", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenRussianTextUsedWithLike_ShouldMatchCorrectly()
    {
        var query = "select Name from #A.entities() where Name like '%Петер%'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Москва"),
                    new BasicEntity("Санкт-Петербург"),
                    new BasicEntity("Новосибирск")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Санкт-Петербург", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenRussianTextGroupedByName_ShouldGroupCorrectly()
    {
        var query = "select Name, Count(Name) from #A.entities() group by Name having Count(Name) >= 2";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Москва"),
                    new BasicEntity("Москва"),
                    new BasicEntity("Казань"),
                    new BasicEntity("Москва"),
                    new BasicEntity("Казань")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "Москва" && (int)row.Values[1] == 3),
            "Expected group 'Москва' with count 3");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "Казань" && (int)row.Values[1] == 2),
            "Expected group 'Казань' with count 2");
    }

    #endregion

    #region French Text Tests

    [TestMethod]
    public void WhenFrenchTextUsedAsLiteral_ShouldReturnCorrectValue()
    {
        var query = "select 'Les élèves français étudient à l\\'université' from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [new BasicEntity("test")]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Les élèves français étudient à l'université", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenFrenchTextInColumn_ShouldSelectCorrectly()
    {
        var query = "select Name from #A.entities() where Name = 'Montréal'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Montréal"),
                    new BasicEntity("Château"),
                    new BasicEntity("François")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Montréal", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenFrenchTextUsedWithLike_ShouldMatchCorrectly()
    {
        var query = "select Name from #A.entities() where Name like '%âteau%'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Montréal"),
                    new BasicEntity("Château"),
                    new BasicEntity("François")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Château", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenFrenchAccentedCharactersGrouped_ShouldGroupCorrectly()
    {
        var query = "select Name, Count(Name) from #A.entities() group by Name having Count(Name) >= 2";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Résumé"),
                    new BasicEntity("Résumé"),
                    new BasicEntity("Café"),
                    new BasicEntity("Résumé"),
                    new BasicEntity("Café")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "Résumé" && (int)row.Values[1] == 3),
            "Expected group 'Résumé' with count 3");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "Café" && (int)row.Values[1] == 2),
            "Expected group 'Café' with count 2");
    }

    #endregion

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

    #region Arabic Text Tests

    [TestMethod]
    public void WhenArabicTextUsedAsLiteral_ShouldReturnCorrectValue()
    {
        var query = "select 'مرحبا بالعالم' from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [new BasicEntity("test")]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("مرحبا بالعالم", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenArabicTextInColumn_ShouldSelectCorrectly()
    {
        var query = "select Name from #A.entities() where Name = 'القاهرة'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("القاهرة"),
                    new BasicEntity("الرياض"),
                    new BasicEntity("دبي")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("القاهرة", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenArabicTextUsedWithLike_ShouldMatchCorrectly()
    {
        var query = "select Name from #A.entities() where Name like '%الر%'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("القاهرة"),
                    new BasicEntity("الرياض"),
                    new BasicEntity("دبي")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("الرياض", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenArabicTextGroupedByName_ShouldGroupCorrectly()
    {
        var query = "select Name, Count(Name) from #A.entities() group by Name having Count(Name) >= 2";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("القاهرة"),
                    new BasicEntity("القاهرة"),
                    new BasicEntity("دبي"),
                    new BasicEntity("القاهرة"),
                    new BasicEntity("دبي")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "القاهرة" && (int)row.Values[1] == 3),
            "Expected group 'القاهرة' with count 3");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "دبي" && (int)row.Values[1] == 2),
            "Expected group 'دبي' with count 2");
    }

    #endregion

    #region German Text Tests

    [TestMethod]
    public void WhenGermanTextUsedAsLiteral_ShouldReturnCorrectValue()
    {
        var query = "select 'Ärzte überprüfen größere Übungen' from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [new BasicEntity("test")]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Ärzte überprüfen größere Übungen", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenGermanTextWithEszettInColumn_ShouldSelectCorrectly()
    {
        var query = "select Name from #A.entities() where Name = 'Straße'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Straße"),
                    new BasicEntity("München"),
                    new BasicEntity("Düsseldorf")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Straße", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenGermanUmlautsUsedWithLike_ShouldMatchCorrectly()
    {
        var query = "select Name from #A.entities() where Name like '%ünch%'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Straße"),
                    new BasicEntity("München"),
                    new BasicEntity("Düsseldorf")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("München", table[0].Values[0]);
    }

    #endregion

    #region Thai Text Tests

    [TestMethod]
    public void WhenThaiTextUsedAsLiteral_ShouldReturnCorrectValue()
    {
        var query = "select 'สวัสดีชาวโลก' from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [new BasicEntity("test")]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("สวัสดีชาวโลก", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenThaiTextInColumn_ShouldSelectCorrectly()
    {
        var query = "select Name from #A.entities() where Name = 'กรุงเทพมหานคร'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("กรุงเทพมหานคร"),
                    new BasicEntity("เชียงใหม่"),
                    new BasicEntity("ภูเก็ต")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("กรุงเทพมหานคร", table[0].Values[0]);
    }

    #endregion

    #region Hebrew Text Tests

    [TestMethod]
    public void WhenHebrewTextUsedAsLiteral_ShouldReturnCorrectValue()
    {
        var query = "select 'שלום עולם' from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [new BasicEntity("test")]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("שלום עולם", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenHebrewTextInColumn_ShouldSelectCorrectly()
    {
        var query = "select Name from #A.entities() where Name = 'ירושלים'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("ירושלים"),
                    new BasicEntity("תל אביב"),
                    new BasicEntity("חיפה")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("ירושלים", table[0].Values[0]);
    }

    #endregion

    #region Hindi Text Tests

    [TestMethod]
    public void WhenHindiTextUsedAsLiteral_ShouldReturnCorrectValue()
    {
        var query = "select 'नमस्ते दुनिया' from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [new BasicEntity("test")]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("नमस्ते दुनिया", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenHindiTextInColumn_ShouldSelectCorrectly()
    {
        var query = "select Name from #A.entities() where Name = 'मुंबई'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("मुंबई"),
                    new BasicEntity("दिल्ली"),
                    new BasicEntity("कोलकाता")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("मुंबई", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenHindiTextUsedWithLike_ShouldMatchCorrectly()
    {
        var query = "select Name from #A.entities() where Name like '%दिल%'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("मुंबई"),
                    new BasicEntity("दिल्ली"),
                    new BasicEntity("कोलकाता")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("दिल्ली", table[0].Values[0]);
    }

    #endregion

    #region Turkish Text Tests

    [TestMethod]
    public void WhenTurkishTextUsedAsLiteral_ShouldReturnCorrectValue()
    {
        var query = "select 'Türkçe öğretmeni çalışıyor' from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [new BasicEntity("test")]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Türkçe öğretmeni çalışıyor", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenTurkishSpecialCharsInColumn_ShouldSelectCorrectly()
    {
        var query = "select Name from #A.entities() where Name = 'İstanbul'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("İstanbul"),
                    new BasicEntity("Ankara"),
                    new BasicEntity("İzmir")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("İstanbul", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenTurkishDotlessIAndDottedI_ShouldDistinguishCorrectly()
    {
        var query = "select Name from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("İ"),
                    new BasicEntity("ı"),
                    new BasicEntity("I"),
                    new BasicEntity("i")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);

        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "İ"),
            "Row with 'İ' (Turkish dotted I) not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "ı"),
            "Row with 'ı' (Turkish dotless i) not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "I"),
            "Row with 'I' (Latin capital I) not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "i"),
            "Row with 'i' (Latin lowercase i) not found");
    }

    #endregion

    #region Greek Text Tests

    [TestMethod]
    public void WhenGreekTextUsedAsLiteral_ShouldReturnCorrectValue()
    {
        var query = "select 'Γεια σου κόσμε' from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [new BasicEntity("test")]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Γεια σου κόσμε", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenGreekTextInColumn_ShouldSelectCorrectly()
    {
        var query = "select Name from #A.entities() where Name = 'Αθήνα'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Αθήνα"),
                    new BasicEntity("Θεσσαλονίκη"),
                    new BasicEntity("Πάτρα")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Αθήνα", table[0].Values[0]);
    }

    #endregion

    #region Ukrainian Text Tests

    [TestMethod]
    public void WhenUkrainianTextUsedAsLiteral_ShouldReturnCorrectValue()
    {
        var query = "select 'Привіт світе' from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [new BasicEntity("test")]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Привіт світе", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenUkrainianTextInColumn_ShouldSelectCorrectly()
    {
        var query = "select Name from #A.entities() where Name = 'Київ'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Київ"),
                    new BasicEntity("Львів"),
                    new BasicEntity("Одеса")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Київ", table[0].Values[0]);
    }

    #endregion

    #region Vietnamese Text Tests

    [TestMethod]
    public void WhenVietnameseTextUsedAsLiteral_ShouldReturnCorrectValue()
    {
        var query = "select 'Xin chào thế giới' from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [new BasicEntity("test")]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Xin chào thế giới", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenVietnameseTextInColumn_ShouldSelectCorrectly()
    {
        var query = "select Name from #A.entities() where Name = 'Hà Nội'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Hà Nội"),
                    new BasicEntity("Thành phố Hồ Chí Minh"),
                    new BasicEntity("Đà Nẵng")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Hà Nội", table[0].Values[0]);
    }

    #endregion

    #region Mixed Languages and Cross-Language Tests

    [TestMethod]
    public void WhenMultipleLanguagesInSameQuery_ShouldReturnAll()
    {
        var query = "select Name from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Hello"),
                    new BasicEntity("Привет"),
                    new BasicEntity("こんにちは"),
                    new BasicEntity("你好"),
                    new BasicEntity("مرحبا"),
                    new BasicEntity("Cześć"),
                    new BasicEntity("안녕하세요"),
                    new BasicEntity("Bonjour"),
                    new BasicEntity("สวัสดี"),
                    new BasicEntity("नमस्ते")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(10, table.Count);

        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Hello"), "Row 'Hello' not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Привет"), "Row 'Привет' not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "こんにちは"), "Row 'こんにちは' not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "你好"), "Row '你好' not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "مرحبا"), "Row 'مرحبا' not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Cześć"), "Row 'Cześć' not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "안녕하세요"), "Row '안녕하세요' not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Bonjour"), "Row 'Bonjour' not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "สวัสดี"), "Row 'สวัสดี' not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "नमस्ते"), "Row 'नमस्ते' not found");
    }

    [TestMethod]
    public void WhenMultipleLanguagesGrouped_ShouldGroupByLanguageCorrectly()
    {
        var query = "select Country, Count(Country) from #A.entities() group by Country";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Polska", "Warszawa"),
                    new BasicEntity("Polska", "Kraków"),
                    new BasicEntity("日本", "東京"),
                    new BasicEntity("日本", "大阪"),
                    new BasicEntity("Россия", "Москва"),
                    new BasicEntity("中国", "北京")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "Polska" && (int)row.Values[1] == 2),
            "Expected group 'Polska' with count 2");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "日本" && (int)row.Values[1] == 2),
            "Expected group '日本' with count 2");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "Россия" && (int)row.Values[1] == 1),
            "Expected group 'Россия' with count 1");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "中国" && (int)row.Values[1] == 1),
            "Expected group '中国' with count 1");
    }

    [TestMethod]
    public void WhenMultipleLanguagesFilteredWithWhere_ShouldFilterCorrectly()
    {
        var query = "select City, Country from #A.entities() where Country = '日本'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Polska", "Warszawa"),
                    new BasicEntity("日本", "東京"),
                    new BasicEntity("日本", "大阪"),
                    new BasicEntity("Россия", "Москва")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "東京" && (string)row.Values[1] == "日本"),
            "Row with '東京' not found");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "大阪" && (string)row.Values[1] == "日本"),
            "Row with '大阪' not found");
    }

    [TestMethod]
    public void WhenConcatenatingMultipleLanguages_ShouldConcatenateCorrectly()
    {
        var query = "select City + ' - ' + Country from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("日本", "東京"),
                    new BasicEntity("Polska", "Kraków"),
                    new BasicEntity("Россия", "Москва")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "東京 - 日本"),
            "Row '東京 - 日本' not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Kraków - Polska"),
            "Row 'Kraków - Polska' not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Москва - Россия"),
            "Row 'Москва - Россия' not found");
    }

    [TestMethod]
    public void WhenOrderingMultilingualText_ShouldNotThrow()
    {
        var query = "select Name from #A.entities() order by Name asc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Kraków"),
                    new BasicEntity("東京"),
                    new BasicEntity("Москва"),
                    new BasicEntity("北京"),
                    new BasicEntity("서울")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(5, table.Count);


        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Kraków"),
            "Row with 'Kraków' not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "東京"),
            "Row with '東京' not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Москва"),
            "Row with 'Москва' not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "北京"),
            "Row with '北京' not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "서울"),
            "Row with '서울' not found");
    }

    [TestMethod]
    public void WhenDistinctOnMultilingualText_ShouldRemoveDuplicatesCorrectly()
    {
        var query = "select distinct Name from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Kraków"),
                    new BasicEntity("東京"),
                    new BasicEntity("Kraków"),
                    new BasicEntity("東京"),
                    new BasicEntity("Москва"),
                    new BasicEntity("Москва"),
                    new BasicEntity("北京")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);

        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Kraków"),
            "Row with 'Kraków' not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "東京"),
            "Row with '東京' not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Москва"),
            "Row with 'Москва' not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "北京"),
            "Row with '北京' not found");
    }

    [TestMethod]
    public void WhenMultilingualTextInUnion_ShouldCombineCorrectly()
    {
        var query = @"
            select Name from #A.entities()
            union all (Name)
            select Name from #B.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Kraków"),
                    new BasicEntity("東京")
                ]
            },
            {
                "#B", [
                    new BasicEntity("Москва"),
                    new BasicEntity("北京")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);

        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Kraków"),
            "Row with 'Kraków' not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "東京"),
            "Row with '東京' not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Москва"),
            "Row with 'Москва' not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "北京"),
            "Row with '北京' not found");
    }

    [TestMethod]
    public void WhenMultilingualLiteralsUsedInCaseWhen_ShouldEvaluateCorrectly()
    {
        var query = @"
            select
                case
                    when Name = 'Tokyo' then '東京'
                    when Name = 'Moscow' then 'Москва'
                    when Name = 'Beijing' then '北京'
                    when Name = 'Warsaw' then 'Warszawa'
                    else Name
                end
            from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Tokyo"),
                    new BasicEntity("Moscow"),
                    new BasicEntity("Beijing"),
                    new BasicEntity("Warsaw")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);

        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "東京"),
            "Row '東京' not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Москва"),
            "Row 'Москва' not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "北京"),
            "Row '北京' not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Warszawa"),
            "Row 'Warszawa' not found");
    }

    [TestMethod]
    public void WhenMultilingualTextInContains_ShouldMatchCorrectly()
    {
        var query = "select Name from #A.entities() where Name contains ('東京', 'Москва')";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("東京"),
                    new BasicEntity("大阪"),
                    new BasicEntity("Москва"),
                    new BasicEntity("Краків")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "東京"),
            "Row with '東京' not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Москва"),
            "Row with 'Москва' not found");
    }

    [TestMethod]
    public void WhenMultilingualTextInIn_ShouldMatchCorrectly()
    {
        var query = "select Name from #A.entities() where Name in ('東京', 'Москва', '北京')";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("東京"),
                    new BasicEntity("大阪"),
                    new BasicEntity("Москва"),
                    new BasicEntity("Kraków"),
                    new BasicEntity("北京")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "東京"),
            "Row with '東京' not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Москва"),
            "Row with 'Москва' not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "北京"),
            "Row with '北京' not found");
    }

    #endregion

    #region Emoji and Special Unicode Tests

    [TestMethod]
    public void WhenEmojiTextUsedAsLiteral_ShouldReturnCorrectValue()
    {
        var query = "select '😀🎉🌍' from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [new BasicEntity("test")]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("😀🎉🌍", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenEmojiInColumn_ShouldSelectCorrectly()
    {
        var query = "select Name from #A.entities() where Name = '🌍'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("😀"),
                    new BasicEntity("🌍"),
                    new BasicEntity("🎉")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("🌍", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenEmojiGroupedByName_ShouldGroupCorrectly()
    {
        var query = "select Name, Count(Name) from #A.entities() group by Name";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("😀"),
                    new BasicEntity("😀"),
                    new BasicEntity("🌍"),
                    new BasicEntity("🎉"),
                    new BasicEntity("🌍")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "😀" && (int)row.Values[1] == 2),
            "Expected group '😀' with count 2");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "🌍" && (int)row.Values[1] == 2),
            "Expected group '🌍' with count 2");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "🎉" && (int)row.Values[1] == 1),
            "Expected group '🎉' with count 1");
    }

    [TestMethod]
    public void WhenMixedEmojiAndMultilingualText_ShouldHandleCorrectly()
    {
        var query = "select Name from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("🇯🇵 東京"),
                    new BasicEntity("🇷🇺 Москва"),
                    new BasicEntity("🇵🇱 Kraków"),
                    new BasicEntity("🇨🇳 北京")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);

        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "🇯🇵 東京"),
            "Row '🇯🇵 東京' not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "🇷🇺 Москва"),
            "Row '🇷🇺 Москва' not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "🇵🇱 Kraków"),
            "Row '🇵🇱 Kraków' not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "🇨🇳 北京"),
            "Row '🇨🇳 北京' not found");
    }

    #endregion

    #region Edge Cases and String Operations on Multilingual Text

    [TestMethod]
    public void WhenConcatenatingPolishStrings_ShouldConcatenateCorrectly()
    {
        var query = "select City + ' ' + Country from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Polska", "Wrocław")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Wrocław Polska", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenRussianAndChineseComparedWithNotEqual_ShouldReturnCorrectResult()
    {
        var query = "select Name from #A.entities() where Name <> '东京'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("东京"),
                    new BasicEntity("Москва"),
                    new BasicEntity("东京")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Москва", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenMultilineLiteralWithMultipleLanguages_ShouldHandleCorrectly()
    {
        var query = "select 'Cześć, Привет, こんにちは' from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [new BasicEntity("test")]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Cześć, Привет, こんにちは", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenEmptyStringComparedWithMultilingualText_ShouldFilterCorrectly()
    {
        var query = "select Name from #A.entities() where Name <> ''";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity(""),
                    new BasicEntity("東京"),
                    new BasicEntity(""),
                    new BasicEntity("Москва")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "東京"),
            "Row with '東京' not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Москва"),
            "Row with 'Москва' not found");
    }

    [TestMethod]
    public void WhenSpanishTextWithSpecialChars_ShouldHandleCorrectly()
    {
        var query = "select Name from #A.entities() where Name = 'Español'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Español"),
                    new BasicEntity("Niño"),
                    new BasicEntity("Señor")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Español", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenPortugueseTextWithTildes_ShouldHandleCorrectly()
    {
        var query = "select Name from #A.entities() where Name like '%ção%'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Programação"),
                    new BasicEntity("São Paulo"),
                    new BasicEntity("Atenção")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Programação"),
            "Row with 'Programação' not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Atenção"),
            "Row with 'Atenção' not found");
    }

    [TestMethod]
    public void WhenCzechTextWithHacekAndCarek_ShouldHandleCorrectly()
    {
        var query = "select Name from #A.entities() where Name = 'Příliš žluťoučký kůň'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Příliš žluťoučký kůň"),
                    new BasicEntity("Praha"),
                    new BasicEntity("Brno")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Příliš žluťoučký kůň", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenRomanianTextWithDiacritics_ShouldHandleCorrectly()
    {
        var query = "select Name from #A.entities() where Name = 'București'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("București"),
                    new BasicEntity("Iași"),
                    new BasicEntity("Timișoara")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("București", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenNordicTextWithSpecialChars_ShouldHandleCorrectly()
    {
        var query = "select Name from #A.entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Ångström"),
                    new BasicEntity("Malmö"),
                    new BasicEntity("Ærø"),
                    new BasicEntity("Ísland")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);

        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Ångström"),
            "Row 'Ångström' not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Malmö"),
            "Row 'Malmö' not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Ærø"),
            "Row 'Ærø' not found");
        Assert.IsTrue(table.Any(row => (string)row.Values[0] == "Ísland"),
            "Row 'Ísland' not found");
    }

    #endregion
}
