using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionNativeRowQueryTestsFixtures
{
    public sealed class MutableProvider(Musoq.Schema.ISchema schema) : Musoq.Schema.ISchemaProvider
    {
        public Musoq.Schema.ISchema Schema { get; set; } = schema;

        public Musoq.Schema.ISchema GetSchema(string name) => Schema;
    }

    public static class FirstOwner
    {
        public sealed record SharedRow(int Value);
    }

    public static class SecondOwner
    {
        public sealed record SharedRow(int Value);
    }

    public const string NativeTupleQuery =
        "select Number as Item1, Count(Position) as Item2 from #schema.first() group by Number";
}
