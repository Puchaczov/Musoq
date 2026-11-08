using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionProviderQueryTestsFixtures
{
    internal const string OuterContainsQuery = "select a.Id as Id from #schema.first() a left join #schema.first() b " +
        "on b.Id = a.Id + 1 where b.Label contains ('match', null) order by a.Id";

    internal const string FilteredNotInQuery = "select Count(*) filter (where Label not in ('match', null)) as Present from #schema.first()";

    internal const string AggregateMembershipInputQuery = "select Count(Label in ('match')) as Present from #schema.first()";

    internal static OuterMembershipRow[] CreateOuterMembershipRows() =>
        [new(1, "match"), new(2, "other"), new(3, null)];

    public sealed record OuterMembershipRow(int Id, string? Label);

}
