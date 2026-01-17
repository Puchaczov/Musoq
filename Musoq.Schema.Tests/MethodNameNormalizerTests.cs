using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Helpers;

namespace Musoq.Schema.Tests;

/// <summary>
///     Unit tests for <see cref="MethodNameNormalizer" />.
///     Verifies normalization behavior and caching functionality.
/// </summary>
[TestClass]
public class MethodNameNormalizerTests
{
    #region Basic Normalization Tests

    [TestMethod]
    public void Normalize_WhenLowercase_ShouldReturnSameName()
    {
        var result = MethodNameNormalizer.Normalize("mymethod");

        Assert.AreEqual("mymethod", result);
    }

    [TestMethod]
    public void Normalize_WhenUppercase_ShouldReturnLowercase()
    {
        var result = MethodNameNormalizer.Normalize("MYMETHOD");

        Assert.AreEqual("mymethod", result);
    }

    [TestMethod]
    public void Normalize_WhenMixedCase_ShouldReturnLowercase()
    {
        var result = MethodNameNormalizer.Normalize("MyMethodName");

        Assert.AreEqual("mymethodname", result);
    }

    [TestMethod]
    public void Normalize_WhenContainsUnderscores_ShouldRemoveUnderscores()
    {
        var result = MethodNameNormalizer.Normalize("my_method_name");

        Assert.AreEqual("mymethodname", result);
    }

    [TestMethod]
    public void Normalize_WhenMixedCaseWithUnderscores_ShouldNormalize()
    {
        var result = MethodNameNormalizer.Normalize("My_Method_Name");

        Assert.AreEqual("mymethodname", result);
    }

    [TestMethod]
    public void Normalize_WhenAlreadyNormalized_ShouldReturnSameInstance()
    {
        var result1 = MethodNameNormalizer.Normalize("alreadynormalized");
        var result2 = MethodNameNormalizer.Normalize("alreadynormalized");

        Assert.AreEqual("alreadynormalized", result1);
        Assert.AreSame(result1, result2, "Should return same cached instance");
    }

    #endregion

    #region Caching Behavior Tests

    [TestMethod]
    public void Normalize_WhenCalledMultipleTimes_ShouldReturnConsistentResults()
    {
        const string input = "Test_Method_Name";

        var result1 = MethodNameNormalizer.Normalize(input);
        var result2 = MethodNameNormalizer.Normalize(input);
        var result3 = MethodNameNormalizer.Normalize(input);

        Assert.AreEqual(result1, result2);
        Assert.AreEqual(result2, result3);
        Assert.AreEqual("testmethodname", result1);
    }

    [TestMethod]
    public void Normalize_DifferentInputs_ShouldProduceDifferentOutputs()
    {
        var result1 = MethodNameNormalizer.Normalize("MethodA");
        var result2 = MethodNameNormalizer.Normalize("MethodB");

        Assert.AreNotEqual(result1, result2);
        Assert.AreEqual("methoda", result1);
        Assert.AreEqual("methodb", result2);
    }

    [TestMethod]
    public void Normalize_SameLogicalName_DifferentFormats_ShouldProduceSameResult()
    {
        var camelCase = MethodNameNormalizer.Normalize("myMethodName");
        var pascalCase = MethodNameNormalizer.Normalize("MyMethodName");
        var snakeCase = MethodNameNormalizer.Normalize("my_method_name");
        var upperSnake = MethodNameNormalizer.Normalize("MY_METHOD_NAME");
        var allLower = MethodNameNormalizer.Normalize("mymethodname");

        Assert.AreEqual("mymethodname", camelCase);
        Assert.AreEqual("mymethodname", pascalCase);
        Assert.AreEqual("mymethodname", snakeCase);
        Assert.AreEqual("mymethodname", upperSnake);
        Assert.AreEqual("mymethodname", allLower);
    }

    #endregion

    #region Concurrent Access Tests

    [TestMethod]
    public void Normalize_WhenCalledConcurrently_ShouldReturnConsistentResults()
    {
        const string input = "ConcurrentTestMethod";
        const int iterations = 1000;
        var results = new string[iterations];

        Parallel.For(0, iterations, i => { results[i] = MethodNameNormalizer.Normalize(input); });

        foreach (var result in results) Assert.AreEqual("concurrenttestmethod", result);
    }

    [TestMethod]
    public void Normalize_WhenManyDifferentInputsConcurrently_ShouldHandleCorrectly()
    {
        const int distinctInputs = 100;
        const int iterations = 10;
        var inputs = new List<string>();

        for (var i = 0; i < distinctInputs; i++) inputs.Add($"Method_{i}_Name");

        var results = new Dictionary<string, string>();
        var lockObj = new object();

        Parallel.For(0, distinctInputs * iterations, i =>
        {
            var input = inputs[i % distinctInputs];
            var result = MethodNameNormalizer.Normalize(input);

            lock (lockObj)
            {
                if (results.TryGetValue(input, out var existing))
                    Assert.AreEqual(existing, result, $"Inconsistent result for input '{input}'");
                else
                    results[input] = result;
            }
        });

        Assert.HasCount(distinctInputs, results.Keys);
    }

    #endregion

    #region Edge Cases and Error Handling

    [TestMethod]
    public void Normalize_WhenNull_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => MethodNameNormalizer.Normalize(null!));
    }

    [TestMethod]
    public void Normalize_WhenEmpty_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => MethodNameNormalizer.Normalize(""));
    }

    [TestMethod]
    public void Normalize_WhenWhitespace_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => MethodNameNormalizer.Normalize("   "));
    }

    [TestMethod]
    public void Normalize_WhenSingleCharacter_ShouldNormalize()
    {
        Assert.AreEqual("a", MethodNameNormalizer.Normalize("A"));
        Assert.AreEqual("a", MethodNameNormalizer.Normalize("a"));
        Assert.AreEqual("x", MethodNameNormalizer.Normalize("X"));
    }

    [TestMethod]
    public void Normalize_WhenContainsNumbers_ShouldPreserveNumbers()
    {
        var result = MethodNameNormalizer.Normalize("Method123Name");

        Assert.AreEqual("method123name", result);
    }

    [TestMethod]
    public void Normalize_WhenContainsSpecialCharacters_ShouldPreserveNonUnderscoreSpecials()
    {
        var result = MethodNameNormalizer.Normalize("method$name");

        Assert.AreEqual("method$name", result);
    }

    [TestMethod]
    public void Normalize_WhenMultipleConsecutiveUnderscores_ShouldRemoveAll()
    {
        var result = MethodNameNormalizer.Normalize("my___method___name");

        Assert.AreEqual("mymethodname", result);
    }

    [TestMethod]
    public void Normalize_WhenStartsWithUnderscore_ShouldRemove()
    {
        var result = MethodNameNormalizer.Normalize("_privateMethod");

        Assert.AreEqual("privatemethod", result);
    }

    [TestMethod]
    public void Normalize_WhenEndsWithUnderscore_ShouldRemove()
    {
        var result = MethodNameNormalizer.Normalize("method_");

        Assert.AreEqual("method", result);
    }

    #endregion
}