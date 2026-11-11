using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Parser.Diagnostics;
using Musoq.Plugins;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionBoundaryQueryTestsFixtures
{
    public sealed record ErasedNumericInput(string Label, object Value);

    public sealed record ErasedDictionaryInput(IReadOnlyDictionary<string, object> Values);

    public sealed record UnresolvedNestedInput(ExpandoObject Complex);

    public sealed record ConcreteNumericInput(string Label, decimal? Value);
}
