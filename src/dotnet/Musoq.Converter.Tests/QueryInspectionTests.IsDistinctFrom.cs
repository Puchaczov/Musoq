using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForInspection_WhenIsDistinctFromIsUsed_ShouldRenderExecutionIrAndTypedComparison()
    {
        var result = Inspect("select d.Dummy from #system.dual() d where d.Dummy is distinct from 'single'");

        AssertUsesExecutionBackend(result);
        AssertExecutionPlanContains("If [(dummy IS DISTINCT FROM 'single')]", result.ExecutionPlanText);
        Assert.Contains("dummy != \"single\"", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("InternalIsDistinctFromOperator", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("System.Reflection", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenIsNotDistinctFromIsUsed_ShouldRenderExecutionIrAndTypedComparison()
    {
        var result = Inspect("select d.Dummy from #system.dual() d where d.Dummy is not distinct from 'single'");

        AssertUsesExecutionBackend(result);
        AssertExecutionPlanContains("If [(dummy IS NOT DISTINCT FROM 'single')]", result.ExecutionPlanText);
        Assert.Contains("dummy == \"single\"", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("InternalIsNotDistinctFromOperator", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("System.Reflection", result.GeneratedCSharpCode);
    }
}
