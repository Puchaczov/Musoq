using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class ResidualArchitectureWave16GuardrailTests
{
    [TestMethod]
    public void OperatorsPatternCaches_ShouldUseBoundedRuntimeCacheAndRegexTimeouts()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var operatorsPath = Path.Combine(
            repositoryRoot,
            "src", "dotnet", "Musoq.Evaluator", "Operators.cs");
        var operatorsText = File.ReadAllText(operatorsPath);

        Assert.DoesNotContain("ConcurrentDictionary<string", operatorsText);
        Assert.Contains("BoundedRuntimeCache<string, Func<string, bool>> LikeMatcherCache", operatorsText);
        Assert.Contains("BoundedRuntimeCache<string, Regex> RLikePatternCache", operatorsText);
        Assert.Contains("RuntimeCacheOptions.DefaultRegexTimeout", operatorsText);
    }
}
