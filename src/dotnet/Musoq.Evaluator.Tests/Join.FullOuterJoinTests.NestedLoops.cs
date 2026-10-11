using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class JoinFullOuterJoinTests
{
    [TestMethod]
    public void NestedFullOuterJoin_EmptyInputs_ReturnNoRows()
    {
        var table = Run("select a.Id, b.Id from #A.entities() a full outer join #B.entities() b on a.Id = b.Id",
            CreateSources([], []),
            new CompilationOptions(useHashJoin: false, useSortMergeJoin: false));

        AssertRows(table);
    }

    [TestMethod]
    public void NestedFullOuterJoin_EmptyLeft_PreservesRightRows()
    {
        var table = Run("select a.Id, b.Id from #A.entities() a full outer join #B.entities() b on a.Id = b.Id",
            CreateSources([], [new BasicEntity { Id = 2 }]),
            new CompilationOptions(useHashJoin: false, useSortMergeJoin: false));

        AssertRows(table, (null, 2));
    }

    [TestMethod]
    public void NestedFullOuterJoin_EmptyRight_PreservesLeftRows()
    {
        var table = Run("select a.Id, b.Id from #A.entities() a full outer join #B.entities() b on a.Id = b.Id",
            CreateSources([new BasicEntity { Id = 1 }], []),
            new CompilationOptions(useHashJoin: false, useSortMergeJoin: false));

        AssertRows(table, (1, null));
    }

    [TestMethod]
    [DataRow("select a.Id, b.Id from #A.entities() a full outer join #B.entities() b on a.NullableValue = b.NullableValue")]
    [DataRow("with aa as (select Id, NullableValue from #A.entities()), bb as (select Id, NullableValue from #B.entities()) " +
             "select a.Id, b.Id from aa a full outer join bb b on a.NullableValue = b.NullableValue")]
    public void NestedFullOuterJoin_DuplicatesAndNullableKeys_PreserveOccurrences(string query)
    {
        var duplicate = new BasicEntity { Id = 2, NullableValue = 1 };
        var table = Run(query,
            CreateSources([new BasicEntity { Id = 1, NullableValue = 1 }, new BasicEntity { Id = 3 }],
                [duplicate, duplicate, new BasicEntity { Id = 4 }]),
            new CompilationOptions(useHashJoin: false, useSortMergeJoin: false));

        AssertRows(table, (1, 2), (1, 2), (3, null), (null, 4));
    }
}
