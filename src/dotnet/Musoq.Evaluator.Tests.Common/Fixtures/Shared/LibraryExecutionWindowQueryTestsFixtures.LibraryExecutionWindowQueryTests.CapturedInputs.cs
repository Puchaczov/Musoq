using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionWindowQueryTestsFixtures
{
    public sealed class VolatileWindowRow(int position)
    {
        public int Position { get; } = position;
        public int ValueCalls { get; set; }
        public int FilterCalls { get; set; }
    }

    public sealed class VolatileWindowLibrary : LibraryBase
    {
        [BindableMethod, NonDeterministic]
        public static double? NextValue([InjectSpecificSource(typeof(VolatileWindowRow))] VolatileWindowRow row) =>
            row.Position + 10d * ++row.ValueCalls;

        [BindableMethod, NonDeterministic]
        public static bool AcceptRow([InjectSpecificSource(typeof(VolatileWindowRow))] VolatileWindowRow row)
        {
            row.FilterCalls++;
            return row.Position % 2 != 0;
        }
    }
}
