using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionWindowQueryTestsFixtures
{
    public readonly record struct WindowOutput(int Position, decimal? Total);

    public readonly record struct SeededWindowOutput(int Position, decimal Total);
}
