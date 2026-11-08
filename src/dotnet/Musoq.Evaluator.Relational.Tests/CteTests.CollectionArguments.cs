using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;

namespace Musoq.Evaluator.Tests;

public partial class CteTests
{
    private const string CoupledCollectionQuery = "table ResultRows { Value: int };\r\ncouple specstresscte.capture with table ResultRows as Echo;\r\nwith InputData as (\r\n    from values {\r\n        (Value: 3),\r\n        (Value: 1),\r\n        (Value: 2)\r\n    } v\r\n    select v.Value\r\n)\r\nselect received.Value\r\nfrom Echo((InputData)) received";

    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P4-0009")]
    public void Current_P4_0009_CteCollectionArgumentMaterializesAllRows()
    {
        AssertCoupledCollectionResult("Value", useProviderNullability: true);
    }

    [TestMethod]
    public void CteCollectionArgument_CurrentProjectionNameRetainsAllRows()
    {
        AssertCoupledCollectionResult("Value", useProviderNullability: true);
    }

    private void AssertCoupledCollectionResult(string outputName, bool useProviderNullability = false)
    {
        var query = CoupledCollectionQuery;
        if (useProviderNullability)
            query = query.Replace("Value: int", "Value: int?", StringComparison.Ordinal);
        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), new SpecStressCteCollectionSchemaProvider(), LoggerResolver, TestCompilationOptions);
        using var result = compiled.Run();
        TableMaterializationTestHelper.AssertColumns(result, (outputName, typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(result,
            new object?[] { 1 },
            new object?[] { 2 },
            new object?[] { 3 });
    }
}
