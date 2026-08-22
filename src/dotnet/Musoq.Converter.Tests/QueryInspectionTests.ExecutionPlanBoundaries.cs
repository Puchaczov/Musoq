using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForInspection_WhenQueryIsValid_ShouldReturnExecutionPlanText()
    {
        var result = CreateInspection();

        AssertTextEquals(
            string.Join("\n",
                "ExecutionPlan [compiled]",
                "  Shapes",
                "    SourceEntity [d: DualEntity]",
                "      Dummy: string <- property Dummy",
                "    Generated [ResultRow0]",
                "      d.Dummy: string <- field d_Dummy",
                string.Empty,
                "  Body",
                "    PhaseBoundary [Begin]",
                "    PhaseBoundary [From]",
                "    SourceScan [d: DualEntity] -> dRows",
                "    CreateShapeRows [result: ResultShape0 from ResultRow0]",
                "    PhaseBoundary [Select]",
                "    ChunkedForEach [d in dRows]",
                "      AppendShape [result <- ResultShape0(d.Dummy: d.Dummy)]",
                "    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]"),
            result.ExecutionPlanText);
    }
}
