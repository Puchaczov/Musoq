using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionProviderQueryTestsFixtures
{
    internal const string PartialPayloadQuery =
        "binary Packet { Amount: int le, Currency: byte }; "
        + "select s.Position as Position, p.ParsedFields as ParsedFields, p.ErrorField as ErrorField, "
        + "p.BytesConsumed as BytesConsumed from #schema.first() s "
        + "cross apply PartialInterpret<Packet>(s.Data) p order by s.Position";

    public sealed record PartialBytes(int Position, byte[] Data);
}
