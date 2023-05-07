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
        const string query = "desc #dynamic.all()";
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
        const string query = "select Id, Name from #dynamic.all()";
        var sources =
            new List<dynamic>()
            {
                CreateExpandoObject(1, "Test1"),
                CreateExpandoObject(2, "Test2")
            };
        
        var vm = CreateAndRunVirtualMachine(query, sources);
        
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
        
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(1, table[0][0]);
        Assert.AreEqual("Test1", table[0][1]);
        Assert.AreEqual(2, table[1][0]);
        Assert.AreEqual("Test2", table[1][1]);
    }

    [TestMethod]
    public void WithDynamicSource_DescDynamicObjectWithComplexColumns_ShouldPass()
    {
        const string query = "desc #dynamic.all()";
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
        const string query = "select Complex.Id, Complex.Name from #dynamic.all()";
        var sources = new List<dynamic>()
        {
            CreateExpandoObject(CreateExpandoObject(1, "Test1"))
        };
        
        var vm = CreateAndRunVirtualMachine(query, sources);
        
        var table = vm.Run();
        
        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual(typeof(object), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual(typeof(object), table.Columns.ElementAt(1).ColumnType);
        
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1, table[0][0]);
        Assert.AreEqual("Test1", table[0][1]);
    }
    
    private static ExpandoObject CreateExpandoObject(ExpandoObject complex)
    {
        dynamic obj = new ExpandoObject();
        obj.Complex = complex;
        return obj;
    }
    
    private static ExpandoObject CreateExpandoObject(int id, string name)
    {
        dynamic obj = new ExpandoObject();
        obj.Id = id;
        obj.Name = name;
        return obj;
    }
}