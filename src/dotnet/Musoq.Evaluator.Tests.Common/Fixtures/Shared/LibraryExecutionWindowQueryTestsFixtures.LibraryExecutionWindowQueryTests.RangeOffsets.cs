using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionWindowQueryTestsFixtures
{

    public sealed record RangeOffsetRow<T>(int Position, T? Key, int? Value, int? ReferenceKey) where T : struct;

}
