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
            { "#B", [new BasicEntity("003", "", 0), new BasicEntity("004", "", 0)] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["001"], ["002"], ["003"], ["004"]);
    }

    [TestMethod]
    public void AliasedUnionWithDifferentColumnsAsAKeyTest()
    {
        var query = (
            "select\n"
            + "    a.Name as a1,\n"
            + "    b.Value\n"
            + "from #A.Entities() a\n"
            + "cross apply a.ToCharArray(a.Name) b\n"
            + "union (Name)\n"
            + "select\n"
            + "    a.Name as a1,\n"
            + "    b.Value\n"
            + "from #A.Entities() a\n"
            + "cross apply a.ToCharArray(a.Name) b"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a1", typeof(string)), ("Value", typeof(char)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["001", '0'], ["002", '0']);
    }

    [TestMethod]
    public void UnionWithSkipTest()
    {
        var query = (
            "\n"
            + "with left_slice as (select Name from #A.Entities() skip 1),\n"
            + "right_slice as (select Name from #B.Entities() skip 2)\n"
            + "select Name from left_slice union (Name) select Name from right_slice"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] },
            { "#B", [new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("005")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["002"], ["005"]);
    }

    [TestMethod]
    public void UnionAllWithSkipTest()
    {
        var query = (
            "\n"
            + "with left_slice as (select Name from #A.Entities() skip 1),\n"
            + "right_slice as (select Name from #B.Entities() skip 2)\n"
            + "select Name from left_slice union all (Name) select Name from right_slice"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("005")] },
            { "#B", [new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("005")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["005"], ["005"]);
    }

    [TestMethod]
    public void MultipleUnionAllTest()
    {
        var query = (
            "\n"
            + "select Name from #A.Entities() union all (Name)\n"
            + "select Name from #A.Entities() union all (Name)\n"
            + "select Name from #A.Entities() union all (Name)\n"
            + "select Name from #A.Entities() union all (Name)\n"
            + "select Name from #A.Entities()"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>> { { "#A", [new BasicEntity("005")] } };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["005"], ["005"], ["005"], ["005"], ["005"]);
    }

    [TestMethod]
    public void UnionAllWhenMultipleSelectsCombinedWithUnionAllWithinCteExpression_ShouldSucceed()
    {
        var query = (
            "\n"
            + "with p as (\n"
            + "    select 1 as Id, 'EMPTY' as Name from #A.Entities()\n"
            + "    union all (Name)\n"
            + "    select 2 as Id, 'EMPTY2' as Name from #A.Entities()\n"
            + "    union all (Name)\n"
            + "    select 3 as Id, 'EMPTY3' as Name from #A.Entities()\n"
            + ")\n"
            + "select Id, Name from p\n"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>> { { "#A", [new BasicEntity("005")] } };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Id", typeof(int)), ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [1, "EMPTY"], [2, "EMPTY2"], [3, "EMPTY3"]);
    }

    [TestMethod]
    public void UnionWhenMultipleSelectsCombinedWithUnionWithinCteExpression_ShouldSucceed()
    {
        var query = (
            "\n"
            + "with p as (\n"
            + "    select 1 as Id, 'EMPTY' as Name from #A.Entities()\n"
            + "    union (Name)\n"
            + "    select 2 as Id, 'EMPTY2' as Name from #A.Entities()\n"
            + "    union (Name)\n"
            + "    select 3 as Id, 'EMPTY3' as Name from #A.Entities()\n"
            + ")\n"
            + "select Id, Name from p\n"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>> { { "#A", [new BasicEntity("005")] } };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Id", typeof(int)), ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [1, "EMPTY"], [2, "EMPTY2"], [3, "EMPTY3"]);
    }

    [TestMethod]
    public void MultipleUnionAllWithSkipTest()
    {
        var query = (
            "\n"
            + "with first_slice as (select Name from #A.Entities() skip 1),\n"
            + "second_slice as (select Name from #B.Entities() skip 2),\n"
            + "third_slice as (select Name from #C.Entities() skip 3)\n"
            + "select Name from first_slice\n"
            + "union all (Name) select Name from second_slice\n"
            + "union all (Name) select Name from third_slice"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("005")] },
            { "#B", [new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("005")] },
            { "#C", [new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("004"), new BasicEntity("005")] },
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
            { "#B", [new BasicEntity("003"), new BasicEntity("004")] },
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
            { "#B", [new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("005")] },
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
            { "#C", [new BasicEntity("005")] },
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
            { "#C", [new BasicEntity("005")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["001"], ["002"], ["005"]);
    }

    [TestMethod]
    public void MultipleUnionsComplexTest()
    {
        var query = (
            "\n"
            + "select Name from #A.Entities() union (Name)\n"
            + "select Name from #B.Entities() union (Name)\n"
            + "select Name from #C.Entities() union (Name)\n"
            + "select Name from #D.Entities()"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002")] },
            { "#C", [new BasicEntity("005")] },
            { "#D", [new BasicEntity("007"), new BasicEntity("001")] },
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
            { "#B", [new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("005")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["001"], ["002"], ["001"], ["002"], ["005"]);
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
            { "#C", [new BasicEntity("005")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["001"], ["001"], ["002"], ["005"]);
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
            { "#C", [new BasicEntity("005")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["001"], ["002"], ["005"]);
    }

    [TestMethod]
    public void MultipleUnionsAllComplexTest()
    {
        var query = (
            "\n"
            + "select Name from #A.Entities() union all (Name)\n"
            + "select Name from #B.Entities() union all (Name)\n"
            + "select Name from #C.Entities() union all (Name)\n"
            + "select Name from #D.Entities()"
        );
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001")] },
            { "#B", [new BasicEntity("002")] },
            { "#C", [new BasicEntity("005")] },
            { "#D", [new BasicEntity("007"), new BasicEntity("001")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["001"], ["002"], ["005"], ["007"], ["001"]);
    }

    [TestMethod]
    public void UnionAllWithoutDuplicatedKeysTest()
    {
        var query = @"select Name from #A.Entities() union all (Name) select Name from #B.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] },
            { "#B", [new BasicEntity("003"), new BasicEntity("004"), new BasicEntity("001")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["001"], ["002"], ["003"], ["004"], ["001"]);
    }

    [TestMethod]
    public void UnionSameSourceTest()
    {
        var query = (
            "\n"
            + "select Name from #A.Entities() where Name = '001'\n"
            + "union (Name)\n"
            + "select Name from #A.Entities() where Name = '002'"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["001"], ["002"]);
    }

    [TestMethod]
    public void UnionMultipleTimesSameSourceTest()
    {
        var query = (
            "\n"
            + "select Name from #A.Entities() where Name = '001'\n"
            + "union (Name)\n"
            + "select Name from #A.Entities() where Name = '002'\n"
            + "union (Name)\n"
            + "select Name from #A.Entities() where Name = '003'\n"
            + "union (Name)\n"
            + "select Name from #A.Entities() where Name = '004'\n"
            + "union (Name)\n"
            + "select Name from #A.Entities() where Name = '005'"
        );

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity("001"),
                    new BasicEntity("002"),
                    new BasicEntity("003"),
                    new BasicEntity("004"),
                    new BasicEntity("005"),
                ]
            },
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["001"], ["002"], ["003"], ["004"], ["005"]);
    }

    [TestMethod]
    public void WhenWrongTypeBetweenUnions_ShouldFail()
    {
        var query = @"select Name from #A.Entities() union (Name) select 1 as Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>> { { "#A", [new BasicEntity("001")] } };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3020_SetOperatorColumnTypes, DiagnosticPhase.Bind, "same types");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenWrongTypeBetweenUnionAll_ShouldFail()
    {
        var query = @"select Name from #A.Entities() union all (Name) select 1 as Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>> { { "#A", [new BasicEntity("001")] } };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3020_SetOperatorColumnTypes, DiagnosticPhase.Bind, "same types");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenUnionHasEmptyKeyList_ShouldUseAllProjectedFields()
    {
        var query = @"select Name from #A.Entities() union () select Name as Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>> { { "#A", [new BasicEntity("001")] } };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["001"]);
    }

    [TestMethod]
    public void WhenUnionAllHasEmptyKeyList_ShouldAppendRows()
    {
        var query = @"select Name from #A.Entities() union all () select Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>> { { "#A", [new BasicEntity("001")] } };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["001"], ["001"]);
    }
}
