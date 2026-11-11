using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionProviderQueryTestsFixtures
{
    internal const string IndexedMembersQuery =
        "select Position, Children[-1].Value as Last, Children[8].Value as Missing, "
        + "ChildrenByName['present'].Value as Present, ChildrenByName['missing'].Value as MissingKey "
        + "from #schema.first() order by Position";

    public sealed record IndexedChild(int Value);

    public sealed record IndexedParent(
        int Position,
        IndexedChild[]? Children,
        IReadOnlyDictionary<string, IndexedChild> ChildrenByName
    );
}
