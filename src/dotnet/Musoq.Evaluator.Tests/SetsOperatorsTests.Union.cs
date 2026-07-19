using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

public partial class SetsOperatorsTests
{

    [TestMethod]
    public void UnionWithDifferentColumnsAsAKeyTest()
    {
        var query = @"select Name from #A.Entities() union (Name) select City as Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] },
            { "#B", [new BasicEntity("003", "", 0), new BasicEntity("004", "", 0)] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["001"], ["002"], ["003"], ["004"]);
    }

    [TestMethod]
    public void AliasedUnionWithDifferentColumnsAsAKeyTest()
    {
        var query =
            """
            select
                a.Name as a1,
                b.Value
            from #A.Entities() a
            cross apply a.ToCharArray(a.Name) b
            union (Name)
            select
                a.Name as a1,
                b.Value
            from #A.Entities() a
            cross apply a.ToCharArray(a.Name) b
            """;
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a1", typeof(string)),
            ("b.Value", typeof(char)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["001", '0'], ["001", '0'], ["001", '1'],
            ["002", '0'], ["002", '0'], ["002", '2']);
    }

    [TestMethod]
    public void UnionWithSkipTest()
    {
        var query = @"select Name from #A.Entities() skip 1 union (Name) select Name from #B.Entities() skip 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] },
            { "#B", [new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("005")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["002"], ["005"]);
    }

    [TestMethod]
    public void UnionAllWithSkipTest()
    {
        var query = @"select Name from #A.Entities() skip 1 union all (Name) select Name from #B.Entities() skip 2";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("005")] },
            { "#B", [new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("005")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["005"], ["005"]);
    }

    [TestMethod]
    public void MultipleUnionAllTest()
    {
        var query = @"
select Name from #A.Entities() union all (Name)
select Name from #A.Entities() union all (Name)
select Name from #A.Entities() union all (Name)
select Name from #A.Entities() union all (Name)
select Name from #A.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("005")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["005"], ["005"], ["005"], ["005"], ["005"]);
    }

