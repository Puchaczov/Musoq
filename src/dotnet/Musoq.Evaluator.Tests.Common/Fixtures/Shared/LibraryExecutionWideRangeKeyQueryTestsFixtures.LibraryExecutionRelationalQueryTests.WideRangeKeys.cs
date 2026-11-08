using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionWideRangeKeyQueryTestsFixtures
{

    public const string WideRangeQuery = """
            select a.Position as FirstPosition, b.Position as SecondPosition
            from #schema.first() a inner join #schema.first() b
                on a.K1 = b.K1 and a.K2 = b.K2 and a.K3 = b.K3
                and a.K4 = b.K4 and a.K5 = b.K5 and a.K6 = b.K6
                and a.K7 = b.K7 and a.K8 = b.K8 and a.K9 = b.K9
                and a.Amount > b.Amount
            order by FirstPosition, SecondPosition
            """;

    public static WideRangeInput[] CreateRows() =>
        [
            new(1, 10m, 1, 2, 3, 4, 5, 6, 7, 8, "A"),
            new(2, 20m, 1, 2, 3, 4, 5, 6, 7, 8, "A"),
            new(3, 30m, 1, 2, 3, 4, 5, 6, 7, 9, "A"),
            new(4, 40m, 1, 2, 3, 4, 5, 6, 7, 8, null)
        ];

    public sealed record WideRangeInput(int Position, decimal Amount,
        int K1, int K2, int K3, int K4, int K5, int K6, int K7, int K8, string? K9);

}
