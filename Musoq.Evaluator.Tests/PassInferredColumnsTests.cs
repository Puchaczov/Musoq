using System.Dynamic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Unknown;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class PassInferredColumnsTests : UnknownQueryTestsBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void PassInferredColumnsTest()
    {
        var query = "select Name, Surname, ContactNumber from #test.whatever() where Country = 'Poland'";

        dynamic first = new ExpandoObject();
        first.Name = "Roland";
        first.Surname = "Muchowski";
        first.Country = "Poland";
        first.ContactNumber = "123456789";

        dynamic second = new ExpandoObject();
        second.Name = "John";
        second.Surname = "Doe";
        second.Country = "USA";
        second.ContactNumber = "987654321";

        var vm = CreateAndRunVirtualMachine(query,
        [
            first, second
        ]);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Columns.Count());
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(object), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("Surname", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(object), table.Columns.ElementAt(1).ColumnType);
        Assert.AreEqual("ContactNumber", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual(typeof(object), table.Columns.ElementAt(2).ColumnType);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Roland", table[0].Values[0]);
        Assert.AreEqual("Muchowski", table[0].Values[1]);
        Assert.AreEqual("123456789", table[0].Values[2]);
    }

    [TestMethod]
    public void PassInferredColumnsBasedOnCouplingSyntaxTest()
    {
        const string query = "table Persons {" +
                             "   Name 'System.String'," +
                             "   Age 'System.Int32'," +
                             "   Country 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Persons as SourceOfPersons;" +
                             "select Name, Age from SourceOfPersons() where Country = 'Poland';";

        dynamic first = new ExpandoObject();
        first.Name = "Roland";
        first.Age = 21;
        first.Country = "Poland";
        first.ContactNumber = "123456789";

        dynamic second = new ExpandoObject();
        second.Name = "John";
        second.Age = 22;
        second.Country = "USA";
        second.ContactNumber = "987654321";

        var vm = CreateAndRunVirtualMachine(query,
        [
            first, second
        ]);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("Age", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(int?), table.Columns.ElementAt(1).ColumnType);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Roland", table[0].Values[0]);
        Assert.AreEqual(21, table[0].Values[1]);
    }

    [TestMethod]
    public void PassInferredColumnsWithJoinBetweenTwoCoupledTables()
    {
        const string query = "table Persons {" +
                             "   Name 'System.String'," +
                             "   Age 'System.Int32'," +
                             "   Country 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Persons as SourceOfPersons;" +
                             "select p1.Name, p1.Age, p2.Name, p2.Age from SourceOfPersons() p1 inner join SourceOfPersons() p2 on p1.Country = p2.Country " +
                             "where p1.Country = 'Poland' and p1.Name <> p2.Name;";

        dynamic first = new ExpandoObject();
        first.Name = "Roland";
        first.Age = 21;
        first.Country = "Poland";
        first.ContactNumber = "123456789";

        dynamic second = new ExpandoObject();
        second.Name = "John";
        second.Age = 22;
        second.Country = "USA";
        second.ContactNumber = "987654321";

        dynamic third = new ExpandoObject();
        third.Name = "Mateusz";
        third.Age = 22;
        third.Country = "Poland";
        third.ContactNumber = "31233133";

        var vm = CreateAndRunVirtualMachine(query,
        [
            first, second, third
        ]);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Columns.Count());
        Assert.AreEqual("p1.Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("p1.Age", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(int?), table.Columns.ElementAt(1).ColumnType);
        Assert.AreEqual("p2.Name", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(2).ColumnType);
        Assert.AreEqual("p2.Age", table.Columns.ElementAt(3).ColumnName);
        Assert.AreEqual(typeof(int?), table.Columns.ElementAt(3).ColumnType);

        Assert.AreEqual(2, table.Count);

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "Roland" &&
                (int)row.Values[1] == 21 &&
                (string)row.Values[2] == "Mateusz" &&
                (int)row.Values[3] == 22),
            "Row with Roland and Mateusz not found");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "Mateusz" &&
                (int)row.Values[1] == 22 &&
                (string)row.Values[2] == "Roland" &&
                (int)row.Values[3] == 21),
            "Row with Mateusz and Roland not found");
    }

    [TestMethod]
    public void PassInferredColumnsWithSetOfCoupledTables()
    {
        const string query = "table Persons {" +
                             "   Name 'System.String'," +
                             "   Age 'System.Int32'," +
                             "   Country 'System.String'" +
                             "};" +
                             "couple #test.whatever with table Persons as SourceOfPersons;" +
                             "select Name, Age from SourceOfPersons() where Country = 'Poland' " +
                             "union all (Name, Age) " +
                             "select Name, Age from SourceOfPersons() where Country = 'USA';";

        dynamic first = new ExpandoObject();
        first.Name = "Roland";
        first.Age = 21;
        first.Country = "Poland";
        first.ContactNumber = "123456789";

        dynamic second = new ExpandoObject();
        second.Name = "John";
        second.Age = 22;
        second.Country = "USA";
        second.ContactNumber = "987654321";

        dynamic third = new ExpandoObject();
        third.Name = "Mateusz";
        third.Age = 22;
        third.Country = "Poland";
        third.ContactNumber = "31233133";

        var vm = CreateAndRunVirtualMachine(query,
        [
            first, second, third
        ]);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("Name", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("Age", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(int?), table.Columns.ElementAt(1).ColumnType);

        Assert.AreEqual(3, table.Count, "Table should contain 3 rows");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "Roland" &&
                (int)row.Values[1] == 21),
            "Row with Roland age 21 not found");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "Mateusz" &&
                (int)row.Values[1] == 22),
            "Row with Mateusz age 22 not found");

        Assert.IsTrue(table.Any(row =>
                (string)row.Values[0] == "John" &&
                (int)row.Values[1] == 22),
            "Row with John age 22 not found");
    }
}