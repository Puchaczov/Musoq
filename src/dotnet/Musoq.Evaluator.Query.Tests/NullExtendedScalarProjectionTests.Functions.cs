using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

public sealed partial class NullExtendedScalarProjectionTests
{
    [TestMethod]
    [DataRow(false, "fallback")]
    [DataRow(true, "x")]
    public void ScalarArguments_ShouldKeepEagerEvaluationOrder(bool matched, string expected)
    {
        var trace = new List<string>();
        NullProjectionLibrary.Trace.Value = trace;
        try
        {
            var sql = "select Coalesce(TraceValue(b.Dummy, 'left'), TraceValue('fallback', 'right')) as F " +
                "from #schema.first() a left outer join #schema.first() b on 1=" + (matched ? "1" : "0");
            using var query = CompileCustom(sql);
            using var table = query.Run();
            TableMaterializationTestHelper.AssertRowsInOrder(table, [expected]);
            CollectionAssert.AreEqual(new[] { "left", "right" }, trace);
        }
        finally { NullProjectionLibrary.Trace.Value = null; }
    }

    [TestMethod]
    public void CustomNullHandlingMethod_ShouldReceiveMissingColumnAsNull()
    {
        using var query = CompileCustom("select Recover(b.Dummy, a.Dummy) as F from #schema.first() a left outer join #schema.first() b on 1=0");
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["recovered:x"]);
    }

    [TestMethod]
    public void MissingInjectedCarrier_ShouldKeepExistingAbsentSourceBehavior()
    {
        var trace = new List<string>();
        NullProjectionLibrary.Trace.Value = trace;
        try
        {
            using var query = CompileCustom("select b.ReadInjected() as F from #schema.first() a left outer join #schema.first() b on 1=0");
            using var table = query.Run();
            TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { null });
            Assert.IsEmpty(trace);
        }
        finally { NullProjectionLibrary.Trace.Value = null; }
    }

    private static CompiledQuery CompileCustom(string sql) => InstanceCreator.CompileForExecution(sql,
        Guid.NewGuid().ToString(), LibraryExecutionFixture.CreateProvider<NullProjectionRow, NullProjectionLibrary>([new("x")]),
        new TestsLoggerResolver(), new CompilationOptions(parallelizationMode: ParallelizationMode.None));
}

public readonly record struct NullProjectionRow(string Dummy);

public sealed class NullProjectionLibrary : LibraryBase
{
    public static readonly AsyncLocal<List<string>?> Trace = new();
    [BindableMethod]
    public string ReadInjected([InjectSpecificSource(typeof(NullProjectionRow))] NullProjectionRow row)
    {
        Trace.Value!.Add("injected");
        return row.Dummy;
    }
    [BindableMethod]
    public string Recover(string? value, string fallback) => value ?? "recovered:" + fallback;
    [BindableMethod, NonDeterministic]
    public string? TraceValue(string? value, string label)
    {
        Trace.Value!.Add(label);
        return value;
    }
}
