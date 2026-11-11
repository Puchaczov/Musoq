using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionNativeSourceArgumentQueryTestsFixtures
{
    internal const string ApplyQuery = """
        select a.Id as Id, b.Amount as Amount, b.Label as Label
        from #native.rows() a
        cross apply #native.echo(a.Amount, a.Label) b
        order by Id
        """;

    internal static ISchemaProvider CreateProvider() => new NativeProvider(new NativeSchema());

    public sealed record NativeEntity(int Id, decimal? Amount, string Label);

    public sealed class EchoSignature(decimal? amount, string label)
    {
        public decimal? Amount => amount;
        public string Label => label;
    }

    internal sealed class NativeProvider(NativeSchema schema) : ISchemaProvider
    {
        public ISchema GetSchema(string name) => schema;
    }

    internal sealed class NativeSchema() : SchemaBase("native", new MethodsAggregator(new MethodsManager()))
    {
        public List<(decimal?, string)> Invocations { get; } = [];

        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext context,
            params object?[] parameters
        ) => new GenericEntityTable<NativeEntity>();

        public override SchemaMethodInfo[] GetRawConstructors(SourceMetadataContext context) =>
            [.. GetRawConstructors("rows", context), .. GetRawConstructors("echo", context)];

        public override SchemaMethodInfo[] GetRawConstructors(string name, SourceMetadataContext context) =>
            name == "echo"
                ?
                [
                    new SchemaMethodInfo(
                        name,
                        new Musoq.Schema.Reflection.ConstructorInfo(
                            typeof(EchoSignature).GetConstructors().Single(),
                            false,
                            ("amount", typeof(decimal?)),
                            ("label", typeof(string))
                        )
                    ),
                ]
                : [new SchemaMethodInfo(name, Musoq.Schema.Reflection.ConstructorInfo.Empty())];

        public override RowSource<T> GetRowSource<T, TArguments>(
            string name,
            SourceExecutionContext context,
            TArguments arguments
        )
        {
            NativeEntity[] rows;
            if (name == "echo")
            {
                var amount = arguments.GetValue<decimal?>(0);
                var label = arguments.GetValue<string>(1);
                Invocations.Add((amount, label));
                rows = [new NativeEntity(0, amount, label)];
            }
            else
            {
                rows =
                [
                    new NativeEntity(1, 1.25m, "first"),
                    new NativeEntity(2, null, "empty"),
                    new NativeEntity(3, -4m, "last"),
                ];
            }
            return EnsureSourceType<T, NativeEntity>(
                name,
                new EntitySource<NativeEntity>(
                    [rows],
                    new Dictionary<string, int>(),
                    new Dictionary<int, Func<NativeEntity, object?>>()
                )
            );
        }
    }
}
