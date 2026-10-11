using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

public sealed partial class ScriptVariableDiagnosticTests
{
    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P1-0139")]
    public void UnsupportedObjectArrayLetIsRejected()
    {
        const string query = "let values: object[] = array { 1, 2 }; select 1 from #specstressnative.rows()";

        AssertUnsupportedStructuralVariableType(query);
    }

    [TestMethod]
    [TestProperty("CandidateId", "PROMOTED-UnsupportedObjectArrayLetDeclarationIsRejectedWithMq3064")]
    public void UnsupportedObjectArrayLetDeclarationIsRejectedWithMq3064()
    {
        const string query = "let values: object[] = array { }; select 1 from #specstressnative.rows()";

        AssertUnsupportedStructuralVariableType(query);
    }

    [TestMethod]
    [DataRow("let value: (Payload: object) = (Payload: 1); select 1 from #specstressnative.rows()")]
    [DataRow("let values: object[][] = array { array { 1 } }; select 1 from #specstressnative.rows()")]
    public void ObjectInsideRecursiveVariableType_ShouldReportUnsupportedType(string query)
    {
        AssertUnsupportedStructuralVariableType(query);
    }

    [TestMethod]
    [DataRow("param(value: (Payload: object)); select 1 from #specstressnative.rows()")]
    [DataRow("param(values: object[][]); select 1 from #specstressnative.rows()")]
    public void ObjectInsideRecursiveParameterType_ShouldReportUnsupportedType(string query)
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            InstanceCreator.CompileForExecution(
                query, Guid.NewGuid().ToString(), new NativeTableCarrierSchemaProvider(),
                LoggerResolver, new CompilationOptions(usePrimitiveTypeValidation: false), TestContext.CancellationToken));

        AssertSingleError(exception, DiagnosticCode.MQ3060_UnsupportedScriptParameterType, DiagnosticPhase.Bind,
            "not a supported Core scalar type");
    }

    private void AssertUnsupportedStructuralVariableType(string query)
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            InstanceCreator.CompileForExecution(
                query, Guid.NewGuid().ToString(), new NativeTableCarrierSchemaProvider(),
                LoggerResolver, new CompilationOptions(usePrimitiveTypeValidation: false), TestContext.CancellationToken));

        AssertSingleError(exception, DiagnosticCode.MQ3064_UnsupportedScriptVariableType, DiagnosticPhase.Bind,
            "is not supported");
    }
}
