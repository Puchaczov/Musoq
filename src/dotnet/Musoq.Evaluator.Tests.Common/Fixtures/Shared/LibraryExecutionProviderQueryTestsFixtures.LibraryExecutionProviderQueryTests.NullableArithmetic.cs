using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionProviderQueryTestsFixtures
{

    public sealed record NullableConversionInput(int Position, string? Text);
    public sealed record NullableArithmeticInput(int Position, long? Value);

}
