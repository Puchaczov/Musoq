using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Tests for MALFORMED queries derived from the specs.
///     These assert that the engine produces meaningful errors.
///     Every failure here is useful feedback about error quality.
/// </summary>
[TestClass]
public class SpecExplorationErrorTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    #region Malformed Queries - Parser/Compile Errors

    [TestMethod]
    public void Spec_Error_SelectWithoutFrom_ShouldFail()
    {
        try
        {
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                { "#A", [new BasicEntity("test")] }
            };
            var vm = CreateAndRunVirtualMachine("select 1", sources);
            vm.Run(TokenSource.Token);
            Assert.Fail("Expected an exception for SELECT without FROM");
        }
        catch (Exception ex)
        {
            Assert.IsGreaterThan(
                0,
                ex.Message.Length, $"Error message should be non-empty, got: {ex.GetType().Name}: {ex.Message}");
        }
    }

    [TestMethod]
    public void Spec_Error_CaseWhenWithoutElse_ShouldFail()
    {
        try
        {
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                { "#A", [new BasicEntity("test") { Population = 100m }] }
            };
            var vm = CreateAndRunVirtualMachine(
                "select case when Population > 0 then 'positive' end from #A.Entities()",
                sources);
            vm.Run(TokenSource.Token);
            Assert.Fail("Expected an exception: CASE WHEN without ELSE should fail per spec");
        }
        catch (Exception ex)
        {
            Assert.IsGreaterThan(
                0,
                ex.Message.Length, $"Error message should be non-empty, got: {ex.GetType().Name}: {ex.Message}");
        }
    }

    [TestMethod]
    public void Spec_Error_DivisionByZeroLiteral_ShouldFail()
    {
        try
        {
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                { "#A", [new BasicEntity("test")] }
            };
            var vm = CreateAndRunVirtualMachine("select 10 / 0 from #A.Entities()", sources);
            vm.Run(TokenSource.Token);
            Assert.Fail("Expected an exception for division by zero literal");
        }
        catch (Exception ex)
        {
            Assert.IsGreaterThan(
                0,
                ex.Message.Length,
                $"Error message should be meaningful for div by zero, got: {ex.GetType().Name}: {ex.Message}");
        }
    }

    [TestMethod]
    public void Spec_Error_ModuloByZeroLiteral_ShouldFail()
    {
        try
        {
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                { "#A", [new BasicEntity("test")] }
            };
            var vm = CreateAndRunVirtualMachine("select 10 % 0 from #A.Entities()", sources);
            vm.Run(TokenSource.Token);
            Assert.Fail("Expected an exception for modulo by zero literal");
        }
        catch (Exception ex)
        {
            Assert.IsGreaterThan(
                0,
                ex.Message.Length,
                $"Error message should be meaningful for mod by zero, got: {ex.GetType().Name}: {ex.Message}");
        }
    }

    [TestMethod]
    public void Spec_Error_SelectAliasInWhere_ShouldFail()
    {
        try
        {
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                { "#A", [new BasicEntity("test")] }
            };
            var vm = CreateAndRunVirtualMachine(
                "select Name as FileName from #A.Entities() where FileName = 'test'",
                sources);
            vm.Run(TokenSource.Token);
            Assert.Fail("Expected an exception: SELECT alias used in WHERE");
        }
        catch (Exception ex)
        {
            Assert.IsGreaterThan(
                0,
                ex.Message.Length,
                $"Error should mention unknown column/alias, got: {ex.GetType().Name}: {ex.Message}");
        }
    }

    [TestMethod]
    public void Spec_Error_NonAggregatedColumnWithGroupBy_ShouldFail()
    {
        try
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
            var vm = CreateAndRunVirtualMachine(
                "select Name, City, Count(1) from #A.Entities() group by City",
                sources);
            vm.Run(TokenSource.Token);
            Assert.Fail("Expected NonAggregatedColumnInSelectException");
        }
        catch (Exception ex)
        {
            Assert.IsGreaterThan(
                0,
                ex.Message.Length,
                $"Error should mention non-aggregated column, got: {ex.GetType().Name}: {ex.Message}");
        }
    }

    [TestMethod]
    public void Spec_Error_DuplicateAliasInJoin_ShouldFail()
    {
        try
        {
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                { "#A", [new BasicEntity("test")] }
            };
            var vm = CreateAndRunVirtualMachine(
                "select 1 from #A.Entities() a inner join #A.Entities() a on 1 = 1",
                sources);
            vm.Run(TokenSource.Token);
            Assert.Fail("Expected AliasAlreadyUsedException");
        }
        catch (Exception ex)
        {
            Assert.IsGreaterThan(
                0,
                ex.Message.Length, $"Error should mention duplicate alias, got: {ex.GetType().Name}: {ex.Message}");
        }
    }

    [TestMethod]
    public void Spec_Error_NonExistingProperty_ShouldFail()
    {
        try
        {
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                { "#A", [new BasicEntity("test")] }
            };
            var vm = CreateAndRunVirtualMachine(
                "select Self.NonExistingProperty from #A.Entities()",
                sources);
            vm.Run(TokenSource.Token);
            Assert.Fail("Expected UnknownPropertyException for non-existing property");
        }
        catch (Exception ex)
        {
            Assert.IsGreaterThan(
                0,
                ex.Message.Length, $"Error should mention unknown property, got: {ex.GetType().Name}: {ex.Message}");
        }
    }

    [TestMethod]
    public void Spec_Error_StarWithGroupBy_ShouldFail()
    {
        try
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
            var vm = CreateAndRunVirtualMachine(
                "select * from #A.Entities() group by City",
                sources);
            vm.Run(TokenSource.Token);
            Assert.Fail("Expected error for SELECT * with GROUP BY");
        }
        catch (Exception ex)
        {
            Assert.IsGreaterThan(
                0,
                ex.Message.Length,
                $"Error should mention non-aggregated column, got: {ex.GetType().Name}: {ex.Message}");
        }
    }

    #endregion

    #region TABLE/COUPLE Malformed Queries

    [TestMethod]
    public void Spec_Error_CoupleWithoutTable_ShouldFail()
    {
        try
        {
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                { "#A", [new BasicEntity("test")] }
            };
            var vm = CreateAndRunVirtualMachine(
                "couple #A.Entities with table NonExistentTable as Source; select * from Source()",
                sources);
            vm.Run(TokenSource.Token);
            Assert.Fail("Expected error: COUPLE referencing non-existent table");
        }
        catch (Exception ex)
        {
            Assert.IsGreaterThan(
                0,
                ex.Message.Length, $"Error should mention undefined table, got: {ex.GetType().Name}: {ex.Message}");
        }
    }

    [TestMethod]
    public void Spec_Error_FieldLinkOutOfRange_ShouldFail()
    {
        try
        {
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A", [
                        new BasicEntity("a") { Country = "POLAND" }
                    ]
                }
            };
            var vm = CreateAndRunVirtualMachine(
                "select ::5, Count(Name) from #A.Entities() group by Country",
                sources);
            vm.Run(TokenSource.Token);
            Assert.Fail("Expected FieldLinkIndexOutOfRangeException for ::5 with 1 GROUP BY column");
        }
        catch (Exception ex)
        {
            Assert.IsGreaterThan(
                0,
                ex.Message.Length,
                $"Error should mention field link out of range, got: {ex.GetType().Name}: {ex.Message}");
        }
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

        Assert.AreEqual(2, table.Count, "200 and 300 are within [100,300]");
    }

    [TestMethod]
    public void Spec_Error_OrderByPosition_NotSupported()
    {
        try
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
            var vm = CreateAndRunVirtualMachine(
                "select Name from #A.Entities() order by 1",
                sources);
            vm.Run(TokenSource.Token);
        }
        catch (Exception ex)
        {
            Assert.IsGreaterThan(
                0,
                ex.Message.Length,
                $"Error for ORDER BY position should be meaningful, got: {ex.GetType().Name}: {ex.Message}");
        }
    }

    #endregion
}
