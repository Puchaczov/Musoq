using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

/// <summary>
/// Permanent REC-129 matrix for runtime-owned failures and internal invariants.
/// The cases keep compile-time impossibility separate from data-dependent
/// execution failures and assert the public boundary before any repair decision.
/// </summary>
[TestClass]
public sealed class REC129RuntimeClassificationTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("RX-01")]
    [DataRow("RX-02")]
    [DataRow("RX-03")]
    [DataRow("RX-04")]
    [DataRow("RX-05")]
    [DataRow("RX-06")]
    [DataRow("RX-07")]
    [DataRow("RX-08")]
    [DataRow("RX-09")]
    [DataRow("RX-10")]
    [DataRow("RX-11")]
    [DataRow("RX-12")]
    public void DynamicInvalidRegexMatrix_ShouldRemainInternalRuntimeFailure(string caseId)
    {
        var (query, sources) = CreateRegexCase(caseId);
        AssertInternalRuntimeFailure(CreateAndRunVirtualMachine(query, sources), caseId);
    }

    [TestMethod]
    [DataRow("SC-01")]
    [DataRow("SC-02")]
    [DataRow("SC-03")]
    [DataRow("SC-04")]
    [DataRow("SC-05")]
    [DataRow("SC-06")]
    [DataRow("SC-07")]
    [DataRow("SC-08")]
    [DataRow("SC-09")]
    [DataRow("SC-10")]
    [DataRow("SC-11")]
    [DataRow("SC-12")]
    public void ScalarCardinalityMatrix_ShouldDistinguishBindAndRuntimeFailures(string caseId)
    {
        var (query, sources, compileTime) = CreateScalarCase(caseId);
        if (compileTime)
        {
            var exception = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));
            AssertSingleError(exception, DiagnosticCode.MQ3095_ScalarSubqueryCardinality, DiagnosticPhase.Bind);
            return;
        }

        AssertInternalRuntimeFailure(CreateAndRunVirtualMachine(query, sources), caseId);
    }

    [TestMethod]
    [DataRow("CV-01")]
    [DataRow("CV-02")]
    [DataRow("CV-03")]
    [DataRow("CV-04")]
    [DataRow("CV-05")]
    [DataRow("CV-06")]
    [DataRow("CV-07")]
    [DataRow("CV-08")]
    [DataRow("CV-09")]
    [DataRow("CV-10")]
    [DataRow("CV-11")]
    [DataRow("CV-12")]
    public void StrictConversionMatrix_ShouldKeepDataDependentFailuresInternal(string caseId)
    {
        var (query, row) = CreateConversionCase(caseId);
        AssertInternalRuntimeFailure(
            CreateAndRunVirtualMachine(query, CreateSingleSource(row)),
            caseId);
    }

    [TestMethod]
    [DataRow("AR-01")]
    [DataRow("AR-02")]
    [DataRow("AR-03")]
    [DataRow("AR-04")]
    [DataRow("AR-05")]
    [DataRow("AR-06")]
    [DataRow("AR-07")]
    [DataRow("AR-08")]
    [DataRow("AR-09")]
    [DataRow("AR-10")]
    [DataRow("AR-11")]
    [DataRow("AR-12")]
    public void ArithmeticClassificationMatrix_ShouldSeparateDataAndConstantFailures(string caseId)
    {
        var (query, sources, expectedCode, compileTime) = CreateArithmeticCase(caseId);
        if (compileTime)
        {
            var exception = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));
            AssertSingleError(exception, expectedCode, DiagnosticPhase.Bind);
            return;
        }

        AssertInternalRuntimeFailure(CreateAndRunVirtualMachine(query, sources), caseId);
    }

    private void AssertInternalRuntimeFailure(CompiledQuery query, string caseId)
    {
        var exception = Assert.Throws<QueryExecutionException>(() =>
            _ = query.Run(TestContext.CancellationToken).Count);
        var envelope = exception.Envelope ?? throw new AssertFailedException($"Expected a runtime envelope for {caseId}.");

        Assert.AreEqual(DiagnosticCode.MQ9002_InternalExecutionError, envelope.Code, caseId);
        Assert.AreEqual(DiagnosticPhase.Internal, envelope.Phase, caseId);
        Assert.AreEqual(DiagnosticSourceKind.Internal, envelope.SourceKind, caseId);
        Assert.IsNull(envelope.Offset, caseId);
        Assert.IsNull(envelope.Length, caseId);
        Assert.IsTrue(envelope.Arguments.ContainsKey("correlationId"), caseId);
        Assert.IsTrue(envelope.Arguments.ContainsKey("exceptionType"), caseId);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.CorrelationId), caseId);
        Assert.IsNotNull(exception.InnerException, caseId);
        Assert.DoesNotContain("secret-rec129", exception.Message, caseId);
    }

    private static (string Query, IDictionary<string, IEnumerable<BasicEntity>> Sources) CreateRegexCase(string caseId)
    {
        const string invalid = "[invalid(";
        var row = new BasicEntity { Name = "value", City = invalid, Country = "POLAND", Id = 1 };
        return caseId switch
        {
            "RX-01" => ("select Name from #A.Entities() where Name rlike City", Sources("#A", row)),
            "RX-02" => ("select City from #A.Entities() where City rlike Name", Sources("#A", new BasicEntity { Name = invalid, City = "value" })),
            "RX-03" => ("select Name rlike City as IsMatch from #A.Entities()", Sources("#A", row)),
            "RX-04" => ("select case when Name rlike City then 'yes' else 'no' end from #A.Entities()", Sources("#A", row)),
            "RX-05" => ("select Name from #A.Entities() where not Name rlike City", Sources("#A", row)),
            "RX-06" => ("select Name from #A.Entities() where (Name rlike City) = true", Sources("#A", row)),
            "RX-07" => ("select Name from #A.Entities() where Name rlike (City + '')", Sources("#A", row)),
            "RX-08" => ("select Name from #A.Entities() where Name rlike (case when Id = 1 then City else 'ok' end)", Sources("#A", row)),
            "RX-09" => ("select Name from #A.Entities() where Name rlike (ToString(City))", Sources("#A", row)),
            "RX-10" => ("select a.Name from #A.Entities() a where a.Name rlike (select b.City from #B.Entities() b)", new Dictionary<string, IEnumerable<BasicEntity>>
            {
                ["#A"] = [new BasicEntity { Name = "value" }],
                ["#B"] = [new BasicEntity { City = invalid }]
            }),
            "RX-11" => ("select Name from #A.Entities() where Name rlike (case when Id = 1 then City else Name end)", Sources("#A", row)),
            "RX-12" => ("select Name from #A.Entities() where Name not rlike (City + 'x')", Sources("#A", row)),
            _ => throw new AssertFailedException($"Unknown regex case '{caseId}'.")
        };
    }

    private static (string Query, IDictionary<string, IEnumerable<BasicEntity>> Sources, bool CompileTime) CreateScalarCase(string caseId)
    {
        var single = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity { Name = "outer" }]
        };
        var multi = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity { Name = "outer" }],
            ["#B"] =
            [
                new BasicEntity { Country = "POLAND", City = "WARSAW" },
                new BasicEntity { Country = "POLAND", City = "KRAKOW" }
            ]
        };

        return caseId switch
        {
            "SC-01" => ("select (select Value from values { { Value: 1 }, { Value: 2 } } valuesSource) as Value from #A.Entities() a", single, true),
            "SC-02" => ("select (select Value from values { { Value: 1 }, { Value: 2 } } valuesSource order by Value) as Value from #A.Entities() a", single, true),
            "SC-03" => ("select 1 from #A.Entities() a where (select Value from values { { Value: 1 }, { Value: 2 } } valuesSource) = 1", single, true),
            "SC-04" => ("select case when (select Value from values { { Value: 1 }, { Value: 2 } } valuesSource) = 1 then 'yes' else 'no' end from #A.Entities() a", single, true),
            "SC-05" => ("select ToString((select Value from values { { Value: 1 }, { Value: 2 } } valuesSource)) from #A.Entities() a", single, true),
            "SC-06" => ("select (select Value from values { { Value: 1 }, { Value: 2 } } valuesSource take 2) from #A.Entities() a", single, false),
            "SC-07" => ("select (select b.City from #B.Entities() b where b.Country = 'POLAND') as City from #A.Entities() a", multi, false),
            "SC-08" => ("select a.Name from #A.Entities() a where (select b.City from #B.Entities() b where b.Country = 'POLAND') = 'WARSAW'", multi, false),
            "SC-09" => ("select case when (select b.City from #B.Entities() b where b.Country = 'POLAND') = 'WARSAW' then 'yes' else 'no' end from #A.Entities() a", multi, false),
            "SC-10" => ("select Coalesce((select b.City from #B.Entities() b where b.Country = 'POLAND'), 'fallback') from #A.Entities() a", multi, false),
            "SC-11" => ("select a.Name from #A.Entities() a order by (select b.City from #B.Entities() b where b.Country = 'POLAND')", multi, false),
            "SC-12" => ("select (select b.City from #B.Entities() b where b.Country = 'POLAND') + '' from #A.Entities() a", multi, false),
            _ => throw new AssertFailedException($"Unknown scalar case '{caseId}'.")
        };
    }

    private static (string Query, BasicEntity Row) CreateConversionCase(string caseId)
    {
        return caseId switch
        {
            "CV-01" => ("select Name::Int32 from #A.Entities()", new BasicEntity { Name = "not-a-number" }),
            "CV-02" => ("select Name::Guid from #A.Entities()", new BasicEntity { Name = "not-a-guid" }),
            "CV-03" => ("select Money::Byte from #A.Entities()", new BasicEntity { Money = 256m }),
            "CV-04" => ("select Name::DateTime from #A.Entities()", new BasicEntity { Name = "not-a-date" }),
            "CV-05" => ("select Name from #A.Entities() where Name::Int32 > 0", new BasicEntity { Name = "not-a-number" }),
            "CV-06" => ("select (Name + '')::Int32 from #A.Entities()", new BasicEntity { Name = "not-a-number" }),
            "CV-07" => ("select Name::UInt64 from #A.Entities()", new BasicEntity { Name = "not-a-number" }),
            "CV-08" => ("select Money::Int32 from #A.Entities()", new BasicEntity { Money = decimal.MaxValue }),
            "CV-09" => ("select Name::TimeSpan from #A.Entities()", new BasicEntity { Name = "not-a-duration" }),
            "CV-10" => ("select Name::Boolean from #A.Entities()", new BasicEntity { Name = "not-a-bool" }),
            "CV-11" => ("select Name from #A.Entities() where (Name + '')::Guid is not null", new BasicEntity { Name = "not-a-guid" }),
            "CV-12" => ("select Name::Decimal from #A.Entities()", new BasicEntity { Name = "not-a-decimal" }),
            _ => throw new AssertFailedException($"Unknown conversion case '{caseId}'.")
        };
    }

    private static (string Query, IDictionary<string, IEnumerable<BasicEntity>> Sources, DiagnosticCode ExpectedCode, bool CompileTime)
        CreateArithmeticCase(string caseId)
    {
        var data = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity { Population = 10m, Money = 1m }]
        };
        var overflow = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity { Population = decimal.MaxValue, Money = 1m }]
        };

        return caseId switch
        {
            "AR-01" => ("select Population / (Population - Population) from #A.Entities()", data, DiagnosticCode.MQ9002_InternalExecutionError, false),
            "AR-02" => ("select Population % (Population - Population) from #A.Entities()", data, DiagnosticCode.MQ9002_InternalExecutionError, false),
            "AR-03" => ("select Population + Money from #A.Entities()", overflow, DiagnosticCode.MQ9002_InternalExecutionError, false),
            "AR-04" => ("select Population - Money from #A.Entities()", new Dictionary<string, IEnumerable<BasicEntity>>
            {
                ["#A"] = [new BasicEntity { Population = decimal.MinValue, Money = 1m }]
            }, DiagnosticCode.MQ9002_InternalExecutionError, false),
            "AR-05" => ("select Population / (Population - (Money + 9)) from #A.Entities()", data, DiagnosticCode.MQ9002_InternalExecutionError, false),
            "AR-06" => ("select Population * Population from #A.Entities()", overflow, DiagnosticCode.MQ9002_InternalExecutionError, false),
            "AR-07" => ("select 10 / 0 from #A.Entities()", data, DiagnosticCode.MQ3008_DivisionByZero, true),
            "AR-08" => ("select 10 % 0 from #A.Entities()", data, DiagnosticCode.MQ3008_DivisionByZero, true),
            "AR-09" => ("select 2147483647i + 1i from #A.Entities()", data, DiagnosticCode.MQ3032_ArithmeticOverflow, true),
            "AR-10" => ("select -2147483648i - 1i from #A.Entities()", data, DiagnosticCode.MQ3032_ArithmeticOverflow, true),
            "AR-11" => ("select 9223372036854775807l * 2l from #A.Entities()", data, DiagnosticCode.MQ3032_ArithmeticOverflow, true),
            "AR-12" => ("select 0ui - 1ui from #A.Entities()", data, DiagnosticCode.MQ3032_ArithmeticOverflow, true),
            _ => throw new AssertFailedException($"Unknown arithmetic case '{caseId}'.")
        };
    }

    private static IDictionary<string, IEnumerable<BasicEntity>> Sources(string schema, BasicEntity row) =>
        new Dictionary<string, IEnumerable<BasicEntity>> { [schema] = [row] };
}
