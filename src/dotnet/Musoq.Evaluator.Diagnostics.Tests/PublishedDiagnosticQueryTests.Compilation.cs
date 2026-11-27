using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Components;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public sealed partial class PublishedDiagnosticQueryTests
{
    [TestMethod]
    [DataRow(
        "select 1 from #A.Entities(); select 2 from #A.Entities()",
        "select 1 from #A.Entities()",
        DiagnosticCode.MQ2036_MultipleExecutableStatements,
        DiagnosticPhase.Parse,
        DiagnosticSourceKind.Query,
        0,
        0,
        1
    )]
    [DataRow(
        "select 10 / 0 from #A.Entities()",
        "select 10 / 2 from #A.Entities()",
        DiagnosticCode.MQ3008_DivisionByZero,
        DiagnosticPhase.Bind,
        DiagnosticSourceKind.Query,
        7,
        6,
        5
    )]
    [DataRow(
        "select 2147483647i + 1i from #A.Entities()",
        "select 2147483646i + 1i from #A.Entities()",
        DiagnosticCode.MQ3032_ArithmeticOverflow,
        DiagnosticPhase.Bind,
        DiagnosticSourceKind.Query,
        7,
        16,
        2147483647
    )]
    [DataRow(
        "select Sum(x)+x as Bad from values {(x:1)} v",
        "select Sum(x)+x as Bad from values {(x:1)} v group by x",
        DiagnosticCode.MQ3097_UnsupportedAggregateProjection,
        DiagnosticPhase.Bind,
        DiagnosticSourceKind.Query,
        7,
        8,
        2
    )]
    public void Compilation_ShouldReportItsSpecificEnvelopeAndCompileTheRepair(
        string trigger,
        string repair,
        DiagnosticCode code,
        DiagnosticPhase phase,
        DiagnosticSourceKind domain,
        int offset,
        int length,
        int expected
    )
    {
        using var compiled = InstanceCreator.CompileForExecution(
            repair,
            Guid.NewGuid().ToString("N"),
            CreateProvider(),
            new TestsLoggerResolver()
        );
        using var rows = compiled.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(rows, [expected]);
        var error = Assert.ThrowsExactly<MusoqQueryException>(() =>
            InstanceCreator.CompileForExecution(
                trigger,
                Guid.NewGuid().ToString("N"),
                CreateProvider(),
                new TestsLoggerResolver()
            )
        );
        var envelope = error.PrimaryEnvelope;
        Assert.AreEqual(code, envelope.Code);
        Assert.AreEqual(phase, envelope.Phase);
        Assert.AreEqual(domain, envelope.SourceKind);
        Assert.AreEqual(DiagnosticSeverity.Error, envelope.Severity);
        Assert.AreEqual(offset, envelope.Offset);
        Assert.AreEqual(length, envelope.Length);
    }
}
