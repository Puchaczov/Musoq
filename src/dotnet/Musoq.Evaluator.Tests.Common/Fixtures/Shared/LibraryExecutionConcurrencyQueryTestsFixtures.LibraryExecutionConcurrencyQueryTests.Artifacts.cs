using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionConcurrencyQueryTestsFixtures
{
    public sealed record ReservedColumnRow(string Token, string Provider, string Logger, string QueryContext);
}
