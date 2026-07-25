using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Build;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class SemanticPhaseArtifactTests
{
    [TestMethod]
    public void TransformTree_ShouldPreserveEverySemanticPhaseInOneArtifact()
    {
        var items = InstanceCreator.CreateForAnalyze(
            "select 1 as Value from #system.dual()",
            Guid.NewGuid().ToString(),
            new SystemSchemaProvider(),
            new TestsLoggerResolver());

        var phase = items.SemanticArtifacts.Phase;

        Assert.IsNotNull(phase.ParsedQuery);
        Assert.IsNotNull(phase.NormalizedQuery);
        Assert.IsNotNull(phase.MetadataQuery);
        Assert.IsNotNull(phase.RewrittenQuery);
        Assert.AreSame(phase.RewrittenQuery, items.SemanticArtifacts.TransformedQueryTree);
        Assert.IsNotNull(phase.Metadata);
        Assert.IsNotNull(phase.ResultShape);
    }
}
