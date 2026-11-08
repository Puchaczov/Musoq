using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionMixedKeyQueryTestsFixtures
{
    public const string MixedKeyQuery = "select a.Position as LeftPosition, b.Position as RightPosition " +
        "from #schema.first() a inner join #schema.first() b " +
        "on a.IntKey = b.LongKey and a.Label = b.Label order by LeftPosition, RightPosition";

    public static MixedInput[] CreateRows() =>
        [new(1, 10, 10, "A"), new(2, 10, 10, "A"), new(3, null, null, "A"),
         new(4, -7, -7, "B"), new(5, 10, 10, "a"), new(6, 20, 30, "A")];
    public sealed record MixedInput(int Position, int? IntKey, long? LongKey, string Label);
    public sealed record SignedInput(int Position, long SignedKey, ulong UnsignedKey, string Label);

}
