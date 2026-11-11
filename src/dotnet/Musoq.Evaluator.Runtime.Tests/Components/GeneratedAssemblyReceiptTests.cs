using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests.Components;

[TestClass]
public sealed class GeneratedAssemblyReceiptTests
{
    [TestMethod]
    public void CachedCompilation_RetainsTheExecutableReceiptWithoutAnotherAssemblyLoad()
    {
        using var capture = new GeneratedAssemblyCapture();
        var provider = new BasicSchemaProvider<BasicEntity>(new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity("first") { Id = 1 }]
        });
        var logger = new TestsLoggerResolver();
        var options = new CompilationOptions(usePrimitiveTypeValidation: false);
        var sql = $"select Sum(e.Id) as Receipt{Guid.NewGuid():N} from #A.Entities() e";
        var first = InstanceCreator.CompileWithDiagnostics(sql, "ReceiptCold", provider, logger, options);
        Assert.IsTrue(first.Succeeded);
        using var firstQuery = first.CompiledQuery;
        GeneratedExecutionIlInspection.RecordCompilation(first, capture);
        var firstType = GeneratedExecutionIlInspection.GetGeneratedComputeMethods(firstQuery).First().DeclaringType;
        Assert.IsTrue(InstanceCreator.HasExecutionCompilationCacheEntryForTests(sql, provider, options));

        var cached = InstanceCreator.CompileWithDiagnostics(sql, "ReceiptCached", provider, logger, options);
        Assert.IsTrue(cached.Succeeded);
        using var cachedQuery = cached.CompiledQuery;
        GeneratedExecutionIlInspection.RecordCompilation(cached, capture);

        Assert.IsTrue(GeneratedExecutionIlInspection.GetGeneratedComputeMethods(cachedQuery).Any());
        Assert.IsInstanceOfType<ClrLoadedExecutableArtifact>(cached.BuildItems!.ExecutableArtifact);
        Assert.AreSame(firstType, GeneratedExecutionIlInspection.GetGeneratedComputeMethods(cachedQuery).First().DeclaringType);
    }

    [TestMethod]
    public void BatchedCompilation_RetainsEachExactGeneratedTypeForIlInspection()
    {
        using var capture = new GeneratedAssemblyCapture();
        var provider = new BasicSchemaProvider<BasicEntity>(new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity("first") { Id = 1 }]
        });
        var logger = new TestsLoggerResolver();
        var options = new CompilationOptions(usePrimitiveTypeValidation: false);
        var results = InstanceCreator.CompileForExecutionBatch(
        [
            new("first", "select e.Id from #A.Entities() e", "ReceiptFirst", provider, logger, options),
            new("second", "select e.Id + 1 from #A.Entities() e", "ReceiptSecond", provider, logger, options)
        ]);
        Assert.HasCount(2, results);
        AssertReceipt(results[0].Result, capture);
        AssertReceipt(results[1].Result, capture);
    }

    private static void AssertReceipt(BuildResult result, GeneratedAssemblyCapture capture)
    {
        Assert.IsTrue(result.Succeeded);
        using var query = result.CompiledQuery;
        GeneratedExecutionIlInspection.RecordCompilation(result, capture);
        Assert.IsTrue(GeneratedExecutionIlInspection.GetGeneratedComputeMethods(query).Any());
    }
}
