using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class MultilingualTextTests
{
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
