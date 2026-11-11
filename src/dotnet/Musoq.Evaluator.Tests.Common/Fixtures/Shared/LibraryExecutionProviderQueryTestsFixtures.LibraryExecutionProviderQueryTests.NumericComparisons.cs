using System;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionProviderQueryTestsFixtures
{
    public sealed record FloatingDecimalComparisonRow<T>(int Position, T? Value, decimal? Amount)
        where T : struct;
}
