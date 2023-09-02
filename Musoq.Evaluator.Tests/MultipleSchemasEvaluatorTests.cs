using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Multi;
using Musoq.Evaluator.Tests.Schema.Multi.First;
using Musoq.Evaluator.Tests.Schema.Multi.Second;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class MultipleSchemasEvaluatorTests : MultiQueryTestBase
{
    [TestMethod]
    public void WhenCompilerMustDecideWhichOneOfTheMethodsUse_ShouldChoseTheFirstOne()
    {
        const string query = "select first.MethodA() from #schema.first() first inner join #schema.second() second on 1 = 1";
        
        var vm = CreateAndRunVirtualMachine(query, new FirstEntity[]
        {
            new()
        }, new SecondEntity[]
        {
            new()
        });
        
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Count);
        
        Assert.AreEqual(0, table[0].Values[0]);
    }
    
    [TestMethod]
    public void WhenCompilerMustDecideWhichOneOfTheMethodsUse_ShouldChoseTheSecondOne()
    {
        const string query = "select second.MethodA() from #schema.first() first inner join #schema.second() second on 1 = 1";
        
        var vm = CreateAndRunVirtualMachine(query, new FirstEntity[]
        {
            new()
        }, new SecondEntity[]
        {
            new()
        });
        
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Count);
        
        Assert.AreEqual(1, table[0].Values[0]);
    }
    
    [TestMethod]
    public void WhenCompilerMustDecideWhichOneOfTheMethodsUse_ShouldChoseFirstOne()
    {
        const string query = "select first.MethodA() from #schema.second() second inner join #schema.first() first on 1 = 1";
        
        var vm = CreateAndRunVirtualMachine(query, new FirstEntity[]
        {
            new()
        }, new SecondEntity[]
        {
            new()
        });
        
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Count);
        
        Assert.AreEqual(0, table[0].Values[0]);
    }
    
    [TestMethod]
    public void WhenCompilerMustDecideWhichOneOfTheMethodsUse_TheMethodHasAdditionalArgument_ShouldChoseTheSecondOne()
    {
        const string query = "select second.MethodB('abc') from #schema.second() second inner join #schema.first() first on 1 = 1";
        
        var vm = CreateAndRunVirtualMachine(query, new FirstEntity[]
        {
            new()
        }, new SecondEntity[]
        {
            new()
        });
        
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Count);
        
        Assert.AreEqual(1, table[0].Values[0]);
    }
    
    [TestMethod]
    public void WhenCompilerMustDecideWhichOneOfTheMethodsUse_TheMethodHasAdditionalArgument_ShouldChoseFirstOne()
    {
        const string query = "select first.MethodB('abc') from #schema.first() first inner join #schema.second() second on 1 = 1";
        
        var vm = CreateAndRunVirtualMachine(query, new FirstEntity[]
        {
            new()
        }, new SecondEntity[]
        {
            new()
        });
        
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Count);
        
        Assert.AreEqual(0, table[0].Values[0]);
    }

    [TestMethod]
    public void WhenMultipleInjectsWithinMethod_ShouldNotThrow()
    {
        const string query = "select AggregateMethodA() from #schema.first()";
        
        var vm = CreateAndRunVirtualMachine(query, new FirstEntity[]
        {
            new()
        }, Array.Empty<SecondEntity>());
        
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Count);
    }
    
    [TestMethod]
    public void WhenInjectingEntityUsesCommonInterfaceWithMethod_ShouldMatchMethodAndCall()
    {
        const string query = "select MethodC() from #schema.first()";
        
        var vm = CreateAndRunVirtualMachine(query, new FirstEntity[]
        {
            new()
        }, Array.Empty<SecondEntity>());
        
        var table = vm.Run();
        
        Assert.AreEqual(1, table.Count);
        
        Assert.AreEqual(5, table[0].Values[0]);
    }
}