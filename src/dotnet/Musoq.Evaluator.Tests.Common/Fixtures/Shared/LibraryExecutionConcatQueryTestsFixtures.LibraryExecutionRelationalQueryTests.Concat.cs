using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionConcatQueryTestsFixtures
{
    public sealed class UninferredLibrary : LibraryBase
    {
        [BindableMethod]
        public string Uninferred<T, TResult>(T value) =>
            throw new System.InvalidOperationException("Uninferred declarations must not execute.");
    }

    public sealed record ConcatInput(int Position, string? Label, int? Value);
}
