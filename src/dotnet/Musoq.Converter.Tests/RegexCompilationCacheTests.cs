using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class RegexCompilationCacheTests
{
    [TestMethod]
    public void ExecutionCache_WhenMatchBudgetChanges_ShouldCompileSeparateArtifacts()
    {
        var query = $"select d.Dummy rlike r'd.mmy' from #system.dual() d where d.Dummy = '{Guid.NewGuid():N}'";
        var provider = new SystemSchemaProvider();
        var first = new CompilationOptions().WithRegexMatchTimeout(TimeSpan.FromSeconds(1));
        var second = first.WithRegexMatchTimeout(TimeSpan.FromSeconds(3));
        var logger = new TestsLoggerResolver();
        using var firstQuery = InstanceCreator.CompileForExecution(query, "RegexBudgetCacheFirst", provider, logger, first);

        Assert.IsTrue(InstanceCreator.HasExecutionCompilationCacheEntryForTests(query, provider, first));
        Assert.IsFalse(InstanceCreator.HasExecutionCompilationCacheEntryForTests(query, provider, second));

        using var secondQuery = InstanceCreator.CompileForExecution(query, "RegexBudgetCacheSecond", provider, logger, second);

        Assert.IsTrue(InstanceCreator.HasExecutionCompilationCacheEntryForTests(query, provider, second));
    }
}
