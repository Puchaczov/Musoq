using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Helpers;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class StringHelpersTests
{
    [TestMethod]
    public void GenerateNamespaceIdentifier_WhenCalledMultipleTimes_ShouldReturnIncreasingValues()
    {
        var first = StringHelpers.GenerateNamespaceIdentifier();
        var second = StringHelpers.GenerateNamespaceIdentifier();
        var third = StringHelpers.GenerateNamespaceIdentifier();
        
        Assert.IsGreaterThan(first, second, "Second should be greater than first");
        Assert.IsGreaterThan(second, third, "Third should be greater than second");
    }

    [TestMethod]
    public void GenerateNamespaceIdentifier_WhenCalledConcurrently_ShouldReturnUniqueValues()
    {
        const int iterations = 10000;
        var results = new ConcurrentBag<long>();
        
        Parallel.For(0, iterations, _ =>
        {
            results.Add(StringHelpers.GenerateNamespaceIdentifier());
        });
        
        var distinctCount = results.Distinct().Count();
        Assert.AreEqual(iterations, distinctCount, 
            $"Expected {iterations} unique values but got {distinctCount}. Concurrent access produced duplicates.");
    }

    [TestMethod]
    public void GenerateNamespaceIdentifier_WhenCalledFromMultipleThreads_ShouldBeThreadSafe()
    {
        const int threadCount = 8;
        const int iterationsPerThread = 1000;
        var results = new ConcurrentBag<long>();
        
        var tasks = Enumerable.Range(0, threadCount)
            .Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < iterationsPerThread; i++)
                {
                    results.Add(StringHelpers.GenerateNamespaceIdentifier());
                }
            }))
            .ToArray();
        
        Task.WaitAll(tasks);
        
        var distinctCount = results.Distinct().Count();
        var expectedCount = threadCount * iterationsPerThread;
        
        Assert.AreEqual(expectedCount, distinctCount,
            $"Expected {expectedCount} unique values but got {distinctCount}. Thread safety violation detected.");
    }

    [TestMethod]
    public void GenerateNamespaceIdentifier_HighContention_ShouldNotLoseIncrements()
    {
        const int iterations = 50000;
        var results = new ConcurrentBag<long>();
        
        var options = new ParallelOptions 
        { 
            MaxDegreeOfParallelism = System.Environment.ProcessorCount 
        };
        
        Parallel.For(0, iterations, options, _ =>
        {
            results.Add(StringHelpers.GenerateNamespaceIdentifier());
        });
        
        var distinctCount = results.Distinct().Count();
        Assert.AreEqual(iterations, distinctCount,
            $"Under high contention: expected {iterations} unique values but got {distinctCount}");
    }
}
