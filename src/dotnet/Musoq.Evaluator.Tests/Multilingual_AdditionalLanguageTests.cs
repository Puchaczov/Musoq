using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class MultilingualTextTests
{
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
}
