using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Tests verifying that NULL in set operator queries is handled correctly:
///     1. NULL values in key columns do not cause NullReferenceException in the generated comparer.
///     2. NULL on one side with a concrete type on the other side does not throw a type mismatch error —
///     the NULL is inferred as null-of-the-concrete-type.
///     3. Multi-way set operations propagate the concrete type through cached fields.
/// </summary>
[global::Musoq.Tests.Common.CultureTestClass]
public partial class NullInSetOperatorsTests : BasicEntityTestBase
{
    #region UNION ALL with null columns (should work — no comparison)

    [TestMethod]
    public void UnionAll_NullColumn_ShouldWorkWithoutComparison()
    {
        var query = (
            "\n"
            + "            select null as Col1, Name from #A.Entities() \n"
            + "            union all (Name) \n"
            + "            select null as Col1, Name from #B.Entities()"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Col1", typeof(System.DBNull)), ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [null, "001"], [null, "002"]);
    }

    #endregion

    #region Multi-way set operations with null key columns

    [TestMethod]
    public void ThreeWayUnion_NullKeyColumn_ShouldNotThrowNullReference()
    {
        var query = (
            "\n"
            + "            select null as Col1, Name from #A.Entities() \n"
            + "            union (Col1) \n"
            + "            select null as Col1, Name from #B.Entities()\n"
            + "            union (Col1) \n"
            + "            select null as Col1, Name from #C.Entities()"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002")] },
            { "#C", [new BasicEntity("003")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Col1", typeof(System.DBNull)), ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [null, "001"]);
    }

    #endregion

    #region INTERSECT — null on one side, concrete type on the other

    [TestMethod]
    public void Intersect_NullOnLeftStringOnRight_ShouldInferTypeFromRight()
    {
        var query = (
            "\n"
            + "            select Name, null as Extra from #A.Entities() \n"
            + "            intersect (Name) \n"
            + "            select Name, City as Extra from #B.Entities()"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("001") { City = "Warsaw" }] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Extra", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["001", null]);
    }

    #endregion

    #region Mismatched concrete types should still fail

    #endregion

    #region UNION with null key column values

    [TestMethod]
    public void Union_SingleNullKeyColumn_ShouldNotThrowNullReference()
    {
        var query = (
            "\n"
            + "            select null as Col1 from #A.Entities() \n"
            + "            union (Col1) \n"
            + "            select null as Col1 from #B.Entities()"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Col1", typeof(System.DBNull)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [(object?)null]);
    }

    [TestMethod]
    public void Union_NullKeyColumnWithOtherColumns_ShouldNotThrowNullReference()
    {
        var query = (
            "\n"
            + "            select null as NullKey, Name from #A.Entities() \n"
            + "            union (NullKey) \n"
            + "            select null as NullKey, Name from #B.Entities()"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("NullKey", typeof(System.DBNull)),
            ("Name", typeof(string))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(table, [null, "001"]);
    }

    [TestMethod]
    public void Union_NullNonKeyColumn_ShouldWork()
    {
        var query = (
            "\n"
            + "            select Name, null as Tag from #A.Entities() \n"
            + "            union (Name) \n"
            + "            select Name, null as Tag from #B.Entities()"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Tag", typeof(System.DBNull)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["001", null], ["002", null]);
    }

    #endregion

    #region EXCEPT with null key column values

    [TestMethod]
    public void Except_SingleNullKeyColumn_ShouldNotThrowNullReference()
    {
        var query = (
            "\n"
            + "            select null as Col1 from #A.Entities() \n"
            + "            except (Col1) \n"
            + "            select null as Col1 from #B.Entities()"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Col1", typeof(System.DBNull)));
        TableMaterializationTestHelper.AssertRowsUnordered(table);
    }

    [TestMethod]
    public void Except_NullNonKeyColumn_ShouldWork()
    {
        var query = (
            "\n"
            + "            select Name, null as Tag from #A.Entities() \n"
            + "            except (Name) \n"
            + "            select Name, null as Tag from #B.Entities()"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Tag", typeof(System.DBNull)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["001", null]);
    }

    #endregion

    #region INTERSECT with null key column values

    [TestMethod]
    public void Intersect_SingleNullKeyColumn_ShouldNotThrowNullReference()
    {
        var query = (
            "\n"
            + "            select null as Col1 from #A.Entities() \n"
            + "            intersect (Col1) \n"
            + "            select null as Col1 from #B.Entities()"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Col1", typeof(System.DBNull)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [(object?)null]);
    }

    [TestMethod]
    public void Intersect_NullNonKeyColumn_ShouldWork()
    {
        var query = (
            "\n"
            + "            select Name, null as Tag from #A.Entities() \n"
            + "            intersect (Name) \n"
            + "            select Name, null as Tag from #B.Entities()"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("001")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Tag", typeof(System.DBNull)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["001", null]);
    }

    #endregion

    #region Null at various positions in column list

    [TestMethod]
    public void Union_NullAsFirstColumn_NonKeyPosition_ShouldWork()
    {
        var query = (
            "\n"
            + "            select null as Extra, Name from #A.Entities() \n"
            + "            union (Name) \n"
            + "            select null as Extra, Name from #B.Entities()"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Extra", typeof(System.DBNull)), ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [null, "001"], [null, "002"]);
    }

    [TestMethod]
    public void Union_NullAsMiddleColumn_NonKeyPosition_ShouldWork()
    {
        var query = (
            "\n"
            + "            select Name, null as Extra, City from #A.Entities() \n"
            + "            union (Name) \n"
            + "            select Name, null as Extra, City from #B.Entities()"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("AAA") { City = "CityA" }] },
            { "#B", [new BasicEntity("BBB") { City = "CityB" }] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Extra", typeof(System.DBNull)),
            ("City", typeof(string))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["AAA", null, "CityA"], ["BBB", null, "CityB"]);
    }

    [TestMethod]
    public void Union_NullAsLastColumn_NonKeyPosition_ShouldWork()
    {
        var query = (
            "\n"
            + "            select Name, null as Extra from #A.Entities() \n"
            + "            union (Name) \n"
            + "            select Name, null as Extra from #B.Entities()"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Extra", typeof(System.DBNull)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["001", null], ["002", null]);
    }

    [TestMethod]
    public void Union_BareNullWithoutAlias_NonKeyPosition_ShouldWork()
    {
        var query = (
            "\n"
            + "            select Name, null from #A.Entities() \n"
            + "            union (Name) \n"
            + "            select Name, null from #B.Entities()"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("null", typeof(System.DBNull)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["001", null], ["002", null]);
    }

    #endregion

    // =====================================================================
    // Mixed NULL and concrete type scenarios — one side has null, the other
    // has a concrete type column. The null should be inferred as the concrete type.
    // =====================================================================

    #region UNION ALL — null on left, concrete type on right

    [TestMethod]
    public void UnionAll_NullOnLeftStringOnRight_ShouldInferTypeFromRight()
    {
        var query = (
            "\n"
            + "            select Name, null as Extra from #A.Entities() \n"
            + "            union all (Name) \n"
            + "            select Name, City as Extra from #B.Entities()"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002") { City = "Warsaw" }] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Extra", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["001", null], ["002", "Warsaw"]);
    }

    [TestMethod]
    public void UnionAll_StringOnLeftNullOnRight_ShouldInferTypeFromLeft()
    {
        var query = (
            "\n"
            + "            select Name, City as Extra from #A.Entities() \n"
            + "            union all (Name) \n"
            + "            select Name, null as Extra from #B.Entities()"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001") { City = "Warsaw" }] },
            { "#B", [new BasicEntity("002")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Extra", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["001", "Warsaw"], ["002", null]);
    }

    #endregion

    #region UNION — null on one side, concrete type on the other

    [TestMethod]
    public void Union_NullOnLeftStringOnRight_ShouldInferTypeFromRight()
    {
        var query = (
            "\n"
            + "            select Name, null as Extra from #A.Entities() \n"
            + "            union (Name) \n"
            + "            select Name, City as Extra from #B.Entities()"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002") { City = "Warsaw" }] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Extra", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["001", null], ["002", "Warsaw"]);
    }

    [TestMethod]
    public void Union_StringOnLeftNullOnRight_ShouldInferTypeFromLeft()
    {
        var query = (
            "\n"
            + "            select Name, City as Extra from #A.Entities() \n"
            + "            union (Name) \n"
            + "            select Name, null as Extra from #B.Entities()"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001") { City = "Warsaw" }] },
            { "#B", [new BasicEntity("002")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Extra", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["001", "Warsaw"], ["002", null]);
    }

    #endregion

    #region EXCEPT — null on one side, concrete type on the other

    [TestMethod]
    public void Except_NullOnLeftStringOnRight_ShouldInferTypeFromRight()
    {
        var query = (
            "\n"
            + "            select Name, null as Extra from #A.Entities() \n"
            + "            except (Name) \n"
            + "            select Name, City as Extra from #B.Entities()"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002") { City = "Warsaw" }] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Extra", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["001", null]);
    }

    [TestMethod]
    public void Except_StringOnLeftNullOnRight_ShouldInferTypeFromLeft()
    {
        var query = (
            "\n"
            + "            select Name, City as Extra from #A.Entities() \n"
            + "            except (Name) \n"
            + "            select Name, null as Extra from #B.Entities()"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001") { City = "Warsaw" }] },
            { "#B", [new BasicEntity("002")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Extra", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["001", "Warsaw"]);
    }

    #endregion

    #region Multi-way set operations — null type propagation through cached fields

    [TestMethod]
    public void ThreeWayUnionAll_NullFirstThenConcreteTypeThenNull_ShouldInferType()
    {
        var query = (
            "\n"
            + "            select Name, null as Extra from #A.Entities() \n"
            + "            union all (Name) \n"
            + "            select Name, City as Extra from #B.Entities()\n"
            + "            union all (Name) \n"
            + "            select Name, null as Extra from #C.Entities()"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002") { City = "Warsaw" }] },
            { "#C", [new BasicEntity("003")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Extra", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["001", null], ["002", "Warsaw"], ["003", null]);
    }

    [TestMethod]
    public void ThreeWayUnionAll_ConcreteTypeThenNullThenNull_ShouldInferType()
    {
        var query = (
            "\n"
            + "            select Name, City as Extra from #A.Entities() \n"
            + "            union all (Name) \n"
            + "            select Name, null as Extra from #B.Entities()\n"
            + "            union all (Name) \n"
            + "            select Name, null as Extra from #C.Entities()"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001") { City = "Warsaw" }] },
            { "#B", [new BasicEntity("002")] },
            { "#C", [new BasicEntity("003")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Extra", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["001", "Warsaw"], ["002", null], ["003", null]);
    }

    [TestMethod]
    public void ThreeWayUnion_NullThenConcreteTypeThenNull_ShouldInferType()
    {
        var query = (
            "\n"
            + "            select Name, null as Extra from #A.Entities() \n"
            + "            union (Name) \n"
            + "            select Name, City as Extra from #B.Entities()\n"
            + "            union (Name) \n"
            + "            select Name, null as Extra from #C.Entities()"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002") { City = "Warsaw" }] },
            { "#C", [new BasicEntity("003")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Extra", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["001", null], ["002", "Warsaw"], ["003", null]);
    }

    #endregion
}
