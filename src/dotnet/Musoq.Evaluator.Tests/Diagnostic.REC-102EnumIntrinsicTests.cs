using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticREC102EnumIntrinsicTests : GenericEntityTestBase
{
    private static readonly REC102EnumEntity[] Rows =
    [
        new(REC102JobStatus.Running, REC102FileAccess.Read | REC102FileAccess.Write, 20, "Running", new object())
    ];

    private static readonly REC102EnumEntity[] NullRows =
    [
        new(null, null, null, "NULL", new object())
    ];

    private static readonly REC102EnumEntity[] UnknownRows =
    [
        new((REC102JobStatus)99, (REC102FileAccess)8, 99, "Unknown", new object())
    ];

    public static IEnumerable<object[]> InvalidEnumIntrinsicCases()
    {
        yield return new object[]
        {
            "REC-102-SIG01",
            "select EnumValue(e.Status, e.Status) from #schema.first() e",
            DiagnosticCode.MQ3111_InvalidEnumHelper
        };
        yield return new object[]
        {
            "REC-102-SIG02",
            "select EnumName() from #schema.first() e",
            DiagnosticCode.MQ3111_InvalidEnumHelper
        };
        yield return new object[]
        {
            "REC-102-SIG03",
            "select IsDefined(e.Status, e.Status) from #schema.first() e",
            DiagnosticCode.MQ3111_InvalidEnumHelper
        };
        yield return new object[]
        {
            "REC-102-SIG04",
            "select HasAnyFlags() from #schema.first() e",
            DiagnosticCode.MQ3111_InvalidEnumHelper
        };
        yield return new object[]
        {
            "REC-102-SIG05",
            "select HasAllFlags() from #schema.first() e",
            DiagnosticCode.MQ3111_InvalidEnumHelper
        };
        yield return new object[]
        {
            "REC-102-SIG06",
            "select EnumValue(e.Label) from #schema.first() e",
            DiagnosticCode.MQ3111_InvalidEnumHelper
        };
        yield return new object[]
        {
            "REC-102-SIG07",
            "select EnumName(e.Number) from #schema.first() e",
            DiagnosticCode.MQ3111_InvalidEnumHelper
        };
        yield return new object[]
        {
            "REC-102-SIG08",
            "select IsDefined(e.Payload) from #schema.first() e",
            DiagnosticCode.MQ3111_InvalidEnumHelper
        };
        yield return new object[]
        {
            "REC-102-SIG09",
            "select HasAnyFlags(e.Status, 'Read') from #schema.first() e",
            DiagnosticCode.MQ3111_InvalidEnumHelper
        };
        yield return new object[]
        {
            "REC-102-SIG10",
            "select HasAllFlags(e.Status, 'Read') from #schema.first() e",
            DiagnosticCode.MQ3111_InvalidEnumHelper
        };
        yield return new object[]
        {
            "REC-102-SIG11",
            "select HasAnyFlags(e.Access, 1) from #schema.first() e",
            DiagnosticCode.MQ3111_InvalidEnumHelper
        };
        yield return new object[]
        {
            "REC-102-SIG12",
            "select HasAllFlags(e.Access, e.Label) from #schema.first() e",
            DiagnosticCode.MQ3111_InvalidEnumHelper
        };
    }

    public static IEnumerable<object[]> FlagsBehaviorCases()
    {
        yield return new object[]
        {
            "REC-102-FLAG01",
            "select HasAnyFlags(e.Access, 'Write') from #schema.first() e",
            true
        };
        yield return new object[]
        {
            "REC-102-FLAG02",
            "select HasAllFlags(e.Access, 'Read', 'Write') from #schema.first() e",
            true
        };
        yield return new object[]
        {
            "REC-102-FLAG03",
            "select HasAnyFlags(e.Access) from #schema.first() e",
            false
        };
        yield return new object[]
        {
            "REC-102-FLAG04",
            "select HasAllFlags(e.Access) from #schema.first() e",
            true
        };
        yield return new object[]
        {
            "REC-102-FLAG05",
            "select HasAllFlags(e.Access, 'ReadWrite') from #schema.first() e",
            true
        };
        yield return new object[]
        {
            "REC-102-FLAG06",
            "select EnumName(e.Access) as DeclaredName from #schema.first() e",
            "ReadWrite"
        };
    }

    public static IEnumerable<object[]> NullAndUnknownIntrinsicCases()
    {
        yield return new object[]
        {
            "REC-102-VALUE01",
            "select EnumValue(e.Status) as StatusValue, EnumName(e.Status) as StatusName, IsDefined(e.Status) as StatusDefined, HasAnyFlags(e.Access, 'Read') as AnyRead, HasAllFlags(e.Access, 'Read') as AllRead from #schema.first() e",
            false,
            new object?[] { null, null, false, false, false }
        };
        yield return new object[]
        {
            "REC-102-VALUE02",
            "select HasAnyFlags(e.Access) as AnyMask, HasAllFlags(e.Access) as AllMask from #schema.first() e",
            false,
            new object?[] { false, false }
        };
        yield return new object[]
        {
            "REC-102-VALUE03",
            "select case when e.Status is null then EnumName(e.Status) else 'unexpected' end as Name from #schema.first() e",
            false,
            new object?[] { null }
        };
        yield return new object[]
        {
            "REC-102-VALUE04",
            "select EnumValue(e.Status) as StatusValue, EnumName(e.Status) as StatusName, IsDefined(e.Status) as StatusDefined, HasAnyFlags(e.Access, 'Read') as AnyRead, HasAllFlags(e.Access, 'Read') as AllRead from #schema.first() e",
            true,
            new object?[] { (short)99, null, false, false, false }
        };
        yield return new object[]
        {
            "REC-102-VALUE05",
            "select EnumName(e.Access) as AccessName, IsDefined(e.Access) as AccessDefined, HasAnyFlags(e.Access) as AnyMask, HasAllFlags(e.Access) as AllMask from #schema.first() e",
            true,
            new object?[] { null, false, false, true }
        };
        yield return new object[]
        {
            "REC-102-VALUE06",
            "select e.Status as Status, EnumValue(e.Status) as StatusValue from #schema.first() e",
            true,
            new object?[] { (short)99, (short)99 }
        };
    }

    [TestMethod]
    [DynamicData(nameof(InvalidEnumIntrinsicCases))]
    public void InvalidEnumIntrinsics_ShouldReportContractDiagnostics(
        string caseId,
        string query,
        DiagnosticCode expectedCode)
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(caseId));

        var exception = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, Rows));

        AssertSingleError(exception, expectedCode, DiagnosticPhase.Bind);
        AssertHasGuidance(exception);
    }

    [TestMethod]
    [DynamicData(nameof(FlagsBehaviorCases))]
    public void FlagsIntrinsics_ShouldHonorDeclaredMasks(
        string caseId,
        string query,
        object expectedValue)
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(caseId));

        using var compiledQuery = CreateAndRunVirtualMachine(query, Rows);
        using var table = compiledQuery.Run(TestContext.CancellationToken);

        Assert.HasCount(1, table);
        Assert.AreEqual(expectedValue, table[0].Values[0]);
    }

    [TestMethod]
    [DynamicData(nameof(NullAndUnknownIntrinsicCases))]
    public void NullAndUnknownEnumValues_ShouldFollowIntrinsicContract(
        string caseId,
        string query,
        bool useUnknownRows,
        object?[] expectedValues)
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(caseId));

        using var compiledQuery = CreateAndRunVirtualMachine(query, useUnknownRows ? UnknownRows : NullRows);
        using var table = compiledQuery.Run(TestContext.CancellationToken);

        Assert.HasCount(1, table);
        CollectionAssert.AreEqual(expectedValues, table[0].Values);
        if (caseId == "REC-102-VALUE06")
            Assert.IsNotNull(table.Columns.ElementAt(0).EnumType);
    }

    [TestMethod]
    public void AllIntrinsics_ShouldExecuteAsCompilerOwnedPrimitiveOperations()
    {
        using var compiledQuery = CreateAndRunVirtualMachine(
            "select EnumValue(e.Status), EnumName(e.Status), IsDefined(e.Status), " +
            "HasAnyFlags(e.Access, 'Read'), HasAllFlags(e.Access, 'Read', 'Write') " +
            "from #schema.first() e",
            Rows);
        using var table = compiledQuery.Run(TestContext.CancellationToken);

        Assert.HasCount(1, table);
        CollectionAssert.AreEqual(
            new object?[] { (short)20, "Running", true, true, true },
            table[0].Values);
        Assert.IsFalse(table.SelectMany(static row => row.Values).Any(static value => value is Enum));
    }

    [TestMethod]
    public void ZeroMaskControl_ShouldDistinguishAnyAndAllSemantics()
    {
        using var compiledQuery = CreateAndRunVirtualMachine(
            "select HasAnyFlags(e.Access), HasAllFlags(e.Access) from #schema.first() e",
            Rows);
        using var table = compiledQuery.Run(TestContext.CancellationToken);

        Assert.HasCount(1, table);
        CollectionAssert.AreEqual(new object?[] { false, true }, table[0].Values);
    }

    [TestMethod]
    public void CompositeNameControl_ShouldReturnOnlyTheDeclaredCompositeName()
    {
        using var compiledQuery = CreateAndRunVirtualMachine(
            "select EnumName(e.Access) from #schema.first() e",
            Rows);
        using var table = compiledQuery.Run(TestContext.CancellationToken);

        Assert.HasCount(1, table);
        Assert.AreEqual("ReadWrite", table[0].Values[0]);
        Assert.DoesNotContain(",", (string)table[0].Values[0]!);
    }

    [TestMethod]
    public void UnknownValueControl_ShouldPreserveCarrierAndEnumMetadata()
    {
        using var compiledQuery = CreateAndRunVirtualMachine(
            "select e.Status, EnumName(e.Status), IsDefined(e.Status) from #schema.first() e",
            UnknownRows);
        using var table = compiledQuery.Run(TestContext.CancellationToken);

        Assert.HasCount(1, table);
        CollectionAssert.AreEqual(new object?[] { (short)99, null, false }, table[0].Values);
        Assert.AreEqual(typeof(short?), table.Columns.ElementAt(0).ColumnType);
        Assert.IsNotNull(table.Columns.ElementAt(0).EnumType);
    }

    [TestMethod]
    public void CandidateMatrix_ShouldContainTwentyFourRegisteredCasesAcrossThreeFamilies()
    {
        var ids = InvalidEnumIntrinsicCases()
            .Concat(FlagsBehaviorCases())
            .Concat(NullAndUnknownIntrinsicCases())
            .Select(static values => (string)values[0])
            .ToArray();

        Assert.HasCount(24, ids);
        Assert.AreEqual(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
        Assert.AreEqual(12, ids.Count(static id => id.Contains("-SIG", StringComparison.Ordinal)));
        Assert.AreEqual(6, ids.Count(static id => id.Contains("-FLAG", StringComparison.Ordinal)));
        Assert.AreEqual(6, ids.Count(static id => id.Contains("-VALUE", StringComparison.Ordinal)));
    }
}

public sealed record REC102EnumEntity(
    REC102JobStatus? Status,
    REC102FileAccess? Access,
    int? Number,
    string Label,
    object Payload);

public enum REC102JobStatus : short
{
    Queued = 10,
    Running = 20,
    Finished = 30
}

[Flags]
public enum REC102FileAccess : uint
{
    None = 0,
    Read = 1,
    Write = 2,
    ReadWrite = Read | Write
}
