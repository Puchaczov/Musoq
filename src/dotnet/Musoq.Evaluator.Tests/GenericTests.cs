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
    public void WhenConcatenationOfSingleValue_AndStringJoinsEnumerable_ShouldPass()
    {
        var table = TestResultMethodTemplate(
            "Concat(StringsJoin('/', Take(Split('something/is/wrong/here', '/'), 3)))");

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("something/is/wrong", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenConcatenationOfSingleValue_AndStringJoinsArray_ShouldPass()
    {
        var table = TestResultMethodTemplate(
            "Concat(StringsJoin('/', EnumerableToArray(Take(Split('something/is/wrong/here', '/'), 3))))");

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("something/is/wrong", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenSkipAndTakeWithoutReductionRequired_ShouldPass()
    {
        var table = TestResultMethodTemplate("EnumerableToArray(SkipAndTake(GetBytes('test1'), 1, 1))");

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("e", Encoding.UTF8.GetString((byte[])table[0].Values[0]));
    }

    [TestMethod]
    public void WhenInferredValueIsNull_ShouldUseDefaultValue()
    {
        var table = TestResultMethodTemplate("GetCountryOrDefault('test')");

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("test", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenResolvedMethodIsGenericWithInjectSpecificSourceAttribute_ShouldConcretizeItAccordingly()
    {
        var table = TestResultMethodTemplate("GetCountryOrDefaultGeneric('test')");

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("test", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenToJsonGenericMethodCall_ShouldReturnCorrectJson()
    {
        var table = TestResultMethodTemplate("ToJson('test')");

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("\"test\"", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenToJsonGenericMethodCallWithArray_ShouldReturnCorrectJson()
    {
        var table = TestResultMethodTemplate("ToJson(1)");

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("1", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenToJsonGenericMethodCallWithObject_ShouldReturnCorrectJson()
    {
        var table = TestResultMethodTemplate("ToJson(ToTimeSpan('00:11:22'))");

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("\"00:11:22\"", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenFirstElementLookedFor_ShouldSuccess()
    {
        var table = TestResultMethodTemplate("FirstOrDefault(Split('a/b/c', '/'))");

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("a", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenNthElementLookedFor_ShouldSuccess()
    {
        var table = TestResultMethodTemplate("NthOrDefault(Split('a/b/c', '/'), 1)");

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("b", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenLastElementLookedFor_ShouldSuccess()
    {
        var table = TestResultMethodTemplate("LastOrDefault(Split('a/b/c', '/'))");

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("c", table[0].Values[0]);
    }

    [TestMethod]
    public void WhenNthElementFromEndLookedFor_ShouldSuccess()
    {
        var table = TestResultMethodTemplate("NthFromEndOrDefault(Split('a/b/c', '/'), 1)");

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("b", table[0].Values[0]);
    }
}
