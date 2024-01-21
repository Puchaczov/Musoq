using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class GenericTests : BasicEntityTestBase
{
    [TestMethod]
    public void WhenMergeArraysButSingleArrayPassed_ShouldChoseCorrectImplementation()
    {
        var table = TestResultMethodTemplate("MergeArrays(GetBytes('test1'))");
        
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("test1", Encoding.UTF8.GetString((byte[])table[0].Values[0]));
    }
    
    [TestMethod]
    public void WhenMergeArraysWithTwoArraysPassed_ShouldChoseCorrectImplementation()
    {
        var table = TestResultMethodTemplate("MergeArrays(GetBytes('test1'), GetBytes('test2'))");
        
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("test1test2", Encoding.UTF8.GetString((byte[])table[0].Values[0]));
    }
    
    [TestMethod]
    public void WhenMergeArraysWithThreeArraysPassed_ShouldChoseCorrectImplementation()
    {
        var table = TestResultMethodTemplate("MergeArrays(GetBytes('test1'), GetBytes('test2'), GetBytes('test3'))");
        
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("test1test2test3", Encoding.UTF8.GetString((byte[])table[0].Values[0]));
    }
    
    [TestMethod]
    public void WhenEnumerableChangesToArray_ShouldPass()
    {
        var table = TestResultMethodTemplate("EnumerableToArray(GetBytes('test1'))");
        
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("test1", Encoding.UTF8.GetString((byte[])table[0].Values[0]));
    }
    
    [TestMethod]
    public void WhenIndistinctEnumerableChangesToArray_ShouldPass()
    {
        var table = TestResultMethodTemplate("EnumerableToArray('test1')");
        
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("test1", string.Join(string.Empty, (char[])table[0].Values[0]));
    }
    
    [TestMethod]
    public void WhenSkipFirstElement_ShouldReturnEverythingElse()
    {
        var table = TestResultMethodTemplate("Skip('test1', 1)");
        
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("est1", string.Join(string.Empty, (IEnumerable<char>)table[0].Values[0]));
    }
    
    [TestMethod]
    public void WhenTakeFirstElement_ShouldReturnOnlyFirstLetter()
    {
        var table = TestResultMethodTemplate("Take('test1', 1)");
        
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("t", string.Join(string.Empty, (IEnumerable<char>)table[0].Values[0]));
    }
    
    [TestMethod]
    public void WhenSkipFirstLetterAndTakeAnotherLetter_ShouldReturnSecondLetter()
    {
        var table = TestResultMethodTemplate("SkipAndTake('test1', 1, 1)");
        
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("e", string.Join(string.Empty, (IEnumerable<char>)table[0].Values[0]));
    }
    
    [TestMethod]
    public void WhenDetectLongestCommonSequence_ShouldReturnCorrectOne()
    {
        var table = TestResultMethodTemplate("LongestCommonSequence('test1', 'test2')");
        
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("test", string.Join(string.Empty, (IEnumerable<char>)table[0].Values[0]));
    }
    
    [TestMethod]
    public void WhenGetElementAtOrDefaultForExistingElement_ShouldPass()
    {
        var table = TestResultMethodTemplate("GetElementAtOrDefault('test1', 1)");
        
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual('e', (char)table[0].Values[0]);
    }
    
    [TestMethod]
    public void WhenGetElementAtOrDefaultForNonExistingElement_ShouldPass()
    {
        var table = TestResultMethodTemplate("GetElementAtOrDefault('test1', 100)");
        
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual('\0', (char?)table[0].Values[0]);
    }
    
    [TestMethod]
    public void WhenRetrieveLengthFromSupportedType_ShouldPass()
    {
        var table = TestResultMethodTemplate("Length('test1')");
        
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(5, (int)table[0].Values[0]);
    }
    
    [TestMethod]
    public void WhenRetrieveLengthFromArrayType_ShouldPass()
    {
        var table = TestResultMethodTemplate("Length(GetBytes('test1'))");
        
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(5, (int)table[0].Values[0]);
    }
    
    [TestMethod]
    public void WhenSkipWithoutReductionRequired_ShouldPass()
    {
        var table = TestResultMethodTemplate("EnumerableToArray(Skip(GetBytes('test1'), 1))");
        
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("est1", Encoding.UTF8.GetString((byte[])table[0].Values[0]));
    }
    
    [TestMethod]
    public void WhenTakeWithoutReductionRequired_ShouldPass()
    {
        var table = TestResultMethodTemplate("EnumerableToArray(Take(GetBytes('test1'), 1))");
        
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("t", Encoding.UTF8.GetString((byte[])table[0].Values[0]));
    }
    
    [TestMethod]
    public void WhenSkipAndTakeWithoutReductionRequired_ShouldPass()
    {
        var table = TestResultMethodTemplate("EnumerableToArray(SkipAndTake(GetBytes('test1'), 1, 1))");
        
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("e", Encoding.UTF8.GetString((byte[])table[0].Values[0]));
    }
}