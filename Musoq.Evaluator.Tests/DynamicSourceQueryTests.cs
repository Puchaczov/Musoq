using System.Collections.Generic;
using System.Dynamic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Dynamic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class DynamicSourceQueryTests : DynamicQueryTestsBase
{
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
        
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(1, table[0][0]);
        Assert.AreEqual("Test1", table[0][1]);
        Assert.AreEqual(2, table[1][0]);
        Assert.AreEqual("Test2", table[1][1]);
    }

    [TestMethod]
    public void WithDynamicSource_AccessComplexObjectProperties_ShouldPass()
    {
        const string query = "select Complex.Id, Complex.Name from #dynamic.all()";
        var sources =
            new List<dynamic>()
            {
                CreateExpandoObject(CreateExpandoObject(1, "Test1")),
            };
        
        var vm = CreateAndRunVirtualMachine(query, sources);
        
        var table = vm.Run();
        
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