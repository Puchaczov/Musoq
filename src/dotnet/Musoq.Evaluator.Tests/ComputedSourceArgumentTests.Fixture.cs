using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;

namespace Musoq.Evaluator.Tests;

public sealed partial class ComputedSourceArgumentTests
{
    private static readonly CompilationOptions Options = new(parallelizationMode: ParallelizationMode.None);

    private static CompiledQuery Compile(string sql, CapturingProvider provider) =>
        InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString(), provider, new TestsLoggerResolver(), Options);

    private static void Set(CompiledQuery query, string name, object? value) =>
        global::Musoq.Tests.Common.NativeParameterTestInputs.Set(query.TypedParameters, query.ParameterDefinitions, name, value);

    public sealed class ArgumentRow
    {
        public string? Path { get; init; }
        public string? Suffix { get; init; }
        public int? Number { get; init; }
    }

    private sealed class CapturingProvider : ISchemaProvider
    {
        internal List<object?[]> Metadata { get; } = [];
        internal List<ArgumentRow> Opened { get; } = [];
        internal int OpenCount { get; set; }
        internal bool RequiresMetadataValue { get; init; }
        internal string? TraceId { get; init; }
        public ISchema GetSchema(string schema) => new CapturingSchema(this);
    }

    private sealed class CapturingSchema(CapturingProvider owner) : SchemaBase("args", CreateLibrary())
    {
        private static readonly SchemaMethodInfo[] Constructors =
        [
            new("echo", new Musoq.Schema.Reflection.ConstructorInfo(typeof(Signature).GetConstructors().Single(), false,
                ("path", typeof(string)), ("suffix", typeof(string)), ("number", typeof(int?))))
        ];

        public override SchemaMethodInfo[] GetRawConstructors(SourceMetadataContext context) => Constructors;

        public override ISchemaTable GetTableByName(string name, SourceMetadataContext context, params object?[] arguments)
        {
            if (name == "echo")
            {
                owner.Metadata.Add(arguments);
                if (owner.RequiresMetadataValue && arguments.Length == 0)
                    throw new ArgumentException("This fixture requires a static path.");
            }
            return new GenericEntityTable<ArgumentRow>();
        }

        public override RowSource<T> GetRowSource<T, TArguments>(string name, SourceExecutionContext context, TArguments arguments)
        {
            owner.OpenCount++;
            ArgumentRow[] rows;
            if (name == "seed") rows = [new() { Path = "a", Number = 1 }, new() { Path = "b", Number = 2 }];
            else
            {
                var row = new ArgumentRow
                {
                    Path = arguments.GetValue<string>(0), Suffix = arguments.GetValue<string>(1),
                    Number = arguments.GetValue<int?>(2)
                };
                owner.Opened.Add(row);
                if (owner.TraceId is { } id) ArgumentLibrary.Record(id, "open");
                rows = [row];
            }
            return EnsureSourceType<T, ArgumentRow>(name, new GenericChunkSource<ArgumentRow>(rows,
                GenericEntityTable<ArgumentRow>.NameToIndexMap, GenericEntityTable<ArgumentRow>.IndexToObjectAccessMap));
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methods = new MethodsManager();
            methods.RegisterLibraries(new ArgumentLibrary());
            return new MethodsAggregator(methods);
        }
    }

    private sealed class Signature
    {
        public Signature(string? path, string? suffix = "", int? number = null) => _ = (path, suffix, number);
    }

    public sealed class ArgumentLibrary : LibraryBase
    {
        private static readonly ConcurrentDictionary<string, ConcurrentQueue<string>> Traces = new();
        internal static void Start(string id) => Traces[id] = new ConcurrentQueue<string>();
        internal static void Record(string id, string label) => Traces[id].Enqueue(label);
        internal static string[] End(string id) => Traces.TryRemove(id, out var trace) ? trace.ToArray() : [];

        [BindableMethod]
        [NonDeterministic]
        public string? Touch(string id, string label, string? value)
        {
            Record(id, label);
            return value;
        }

        [BindableMethod]
        public string? SourcePath([InjectSpecificSource(typeof(ArgumentRow))] ArgumentRow source, string suffix) =>
            source.Path + suffix;
    }
}
