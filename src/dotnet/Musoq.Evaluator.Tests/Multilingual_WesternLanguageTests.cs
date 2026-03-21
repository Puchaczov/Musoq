using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public partial class MultilingualTextTests : BasicEntityTestBase
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
}
