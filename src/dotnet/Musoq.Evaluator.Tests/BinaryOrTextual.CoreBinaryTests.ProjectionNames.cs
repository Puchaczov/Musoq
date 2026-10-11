using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ComponentBinaryEntity = Musoq.Evaluator.Tests.Components.BinaryEntity;
using ComponentBinarySchemaProvider = Musoq.Evaluator.Tests.Components.BinarySchemaProvider;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualCoreBinaryTests
{
    [TestMethod]
    [TestProperty("CandidateId", "PROMOTED-AppliedBinaryFieldNameOmitsItsSourceQualifier")]
    public void AppliedBinaryFieldNameOmitsItsSourceQualifier()
    {
        AssertAppliedBinaryProjectionName("Copied");
    }

    [TestMethod]
    public void AppliedBinaryField_CurrentProjectionNameRetainsByteMetadata()
    {
        AssertAppliedBinaryProjectionName("Copied");
    }

    private void AssertAppliedBinaryProjectionName(string name)
    {
        const string query = "binary NamePacket { Copied: byte } select p.Copied from #test.entities() e cross apply Interpret<NamePacket>(e.Content) p";
        var sources = new Dictionary<string, IEnumerable<ComponentBinaryEntity>>
        {
            ["#test"] = Array.Empty<ComponentBinaryEntity>()
        };
        using var compiled = CompileGeneratedQuery(query, Guid.NewGuid().ToString(),
            new ComponentBinarySchemaProvider(sources), LoggerResolver, TestCompilationOptions);
        using var result = compiled.Run();
        TableMaterializationTestHelper.AssertColumns(result, (name, typeof(byte)));
        Assert.AreEqual(0, result.Count);
    }
}
