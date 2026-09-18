using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Tests for MALFORMED queries derived from the specs.
///     These assert that the engine produces meaningful errors.
///     Every failure here is useful feedback about error quality.
/// </summary>
[TestClass]
public class SpecExplorationErrorTests : BasicEntityTestBase
{

    #region Malformed Queries - Parser/Compile Errors

    [TestMethod]
    public void Spec_Error_SelectWithoutFrom_ShouldFail()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine("select 1", sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2004_MissingFromClause, DiagnosticPhase.Parse, "FROM clause");
    }

    [TestMethod]
    public void Spec_Error_CaseWhenWithoutElse_ShouldFail()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test") { Population = 100m }] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(
                "select case when Population > 0 then 'positive' end from #A.Entities()",
                sources));

        AssertErrorEnvelope(
            ex,
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticPhase.Parse,
            "Expected token is Else but received End");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void Spec_Error_DivisionByZeroLiteral_ShouldFail()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine("select 10 / 0 from #A.Entities()", sources));

        AssertSingleError(ex, DiagnosticCode.MQ3008_DivisionByZero, DiagnosticPhase.Bind);
    }

    [TestMethod]
    public void Spec_Error_ModuloByZeroLiteral_ShouldFail()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine("select 10 % 0 from #A.Entities()", sources));

        AssertSingleError(ex, DiagnosticCode.MQ3008_DivisionByZero, DiagnosticPhase.Bind);
    }

    [TestMethod]
    public void Spec_SelectAliasInWhere_IsSupported()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test"), new BasicEntity("other")] }
        };
        var vm = CreateAndRunVirtualMachine(
            "select Name as FileName from #A.Entities() where FileName = 'test'",
            sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("test", table[0][0]);
    }

    [TestMethod]
    public void Spec_Error_NonAggregatedColumnWithGroupBy_ShouldFail()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a") { City = "NYC", Country = "USA" },
                    new BasicEntity("b") { City = "LA", Country = "USA" }
                ]
            }
        };

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(
                "select Name, City, Count(1) from #A.Entities() group by City",
                sources));

        AssertErrorEnvelope(
            ex,
            DiagnosticCode.MQ3012_NonAggregateInSelect,
            DiagnosticPhase.Bind,
            "must appear in the GROUP BY");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void Spec_Error_DuplicateAliasInJoin_ShouldFail()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(
                "select 1 from #A.Entities() a inner join #A.Entities() a on 1 = 1",
                sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3021_DuplicateAlias, DiagnosticPhase.Bind, "a");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void Spec_Error_NonExistingProperty_ShouldFail()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(
                "select Self.NonExistingProperty from #A.Entities()",
                sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3028_UnknownProperty, DiagnosticPhase.Bind, "NonExistingProperty");
    }

    [TestMethod]
    public void Spec_Error_StarWithGroupBy_ShouldFail()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a") { City = "NYC" },
                    new BasicEntity("b") { City = "LA" }
                ]
            }
        };

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(
                "select * from #A.Entities() group by City",
                sources));

        AssertErrorEnvelope(
            ex,
            DiagnosticCode.MQ3012_NonAggregateInSelect,
            DiagnosticPhase.Bind,
            "must appear in the GROUP BY");
        AssertHasGuidance(ex);
    }

    #endregion

    #region TABLE/COUPLE Malformed Queries

    [TestMethod]
    public void Spec_Error_CoupleWithoutTable_ShouldFail()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(
                "couple #A.Entities with table NonExistentTable as Source; select * from Source()",
                sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3023_TableNotDefined, DiagnosticPhase.Bind, "NonExistentTable");
    }

    [TestMethod]
    public void Spec_Error_LegacyPrefixDoubleColonNumber_ShouldFail()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a") { Country = "POLAND" }
                ]
            }
        };

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(
                "select ::5, Count(Name) from #A.Entities() group by Country",
                sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse, "DoubleColon");
    }

    #endregion

    #region Spec Features Not Supported - Good Errors Expected

    [TestMethod]
    public void Spec_Between_IsSupported()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("a") { Population = 200m },
                    new BasicEntity("b") { Population = 50m },
                    new BasicEntity("c") { Population = 300m }
                ]
            }
        };
        var vm = CreateAndRunVirtualMachine(
            "select Name from #A.Entities() where Population between 100 and 300",
            sources);
        var table = vm.Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["a"], ["c"]);
    }

    [TestMethod]
    public void Spec_Error_OrderByPosition_NotSupported()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice"),
                    new BasicEntity("Bob")
                ]
            }
        };

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(
                "select Name from #A.Entities() order by 1",
                sources));

        AssertErrorEnvelope(
            ex,
            DiagnosticCode.MQ3093_OrderByOrdinalUnsupported,
            DiagnosticPhase.Bind,
            "ORDER BY column position is not supported");
        AssertHasGuidance(ex);
    }

    #endregion
}
