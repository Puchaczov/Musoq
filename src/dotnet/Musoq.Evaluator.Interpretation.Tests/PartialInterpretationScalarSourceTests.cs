using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class PartialInterpretationScalarSourceTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void PartialInterpretation_ShouldUseDirectTypedScalarBindingWithoutChunkBuffers(bool text)
    {
        var sql = text
            ? "text R { Value: chars[1] } select p.BytesConsumed from #test.files() f cross apply PartialParse<R>(f.Text) p"
            : "binary R { Value: byte } select p.BytesConsumed from #test.files() f cross apply PartialInterpret<R>(f.Content) p";
        ISchemaProvider provider = text
            ? new TextSchemaProvider(new Dictionary<string, IEnumerable<TextEntity>> { ["#test"] = [new() { Text = "a" }] })
            : new BinarySchemaProvider(new Dictionary<string, IEnumerable<BinaryEntity>> { ["#test"] = [new() { Content = [1] }] });
        var options = new CompilationOptions(usePrimitiveTypeValidation: false);
        var inspection = InstanceCreator.CompileForInspection(sql, Guid.NewGuid().ToString(), provider, new TestsLoggerResolver(), options);
        Assert.Contains("ScalarForEach [p", inspection.ExecutionPlanText);
        Assert.DoesNotContain("ConvertEnumerableOutputToChunks", inspection.GeneratedCSharpCode);
        Assert.DoesNotContain("WrapScalarForCrossApply", inspection.GeneratedCSharpCode);
        using var query = InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString(), provider, new TestsLoggerResolver(), options);
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1]);
        Assert.IsFalse(GeneratedExecutionIlInspection.GetGeneratedComputeMethods(query).Any(GeneratedExecutionIlInspection.ContainsBoxOpcode));
    }
}