    [TestMethod]
    public void UnionAllWhenMultipleSelectsCombinedWithUnionAllWithinCteExpression_ShouldSucceed()
    {
        var query = @"
with p as (
    select 1 as Id, 'EMPTY' as Name from #A.Entities()
    union all (Name)
    select 2 as Id, 'EMPTY2' as Name from #A.Entities()
    union all (Name)
    select 3 as Id, 'EMPTY3' as Name from #A.Entities()
)
select Id, Name from p
";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("005")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Id", typeof(int)),
            ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            [1, "EMPTY"], [2, "EMPTY2"], [3, "EMPTY3"]);
    }

    [TestMethod]
    public void UnionWhenMultipleSelectsCombinedWithUnionWithinCteExpression_ShouldSucceed()
    {
        var query = @"
with p as (
    select 1 as Id, 'EMPTY' as Name from #A.Entities()
    union (Name)
    select 2 as Id, 'EMPTY2' as Name from #A.Entities()
    union (Name)
    select 3 as Id, 'EMPTY3' as Name from #A.Entities()
)
select Id, Name from p
";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("005")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Id", typeof(int)),
            ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            [1, "EMPTY"], [2, "EMPTY2"], [3, "EMPTY3"]);
    }

    [TestMethod]
    public void MultipleUnionAllWithSkipTest()
    {
        var query = @"
select Name from #A.Entities() skip 1
union all (Name)
select Name from #B.Entities() skip 2
union all (Name)
select Name from #C.Entities() skip 3";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("005")] },
            { "#B", [new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("005")] },
            {
                "#C",
                [
                    new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("004"), new BasicEntity("005")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["005"], ["005"], ["005"]);
    }

    [TestMethod]
    public void UnionWithoutDuplicatedKeysTest()
    {
        var query = @"select Name from #A.Entities() union (Name) select Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] },
            { "#B", [new BasicEntity("003"), new BasicEntity("004")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["001"], ["002"], ["003"], ["004"]);
    }

    [TestMethod]
    public void UnionWithDuplicatedKeysTest()
    {
        var query = @"select Name from #A.Entities() union (Name) select Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] },
            { "#B", [new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("005")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["001"], ["002"], ["005"]);
    }

    [TestMethod]
    public void MultipleUnionsWithDuplicatedKeysTest()
    {
        var query =
            @"select Name from #A.Entities() union (Name) select Name from #B.Entities() union (Name) select Name from #C.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("001"), new BasicEntity("002")] },
            { "#C", [new BasicEntity("005")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["001"], ["002"], ["005"]);
    }

    [TestMethod]
    public void MultipleUnionsWithoutDuplicatedKeysTest()
    {
        var query =
            @"select Name from #A.Entities() union (Name) select Name from #B.Entities() union (Name) select Name from #C.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002")] },
            { "#C", [new BasicEntity("005")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["001"], ["002"], ["005"]);
    }

    [TestMethod]
    public void MultipleUnionsComplexTest()
    {
        var query =
            @"
select Name from #A.Entities() union (Name)
select Name from #B.Entities() union (Name)
select Name from #C.Entities() union (Name)
select Name from #D.Entities()";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002")] },
            { "#C", [new BasicEntity("005")] },
            { "#D", [new BasicEntity("007"), new BasicEntity("001")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["001"], ["002"], ["005"], ["007"]);
    }

    [TestMethod]
    public void UnionAllWithDuplicatedKeysTest()
    {
        var query = @"select Name from #A.Entities() union all (Name) select Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] },
            { "#B", [new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("005")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["001"], ["002"], ["001"], ["002"], ["005"]);
    }

    [TestMethod]
    public void MultipleUnionsAllWithDuplicatedKeysTest()
    {
        var query =
            @"select Name from #A.Entities() union all (Name) select Name from #B.Entities() union all (Name) select Name from #C.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("001"), new BasicEntity("002")] },
            { "#C", [new BasicEntity("005")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["001"], ["001"], ["002"], ["005"]);
    }

    [TestMethod]
    public void MultipleUnionsAllWithoutDuplicatedKeysTest()
    {
        var query =
            @"select Name from #A.Entities() union all (Name) select Name from #B.Entities() union all (Name) select Name from #C.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002")] },
            { "#C", [new BasicEntity("005")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["001"], ["002"], ["005"]);
    }

    [TestMethod]
    public void MultipleUnionsAllComplexTest()
    {
        var query =
            @"
select Name from #A.Entities() union all (Name)
select Name from #B.Entities() union all (Name)
select Name from #C.Entities() union all (Name)
select Name from #D.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002")] },
            { "#C", [new BasicEntity("005")] },
            { "#D", [new BasicEntity("007"), new BasicEntity("001")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["001"], ["002"], ["005"], ["007"], ["001"]);
    }

    [TestMethod]
    public void UnionAllWithoutDuplicatedKeysTest()
    {
        var query = @"select Name from #A.Entities() union all (Name) select Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] },
            { "#B", [new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["001"], ["002"], ["003"], ["004"], ["001"]);
    }

    [TestMethod]
    public void UnionSameSourceTest()
    {
        var query =
            @"
select Name from #A.Entities() where Name = '001'
union (Name)
select Name from #A.Entities() where Name = '002'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["001"], ["002"]);
    }

    [TestMethod]
    public void UnionMultipleTimesSameSourceTest()
    {
        var query =
            @"
select Name from #A.Entities() where Name = '001'
union (Name)
select Name from #A.Entities() where Name = '002'
union (Name)
select Name from #A.Entities() where Name = '003'
union (Name)
select Name from #A.Entities() where Name = '004'
union (Name)
select Name from #A.Entities() where Name = '005'";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("003"), new BasicEntity("004"),
                    new BasicEntity("005")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["001"], ["002"], ["003"], ["004"], ["005"]);
    }

    [TestMethod]
    public void WhenWrongTypeBetweenUnions_ShouldFail()
    {
        var query = @"select Name from #A.Entities() union (Name) select 1 as Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3020_SetOperatorColumnTypes, DiagnosticPhase.Bind, "same types");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenWrongTypeBetweenUnionAll_ShouldFail()
    {
        var query = @"select Name from #A.Entities() union all (Name) select 1 as Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3020_SetOperatorColumnTypes, DiagnosticPhase.Bind, "same types");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenUnionHasEmptyKeyList_ShouldUseAllProjectedFields()
    {
        var query = @"select Name from #A.Entities() union () select Name as Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["001"]);
    }

    [TestMethod]
    public void WhenUnionAllHasEmptyKeyList_ShouldAppendRows()
    {
        var query = @"select Name from #A.Entities() union all () select Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["001"], ["001"]);
    }

}
