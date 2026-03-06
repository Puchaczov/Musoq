using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Plugins.Attributes;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Tests for error scenarios identified in the gap analysis (plan.md).
///     Covers silent misinterpretations (S1-S4), wrong diagnostic codes (W3),
///     untested production codes (U1-U3), and pipeline independence (B2).
/// </summary>
[TestClass]
public class ErrorGapAnalysisTests : GenericEntityTestBase
{
    #region S1-S3: Invalid base-prefixed numbers now produce errors

    [TestMethod]
    public void S1_InvalidHexLiteral_ShouldThrow()
    {
        const string query = "select 0xGG from #schema.first()";
        var source = new[] { new SimpleEntity { Name = "a" } };

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, source));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ1006_InvalidHexNumber, DiagnosticPhase.Parse);
    }

    [TestMethod]
    public void S1_HexWithNoDigits_ShouldThrow()
    {
        const string query = "select 0x from #schema.first()";
        var source = new[] { new SimpleEntity { Name = "a" } };

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, source));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ1006_InvalidHexNumber, DiagnosticPhase.Parse);
    }

    [TestMethod]
    public void S2_InvalidBinaryLiteral_ShouldThrow()
    {
        const string query = "select 0b23 from #schema.first()";
        var source = new[] { new SimpleEntity { Name = "a" } };

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, source));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ1007_InvalidBinaryNumber, DiagnosticPhase.Parse);
    }

    [TestMethod]
    public void S2_BinaryWithNoDigits_ShouldThrow()
    {
        const string query = "select 0b from #schema.first()";
        var source = new[] { new SimpleEntity { Name = "a" } };

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, source));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ1007_InvalidBinaryNumber, DiagnosticPhase.Parse);
    }

    [TestMethod]
    public void S3_InvalidOctalLiteral_ShouldThrow()
    {
        const string query = "select 0o89 from #schema.first()";
        var source = new[] { new SimpleEntity { Name = "a" } };

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, source));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ1008_InvalidOctalNumber, DiagnosticPhase.Parse);
    }

    [TestMethod]
    public void S3_OctalWithNoDigits_ShouldThrow()
    {
        const string query = "select 0o from #schema.first()";
        var source = new[] { new SimpleEntity { Name = "a" } };

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, source));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ1008_InvalidOctalNumber, DiagnosticPhase.Parse);
    }

    [TestMethod]
    public void S1_ValidHexLiteral_ShouldStillWork()
    {
        const string query = "select 0xFF from #schema.first()";
        var source = new[] { new SimpleEntity { Name = "a" } };

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void S2_ValidBinaryLiteral_ShouldStillWork()
    {
        const string query = "select 0b101 from #schema.first()";
        var source = new[] { new SimpleEntity { Name = "a" } };

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void S3_ValidOctalLiteral_ShouldStillWork()
    {
        const string query = "select 0o77 from #schema.first()";
        var source = new[] { new SimpleEntity { Name = "a" } };

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
    }

    #endregion

    #region S4: Unterminated block comments now produce errors

    [TestMethod]
    public void S4_UnterminatedBlockComment_ShouldThrow()
    {
        const string query = "select 1 /* unclosed comment from #schema.first()";
        var source = new[] { new SimpleEntity { Name = "a" } };

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, source));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ1005_UnterminatedBlockComment, DiagnosticPhase.Parse);
    }

    [TestMethod]
    public void S4_TerminatedBlockComment_ShouldStillWork()
    {
        const string query = "select /* comment */ 1 from #schema.first()";
        var source = new[] { new SimpleEntity { Name = "a" } };

        var vm = CreateAndRunVirtualMachine(query, source);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
    }

    #endregion

    #region U1: MQ3025_ColumnMustBeArray — cross apply on non-array column

    [TestMethod]
    public void U1_CrossApplyOnStringColumn_ShouldThrowColumnMustBeArray()
    {
        const string query = "select b.Value from #schema.first() a cross apply a.Name as b";
        var source = new[] { new SimpleEntity { Name = "hello" } };

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, source));

        AssertExactErrors(ex, DiagnosticCode.MQ3001_UnknownColumn, DiagnosticCode.MQ3025_ColumnMustBeArray);
    }

    [TestMethod]
    public void U1_CrossApplyOnIntColumn_ShouldThrowColumnMustBeArray()
    {
        const string query = "select b.Value from #schema.first() a cross apply a.Count as b";
        var source = new[] { new SimpleEntity { Name = "hello", Count = 5 } };

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, source));

        AssertExactErrors(ex, DiagnosticCode.MQ3001_UnknownColumn, DiagnosticCode.MQ3025_ColumnMustBeArray);
    }

    #endregion

    #region U2: MQ3026_ColumnNotBindable — cross apply on non-bindable complex property

    [TestMethod]
    public void U2_CrossApplyOnBindableButInvalidType_ShouldThrowNotBindable()
    {
        const string query = "select b.Value1 from #schema.first() a cross apply a.NotACollection as b";
        var source = new[] { new EntityWithUnbindableProperty { Name = "test" } };

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, source));

        AssertExactErrors(ex, DiagnosticCode.MQ3001_UnknownColumn, DiagnosticCode.MQ3026_ColumnNotBindable);
    }

    #endregion

    #region B2: LexerException now implements IDiagnosticException

    [TestMethod]
    public void B2_UnterminatedString_ShouldProduceSpecificLexerCode()
    {
        const string query = "select 'unterminated from #schema.first()";
        var source = new[] { new SimpleEntity { Name = "a" } };

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, source));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ1002_UnterminatedString, DiagnosticPhase.Parse);
    }

    #endregion

    #region Test Entity Types

    public class SimpleEntity
    {
        public string Name { get; set; }

        public int Count { get; set; }
    }

    public class InnerComplexType
    {
        public string Value1 { get; set; }
    }

    public class EntityWithUnbindableProperty
    {
        public string Name { get; set; }

        [BindablePropertyAsTable]
        public InnerComplexType NotACollection { get; set; }
    }

    #endregion
}
