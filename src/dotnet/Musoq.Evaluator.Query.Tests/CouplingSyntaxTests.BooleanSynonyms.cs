using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;

namespace Musoq.Evaluator.Tests;

public partial class CouplingSyntaxTests
{
    private const string CapturedBoolQuery = "table BoolRows { Count: int, Enabled: bool }; couple #specstressnamedmix.capture with table BoolRows as Captured; select c.Enabled as Enabled from Captured(7, true) c";
    private const string CapturedBooleanQuery = "table BoolRows { Count: int, Enabled: boolean }; couple #specstressnamedmix.capture with table BoolRows as Captured; select c.Enabled as Enabled from Captured(7, true) c";
    private const string CapturedBitQuery = "table BoolRows { Count: int, Enabled: bit }; couple #specstressnamedmix.capture with table BoolRows as Captured; select c.Enabled as Enabled from Captured(7, true) c";

    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P5-0010")]
    public void Current_P5_0010_BooleanTableSynonymsMatchProviderType()
    {
        AssertCapturedBooleanSynonyms(typeof(bool));
    }

    [TestMethod]
    public void BooleanTableSynonyms_CurrentTypedProviderMetadataRetainsTrueValue()
    {
        AssertCapturedBooleanSynonyms(typeof(bool));
    }

    private void AssertCapturedBooleanSynonyms(Type type)
    {
        AssertCapturedBooleanQuery(CapturedBoolQuery, type);
        AssertCapturedBooleanQuery(CapturedBooleanQuery, type);
        AssertCapturedBooleanQuery(CapturedBitQuery, type);
    }

    private void AssertCapturedBooleanQuery(string query, Type type)
    {
        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            new SpecStressNamedMixSchemaProvider(), LoggerResolver, TestCompilationOptions);
        using var result = compiled.Run();
        TableMaterializationTestHelper.AssertColumns(result, ("Enabled", type));
        TableMaterializationTestHelper.AssertRowsUnordered(result, new object?[] { true });
    }
}
