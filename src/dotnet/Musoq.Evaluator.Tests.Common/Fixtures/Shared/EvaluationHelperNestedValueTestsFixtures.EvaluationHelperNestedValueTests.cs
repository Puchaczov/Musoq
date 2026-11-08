using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests.Runtime;

public partial class EvaluationHelperNestedValueTestsFixtures
{

    public sealed record OuterEntity(int Position, InnerEntity? Child);
    public sealed record InnerEntity(string Value);
    public sealed record DictionaryEntity(IReadOnlyDictionary<string, InnerEntity> Children);
    public sealed record IndexedEntity(IReadOnlyList<InnerEntity> Items, IReadOnlyDictionary<string, InnerEntity> Values);
    public sealed record DynamicEntity(TestDynamicObject Child);
    public sealed class ThrowingEntity
    {
        public string Value => throw new InvalidOperationException("getter");
    }
    public sealed class TestDynamicObject : DynamicObject;

}
