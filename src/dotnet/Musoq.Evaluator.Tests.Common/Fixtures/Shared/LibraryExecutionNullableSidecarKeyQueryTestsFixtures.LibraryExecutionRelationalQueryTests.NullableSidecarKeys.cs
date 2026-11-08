using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionNullableSidecarKeyQueryTestsFixtures
{

    public sealed record NullableKeyInput(int Position, int Key, int? NullableKey, string Tag);

}
