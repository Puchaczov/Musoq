using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionNativeConversionQueryTestsFixtures
{
    internal const string Query = "select Position, Amount::int as Rounded, Amount::double as Floating, " +
        "Amount::string as Text from #schema.first() order by Position";

    public sealed record Payload(int Value);
    public sealed record UnsupportedInput(Payload Payload);
    public sealed record Input(int Position, decimal? Amount);

}
