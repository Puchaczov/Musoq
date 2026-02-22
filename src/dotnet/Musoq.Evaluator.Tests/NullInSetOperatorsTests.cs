using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Tests verifying that NULL in set operator queries is handled correctly:
///     1. NULL values in key columns do not cause NullReferenceException in the generated comparer.
///     2. NULL on one side with a concrete type on the other side does not throw a type mismatch error —
///     the NULL is inferred as null-of-the-concrete-type.
///     3. Multi-way set operations propagate the concrete type through cached fields.
/// </summary>
[TestClass]
public class NullInSetOperatorsTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    #region UNION ALL with null columns (should work — no comparison)

    [TestMethod]
    public void UnionAll_NullColumn_ShouldWorkWithoutComparison()
    {
        var query = @"
            select null as Col1, Name from #A.Entities() 
            union all (Name) 
            select null as Col1, Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.IsNull(table[0].Values[0]);
        Assert.AreEqual("001", table[0].Values[1]);
        Assert.IsNull(table[1].Values[0]);
        Assert.AreEqual("002", table[1].Values[1]);
    }

    #endregion

    #region Multi-way set operations with null key columns

    [TestMethod]
    public void ThreeWayUnion_NullKeyColumn_ShouldNotThrowNullReference()
    {
        var query = @"
            select null as Col1, Name from #A.Entities() 
            union (Col1) 
            select null as Col1, Name from #B.Entities()
            union (Col1) 
            select null as Col1, Name from #C.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002")] },
            { "#C", [new BasicEntity("003")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsGreaterThanOrEqualTo(1, table.Count);
    }

    #endregion

    #region INTERSECT — null on one side, concrete type on the other

    [TestMethod]
    public void Intersect_NullOnLeftStringOnRight_ShouldInferTypeFromRight()
    {
        var query = @"
            select Name, null as Extra from #A.Entities() 
            intersect (Name) 
            select Name, City as Extra from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("001") { City = "Warsaw" }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("001", table[0].Values[0]);
    }

    #endregion

    #region Mismatched concrete types should still fail

    [TestMethod]
    public void Union_DifferentConcreteTypes_ShouldStillThrowTypeMismatch()
    {
        var query = @"
            select Name from #A.Entities() 
            union (Name) 
            select 1 as Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] }
        };

        Assert.Throws<SetOperatorMustHaveSameTypesOfColumnsException>(() => CreateAndRunVirtualMachine(query, sources));
    }

    #endregion

    #region UNION with null key column values

    [TestMethod]
    public void Union_SingleNullKeyColumn_ShouldNotThrowNullReference()
    {
        var query = @"
            select null as Col1 from #A.Entities() 
            union (Col1) 
            select null as Col1 from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count, "Both nulls are equal so union should deduplicate to 1 row");
        Assert.IsNull(table[0].Values[0]);
    }

    [TestMethod]
    public void Union_NullKeyColumnWithOtherColumns_ShouldNotThrowNullReference()
    {
        var query = @"
            select null as NullKey, Name from #A.Entities() 
            union (NullKey) 
            select null as NullKey, Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.IsGreaterThanOrEqualTo(1, table.Count);
    }

    [TestMethod]
    public void Union_NullNonKeyColumn_ShouldWork()
    {
        var query = @"
            select Name, null as Tag from #A.Entities() 
            union (Name) 
            select Name, null as Tag from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("001", table[0].Values[0]);
        Assert.IsNull(table[0].Values[1]);
        Assert.AreEqual("002", table[1].Values[0]);
        Assert.IsNull(table[1].Values[1]);
    }

    #endregion

    #region EXCEPT with null key column values

    [TestMethod]
    public void Except_SingleNullKeyColumn_ShouldNotThrowNullReference()
    {
        var query = @"
            select null as Col1 from #A.Entities() 
            except (Col1) 
            select null as Col1 from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(0, table.Count, "null equals null so except should remove all rows");
    }

    [TestMethod]
    public void Except_NullNonKeyColumn_ShouldWork()
    {
        var query = @"
            select Name, null as Tag from #A.Entities() 
            except (Name) 
            select Name, null as Tag from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("001", table[0].Values[0]);
    }

    #endregion

    #region INTERSECT with null key column values

    [TestMethod]
    public void Intersect_SingleNullKeyColumn_ShouldNotThrowNullReference()
    {
        var query = @"
            select null as Col1 from #A.Entities() 
            intersect (Col1) 
            select null as Col1 from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count, "null equals null so intersect should keep the row");
        Assert.IsNull(table[0].Values[0]);
    }

    [TestMethod]
    public void Intersect_NullNonKeyColumn_ShouldWork()
    {
        var query = @"
            select Name, null as Tag from #A.Entities() 
            intersect (Name) 
            select Name, null as Tag from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("001")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("001", table[0].Values[0]);
    }

    #endregion

    #region Null at various positions in column list

    [TestMethod]
    public void Union_NullAsFirstColumn_NonKeyPosition_ShouldWork()
    {
        var query = @"
            select null as Extra, Name from #A.Entities() 
            union (Name) 
            select null as Extra, Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Union_NullAsMiddleColumn_NonKeyPosition_ShouldWork()
    {
        var query = @"
            select Name, null as Extra, City from #A.Entities() 
            union (Name) 
            select Name, null as Extra, City from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("AAA") { City = "CityA" }] },
            { "#B", [new BasicEntity("BBB") { City = "CityB" }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Union_NullAsLastColumn_NonKeyPosition_ShouldWork()
    {
        var query = @"
            select Name, null as Extra from #A.Entities() 
            union (Name) 
            select Name, null as Extra from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Union_BareNullWithoutAlias_NonKeyPosition_ShouldWork()
    {
        var query = @"
            select Name, null from #A.Entities() 
            union (Name) 
            select Name, null from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
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
        var query = @"
            select Name, null as Extra from #A.Entities() 
            union all (Name) 
            select Name, City as Extra from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002") { City = "Warsaw" }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("001", table[0].Values[0]);
        Assert.IsNull(table[0].Values[1]);
        Assert.AreEqual("002", table[1].Values[0]);
        Assert.AreEqual("Warsaw", table[1].Values[1]);
    }

    [TestMethod]
    public void UnionAll_StringOnLeftNullOnRight_ShouldInferTypeFromLeft()
    {
        var query = @"
            select Name, City as Extra from #A.Entities() 
            union all (Name) 
            select Name, null as Extra from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001") { City = "Warsaw" }] },
            { "#B", [new BasicEntity("002")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("001", table[0].Values[0]);
        Assert.AreEqual("Warsaw", table[0].Values[1]);
        Assert.AreEqual("002", table[1].Values[0]);
        Assert.IsNull(table[1].Values[1]);
    }

    #endregion

    #region UNION — null on one side, concrete type on the other

    [TestMethod]
    public void Union_NullOnLeftStringOnRight_ShouldInferTypeFromRight()
    {
        var query = @"
            select Name, null as Extra from #A.Entities() 
            union (Name) 
            select Name, City as Extra from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002") { City = "Warsaw" }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Union_StringOnLeftNullOnRight_ShouldInferTypeFromLeft()
    {
        var query = @"
            select Name, City as Extra from #A.Entities() 
            union (Name) 
            select Name, null as Extra from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001") { City = "Warsaw" }] },
            { "#B", [new BasicEntity("002")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
    }

    #endregion

    #region EXCEPT — null on one side, concrete type on the other

    [TestMethod]
    public void Except_NullOnLeftStringOnRight_ShouldInferTypeFromRight()
    {
        var query = @"
            select Name, null as Extra from #A.Entities() 
            except (Name) 
            select Name, City as Extra from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002") { City = "Warsaw" }] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("001", table[0].Values[0]);
    }

    [TestMethod]
    public void Except_StringOnLeftNullOnRight_ShouldInferTypeFromLeft()
    {
        var query = @"
            select Name, City as Extra from #A.Entities() 
            except (Name) 
            select Name, null as Extra from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001") { City = "Warsaw" }] },
            { "#B", [new BasicEntity("002")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("001", table[0].Values[0]);
    }

    #endregion

    #region Multi-way set operations — null type propagation through cached fields

    [TestMethod]
    public void ThreeWayUnionAll_NullFirstThenConcreteTypeThenNull_ShouldInferType()
    {
        var query = @"
            select Name, null as Extra from #A.Entities() 
            union all (Name) 
            select Name, City as Extra from #B.Entities()
            union all (Name) 
            select Name, null as Extra from #C.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002") { City = "Warsaw" }] },
            { "#C", [new BasicEntity("003")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.IsNull(table[0].Values[1]);
        Assert.AreEqual("Warsaw", table[1].Values[1]);
        Assert.IsNull(table[2].Values[1]);
    }

    [TestMethod]
    public void ThreeWayUnionAll_ConcreteTypeThenNullThenNull_ShouldInferType()
    {
        var query = @"
            select Name, City as Extra from #A.Entities() 
            union all (Name) 
            select Name, null as Extra from #B.Entities()
            union all (Name) 
            select Name, null as Extra from #C.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001") { City = "Warsaw" }] },
            { "#B", [new BasicEntity("002")] },
            { "#C", [new BasicEntity("003")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("Warsaw", table[0].Values[1]);
        Assert.IsNull(table[1].Values[1]);
        Assert.IsNull(table[2].Values[1]);
    }

    [TestMethod]
    public void ThreeWayUnion_NullThenConcreteTypeThenNull_ShouldInferType()
    {
        var query = @"
            select Name, null as Extra from #A.Entities() 
            union (Name) 
            select Name, City as Extra from #B.Entities()
            union (Name) 
            select Name, null as Extra from #C.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002") { City = "Warsaw" }] },
            { "#C", [new BasicEntity("003")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
    }

    #endregion
}
