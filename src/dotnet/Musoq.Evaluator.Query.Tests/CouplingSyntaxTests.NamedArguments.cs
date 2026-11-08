using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Tests;

public partial class CouplingSyntaxTests
{
    private const string PositionalCapturedQuery = "table CapturedRows { Count: int, Enabled: bool }; couple #specstressnamedmix.capture with table CapturedRows as Captured; select c.Count as ReceivedCount, c.Enabled from Captured(7, true) c";
    private const string NamedCapturedQuery = "table CapturedRows { Count: int, Enabled: bool }; couple #specstressnamedmix.capture with table CapturedRows as Captured; select c.Count as ReceivedCount, c.Enabled from Captured(count: 7, enabled: true) c";

    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P5-0008")]
    public void Current_P5_0008_CoupledNamedArgumentsMatchPositionalForms()
    {
        AssertNamedCapturedForms("Enabled", typeof(int), typeof(bool));
    }

    [TestMethod]
    public void CoupledNamedArguments_CurrentTypedMetadataMatchesPositionalValues()
    {
        AssertNamedCapturedForms("Enabled", typeof(int), typeof(bool));
    }

    private void AssertNamedCapturedForms(string enabledName, Type countType, Type enabledType)
    {
        var provider = new SpecStressNamedMixSchemaProvider();
        using var positional = InstanceCreator.CompileForExecution(PositionalCapturedQuery, Guid.NewGuid().ToString(), provider, LoggerResolver, TestCompilationOptions);
        using var positionalResult = positional.Run();
        AssertNamedCapturedRows(positionalResult, enabledName, countType, enabledType);
        using var named = InstanceCreator.CompileForExecution(NamedCapturedQuery, Guid.NewGuid().ToString(), provider, LoggerResolver, TestCompilationOptions);
        using var namedResult = named.Run();
        AssertNamedCapturedRows(namedResult, enabledName, countType, enabledType);
    }

    private static void AssertNamedCapturedRows(Table result, string enabledName, Type countType, Type enabledType)
    {
        TableMaterializationTestHelper.AssertColumns(result, ("ReceivedCount", countType), (enabledName, enabledType));
        TableMaterializationTestHelper.AssertRowsUnordered(result, new object?[] { 7, true });
    }
}
