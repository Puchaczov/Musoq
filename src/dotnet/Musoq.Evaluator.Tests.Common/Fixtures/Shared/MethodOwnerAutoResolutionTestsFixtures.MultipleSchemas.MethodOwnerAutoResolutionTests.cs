using System.Collections.Generic;
using System.Linq;
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

public partial class MethodOwnerAutoResolutionTestsFixtures
{

    internal static GenericSchema<TLibrary> CreateSchema<TLibrary>(BasicEntity[] source)
        where TLibrary : LibraryBase, new()
    {
        return new GenericSchema<TLibrary>(new Dictionary<string, (ISchemaTable SchemaTable, object RowSource)>
        {
            {
                "entities",
                (new BasicEntityTable(), new MultiRowSource<BasicEntity>(source))
            }
        });
    }

    public sealed class SharedOnlyLibraryA : LibraryBase;

    public sealed class SharedOnlyLibraryB : LibraryBase;

    public sealed class UniqueMethodLibraryA : LibraryBase
    {
        [BindableMethod]
        public decimal UniqueToA(decimal value)
        {
            return value * 2;
        }
    }

    public sealed class AmbiguousMethodLibraryA : LibraryBase
    {
        [BindableMethod]
        public decimal AmbiguousMethod(decimal value)
        {
            return value / 10;
        }
    }

    public sealed class AmbiguousMethodLibraryB : LibraryBase
    {
        [BindableMethod]
        public decimal AmbiguousMethod(decimal value)
        {
            return value / 20;
        }
    }

}
