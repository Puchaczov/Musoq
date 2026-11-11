using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Evaluator.Tests.Schema.Multi;
using Musoq.Parser.Diagnostics;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

public partial class AggregateOwnerAmbiguityTestsFixtures
{
    internal static GenericSchema<TLibrary> CreateSchema<TLibrary>(BasicEntity[] source)
        where TLibrary : LibraryBase, new()
    {
        return new GenericSchema<TLibrary>(
            new Dictionary<string, (ISchemaTable SchemaTable, object RowSource)>
            {
                { "entities", (new BasicEntityTable(), new MultiRowSource<BasicEntity>(source)) },
            }
        );
    }

    public sealed class AggregateLibraryA : LibraryBase
    {
        [AggregateFunction(typeof(AmbiguousAggregateKernelA), Name = nameof(AmbiguousAgg), Inline = true)]
        public int AmbiguousAgg(decimal? value)
        {
            return AggregateFunction.NotInvoked<int>();
        }

        public static class AmbiguousAggregateKernelA
        {
            /// <summary>Initializes independently owned kernel state.</summary>
            public static void Reset(ref State state) => state = default;

            public struct State
            {
                public int Value;
            }

            public static void Set(ref State state, decimal? value)
            {
                state.Value = 10;
            }

            public static int Get(in State state)
            {
                return state.Value;
            }

            public static void Merge(ref State target, in State source)
            {
                if (source.Value != 0)
                    target.Value = source.Value;
            }
        }
    }

    public sealed class AggregateLibraryB : LibraryBase
    {
        [AggregateFunction(typeof(AmbiguousAggregateKernelB), Name = nameof(AmbiguousAgg), Inline = true)]
        public int AmbiguousAgg(decimal? value)
        {
            return AggregateFunction.NotInvoked<int>();
        }

        public static class AmbiguousAggregateKernelB
        {
            /// <summary>Initializes independently owned kernel state.</summary>
            public static void Reset(ref State state) => state = default;

            public struct State
            {
                public int Value;
            }

            public static void Set(ref State state, decimal? value)
            {
                state.Value = 20;
            }

            public static int Get(in State state)
            {
                return state.Value;
            }

            public static void Merge(ref State target, in State source)
            {
                if (source.Value != 0)
                    target.Value = source.Value;
            }
        }
    }
}
