using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed partial class JoinSemiAntiCrossJoinTests : BasicEntityTestBase
{
    private Table RunJoinQuery(string query, CompilationOptions? options = null)
    {
        var sources = CreateJoinSources(CreateRightRows());
        var vm = options is null
            ? CreateAndRunVirtualMachine<BasicEntity>(query, sources)
            : CreateAndRunVirtualMachine(query, sources, options);

        return TableMaterializationTestHelper.Materialize(vm.Run(TokenSource.Token));
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateJoinSources(IEnumerable<BasicEntity> rightRows)
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("A1") { Id = 1, Population = 100 },
                    new BasicEntity("A2") { Id = 2, Population = 40 },
                    new BasicEntity("A3") { Id = 3, Population = 200 }
                ]
            },
            { "#B", rightRows }
        };
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateThreeWayJoinSources()
    {
        var sources = CreateJoinSources(CreateRightRows());
        sources.Add("#C", [
            new BasicEntity("C1") { Id = 1 },
            new BasicEntity("C3") { Id = 3 }
        ]);

        return sources;
    }

    private static BasicEntity[] CreateRightRows()
    {
        return [
            new BasicEntity("B1") { Id = 1, Population = 50 },
            new BasicEntity("B1Duplicate") { Id = 1, Population = 50 },
            new BasicEntity("B3") { Id = 3, Population = 150 }
        ];
    }
}
