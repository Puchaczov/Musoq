using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Dynamic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class DynamicSourceQueryTests : DynamicQueryTestsBase
{
    [TestMethod]
    public void WithDynamicSource_DescDynamicObjectWithSimpleColumns_ShouldPass()
    {
        const string query = "desc @dynamic.all()";
        var sources =
            new List<dynamic>()
            {
                CreateExpandoObject(1, "Test1"),
                CreateExpandoObject(2, "Test2")
            };
        
        var vm = CreateAndRunVirtualMachine(query, sources);
        
        var table = vm.Run();
        
        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("Id", table[0][0]);
        Assert.AreEqual("System.Int32", table[0][2]);
        Assert.AreEqual("Name", table[1][0]);
        Assert.AreEqual("System.String", table[1][2]);
        Assert.AreEqual("Name.Chars", table[2][0]);
        Assert.AreEqual("System.Char", table[2][2]);
        Assert.AreEqual("Name.Length", table[3][0]);
        Assert.AreEqual("System.Int32", table[3][2]);
    }
    
    [TestMethod]
    public void WithDynamicSource_SimpleQuery_ShouldPass()
    {
        const string query = "select Id, Name from @dynamic.all()";
        var sources =
            new List<dynamic>
            {
                CreateExpandoObject(1, "Test1"),
                CreateExpandoObject(2, "Test2")
            };
        
        var vm = CreateAndRunVirtualMachine(query, sources);
        
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
        
        Assert.IsTrue(table.Count == 2, "Table should contain 2 rows");

        Assert.IsTrue(table.All(row => 
                new[] { (1, "Test1"), (2, "Test2") }.Contains(((int)row[0], (string)row[1]))),
            "Expected rows with values: (1,Test1), (2,Test2)");
    }

    [TestMethod]
    public void WithDynamicSource_DescDynamicObjectWithComplexColumns_ShouldPass()
    {
        const string query = "desc @dynamic.all()";
        var sources = new List<dynamic>()
        {
            CreateExpandoObject(CreateExpandoObject(1, "Test1"))
        };
        
        var vm = CreateAndRunVirtualMachine(query, sources);
        
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Complex", table[0][0]);
        Assert.AreEqual(0, (int)table[0][1]);
        Assert.AreEqual(typeof(ExpandoObject).FullName, table[0][2]);
    }

    [TestMethod]
    public void WithDynamicSource_AccessComplexObjectProperties_ShouldPass()
    {
        const string query = "select Complex.Id, Complex.Name from @dynamic.all()";
        var sources = new List<dynamic>()
        {
            CreateExpandoObject(CreateExpandoObject(1, "Test1"))
        };
        var schema = new Dictionary<string, Type>()
        {
            {"Complex", typeof(ExpandoObject)},
        };
        
        var vm = CreateAndRunVirtualMachine(query, sources, schema);
        
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual(typeof(object), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual(typeof(object), table.Columns.ElementAt(1).ColumnType);
        
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1, table[0][0]);
        Assert.AreEqual("Test1", table[0][1]);
    }

    [TestMethod]
    public void WithDynamicSource_AccessComplexChainedObjectProperties_ShouldPass()
    {
        const string query = "select Complex.Complex.Id, Complex.Complex.Name from @dynamic.all()";
        var sources = new List<dynamic>()
        {
            CreateExpandoObject(CreateExpandoObject(CreateExpandoObject(1, "Test1")))
        };
        var schema = new Dictionary<string, Type>
        {
            {"Complex", typeof(ExpandoObject)}
        };
        
        var vm = CreateAndRunVirtualMachine(query, sources, schema);
        
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual(typeof(object), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual(typeof(object), table.Columns.ElementAt(1).ColumnType);
        
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1, table[0][0]);
        Assert.AreEqual("Test1", table[0][1]);
    }

    [TestMethod]
    public void WithDynamicSource_AccessArray_ShouldPass()
    {
        const string query = "select Complex.Array[0], Complex.Array[1] from @dynamic.all()";
        var sources = new List<dynamic>()
        {
            CreateExpandoObject(CreateExpandoObject([1, 2]))
        };
        var schema = new Dictionary<string, Type>()
        {
            {"Complex", typeof(ExpandoObject)}
        };
        
        var vm = CreateAndRunVirtualMachine(query, sources, schema);
        
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual(typeof(object), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual(typeof(object), table.Columns.ElementAt(1).ColumnType);
        
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1, table[0][0]);
        Assert.AreEqual(2, table[0][1]);
    }

    [TestMethod]
    public void WithDynamicSource_AccessExpandoObjectArray_ShouldPass()
    {
        const string query = "select Complex.Array[0].Id, Complex.Array[0].Name from @dynamic.all()";
        var sources = new List<dynamic>()
        {
            CreateExpandoObject(CreateExpandoObject([
                CreateExpandoObject(1, "Test1")
            ]))
        };
        var schema = new Dictionary<string, Type>
        {
            {"Complex", typeof(ExpandoObject)}
        };
        
        var vm = CreateAndRunVirtualMachine(query, sources, schema);
        
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual(typeof(object), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual(typeof(object), table.Columns.ElementAt(1).ColumnType);
        
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1, table[0][0]);
        Assert.AreEqual("Test1", table[0][1]);
    }
    

    [TestMethod]
    public void WithDynamicSource_IncrementAccessedProperty_ShouldPass()
    {
        const string query = "select Increment(Complex.Array[0].Id) from @dynamic.all()";
        var sources = new List<dynamic>()
        {
            CreateExpandoObject(CreateExpandoObject([
                CreateExpandoObject(1)
            ]))
        };
        var schema = new Dictionary<string, Type>
        {
            {"Complex", typeof(ExpandoObject)}
        };
        
        var vm = CreateAndRunVirtualMachine(query, sources, schema);
        
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Columns.Count());
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(0).ColumnType);
        
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(2, table[0][0]);
    }

    [TestMethod]
    public void WithRedundantColumn_ShouldBeTrimmed_ShouldPass()
    {
        const string query = @"
select 
    weather.Location as 'City', 
    weather.TemperatureInCelsiusDegree as 'TemperatureInCelsiusDegree', 
    location.Name as 'Location' 
from @weather.all() weather 
inner join @location.all() location on weather.Location = location.Name
";
        dynamic weather = new ExpandoObject();
        weather.Location = "London";
        weather.TemperatureInCelsiusDegree = 10;
        var weatherSource = new List<dynamic>
        {
            weather
        };
        var weatherSchema = new Dictionary<string, Type>()
        {
            {"Name", typeof(decimal)},
            {"Location", typeof(string)},
            {"TemperatureInCelsiusDegree", typeof(int)}
        };
        
        dynamic location = new ExpandoObject();
        location.Name = "London";
        var locationSource = new List<dynamic>()
        {
            location
        };
        var locationSchema = new Dictionary<string, Type>()
        {
            {"Name", typeof(string)}
        };
        
        var vm = CreateAndRunVirtualMachine(query, [
            ("@weather", (IReadOnlyCollection<dynamic>)weatherSource, (IReadOnlyDictionary<string, Type>)weatherSchema),
            ("@location", locationSource, locationSchema)
        ]);
        
        var table = vm.Run();
        
        Assert.AreEqual(3, table.Columns.Count());
        
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("London", table[0][0]);
        Assert.AreEqual(10, table[0][1]);
        Assert.AreEqual("London", table[0][2]);
    }
    
    [TestMethod]
    public void WithDynamicSource_SyntaxKeywordAsColumnNameUSed_ShouldPass()
    {
        const string query = "select [case], [end] from @dynamic.all()";
        IDictionary<string, object> expando = new ExpandoObject();
        
        expando.Add("case", "case");
        expando.Add("end", "end");
        
        var sources =
            new List<dynamic>
            {
                (dynamic)expando
            };
        
        var vm = CreateAndRunVirtualMachine(query, sources);
        
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
        
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("case", table[0][0]);
        Assert.AreEqual("end", table[0][1]);
    }
}