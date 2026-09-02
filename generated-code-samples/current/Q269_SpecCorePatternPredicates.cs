// === Parsed Query ===
/*
select Name, Name like 'A%' as IsLike, Name not like 'Z%' as IsNotLike, Name rlike '^[A-Z]' as IsRlike, Name not rlike '^$' as IsNotRlike from #A.entities() where any(Name, City) like '%a%' and all(Name, City) not like '%z%'
*/

// === Logical Plan ===
/*
MultiStatement
  Project [ko3iko.Name as Name, ko3iko.Name LIKE 'A%' as IsLike, NOT ko3iko.Name LIKE 'Z%' as IsNotLike, ko3iko.Name RLIKE '^[A-Z]' as IsRlike, NOT ko3iko.Name RLIKE '^$' as IsNotRlike]
    Filter [((ko3iko.Name LIKE '%a%' OR ko3iko.City LIKE '%a%') AND (NOT ko3iko.Name LIKE '%z%' AND NOT ko3iko.City LIKE '%z%'))]
      SchemaScan [#A.entities() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [ko3iko.Name as Name, ko3iko.Name LIKE 'A%' as IsLike, NOT ko3iko.Name LIKE 'Z%' as IsNotLike, ko3iko.Name RLIKE '^[A-Z]' as IsRlike, NOT ko3iko.Name RLIKE '^$' as IsNotRlike]
    PhysicalFilter [((ko3iko.Name LIKE '%a%' OR ko3iko.City LIKE '%a%') AND (NOT ko3iko.Name LIKE '%z%' AND NOT ko3iko.City LIKE '%z%'))]
      PhysicalSchemaScan [#A.entities() as ko3iko] [pushdown: ((1 = 1) OR (1 = 1)), NOT (1 = 1), NOT (1 = 1)]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: BasicEntity]
      Name: string <- property Name
      City: string <- property City
    Generated [ResultRow0]
      Name: string <- field Name
      IsLike: bool <- field IsLike
      IsNotLike: bool <- field IsNotLike
      IsRlike: bool <- field IsRlike
      IsNotRlike: bool <- field IsNotRlike

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: BasicEntity] -> ko3ikoRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [Where]
    PhaseBoundary [Select]
    ChunkedForEach [ko3iko in ko3ikoRows]
      Let [name: string = ko3iko.Name]
      Let [city: string = ko3iko.City]
      If [((name LIKE '%a%' OR city LIKE '%a%') AND (NOT name LIKE '%z%' AND NOT city LIKE '%z%'))]
        AppendShape [result <- ResultShape0(Name: name, IsLike: name LIKE 'A%', IsNotLike: NOT name LIKE 'Z%', IsRlike: name RLIKE '^[A-Z]', IsNotRlike: NOT name RLIKE '^$')]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q269_SpecCorePatternPredicates
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Musoq.Schema;
    using Musoq.Schema.Optimization;
    using Musoq.Evaluator;
    using Musoq.Evaluator.Tables;
    using Musoq.Evaluator.Helpers;
    using Musoq.Evaluator.Runtime;
    using Musoq.Schema.DataSources;
    using System.Linq;

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Name", typeof(string), 0),
            new Column("IsLike", typeof(bool), 1),
            new Column("IsNotLike", typeof(bool), 2),
            new Column("IsRlike", typeof(bool), 3),
            new Column("IsNotRlike", typeof(bool), 4)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 10), new Column("City", typeof(string), 11) });
        public ILogger Logger { get; set; }
        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = Array.Empty<ScriptParameterContract>();
        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = Array.Empty<ScriptParameterDefinition>();
        public IDictionary<string, System.Object> Parameters { get; } = new Dictionary<string, System.Object>(StringComparer.Ordinal);
        public ISchemaProvider Provider { get; set; }
        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; }

        public event DataSourceEventHandler DataSourceProgress;
        public event QueryPhaseEventHandler PhaseChanged;
        public event QueryProgressEventHandler QueryProgress;
        public Table Run(CancellationToken token)
        {
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Name, __musoqShapeRow.IsLike, __musoqShapeRow.IsNotLike, __musoqShapeRow.IsRlike, __musoqShapeRow.IsNotRlike);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __ko3ikoSchema = provider.GetSchema("#A");
                var ko3ikoRowsSource = __ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                OnPhaseChanged("compiled", QueryPhase.Where);
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var ko3ikoChunk in ko3ikoRows)
                {
                    if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> ko3ikoChunkView)
                    {
                        if (ko3ikoChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] ko3ikoChunkViewArray)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewArray[ko3ikoChunkViewOffset + ko3ikoIndex];
                                string name = ko3iko.Name;
                                string city = ko3iko.City;
                                if (((new Musoq.Evaluator.Operators().Like(name, "%a%") || new Musoq.Evaluator.Operators().Like(city, "%a%")) && ((!new Musoq.Evaluator.Operators().Like(name, "%z%")) && (!new Musoq.Evaluator.Operators().Like(city, "%z%")))))
                                {
                                    yield return new ResultShape0(name, new Musoq.Evaluator.Operators().Like(name, "A%"), (!new Musoq.Evaluator.Operators().Like(name, "Z%")), new Musoq.Evaluator.Operators().RLike(name, "^[A-Z]"), (!new Musoq.Evaluator.Operators().RLike(name, "^$")));
                                }
                            }

                            continue;
                        }

                        if (ko3ikoChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> ko3ikoChunkViewList)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewList[ko3ikoChunkViewOffset + ko3ikoIndex];
                                string name = ko3iko.Name;
                                string city = ko3iko.City;
                                if (((new Musoq.Evaluator.Operators().Like(name, "%a%") || new Musoq.Evaluator.Operators().Like(city, "%a%")) && ((!new Musoq.Evaluator.Operators().Like(name, "%z%")) && (!new Musoq.Evaluator.Operators().Like(city, "%z%")))))
                                {
                                    yield return new ResultShape0(name, new Musoq.Evaluator.Operators().Like(name, "A%"), (!new Musoq.Evaluator.Operators().Like(name, "Z%")), new Musoq.Evaluator.Operators().RLike(name, "^[A-Z]"), (!new Musoq.Evaluator.Operators().RLike(name, "^$")));
                                }
                            }

                            continue;
                        }
                    }

                    for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunk.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                    {
                        if ((ko3ikoIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var ko3iko = ko3ikoChunk[ko3ikoIndex];
                        string name = ko3iko.Name;
                        string city = ko3iko.City;
                        if (((new Musoq.Evaluator.Operators().Like(name, "%a%") || new Musoq.Evaluator.Operators().Like(city, "%a%")) && ((!new Musoq.Evaluator.Operators().Like(name, "%z%")) && (!new Musoq.Evaluator.Operators().Like(city, "%z%")))))
                        {
                            yield return new ResultShape0(name, new Musoq.Evaluator.Operators().Like(name, "A%"), (!new Musoq.Evaluator.Operators().Like(name, "Z%")), new Musoq.Evaluator.Operators().RLike(name, "^[A-Z]"), (!new Musoq.Evaluator.Operators().RLike(name, "^$")));
                        }
                    }
                }
            }
            finally
            {
                try
                {
                    __musoqProgressContext?.CompleteQueryProgress();
                }
                finally
                {
                    OnPhaseChanged("compiled", QueryPhase.End);
                }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private void OnDataSourceProgress(object sender, DataSourceEventArgs e)
        {
            DataSourceProgress?.Invoke(this, e);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private void OnPhaseChanged(string queryId, QueryPhase phase)
        {
            PhaseChanged?.Invoke(this, new QueryPhaseEventArgs(queryId, phase));
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, bool __value1, bool __value2, bool __value3, bool __value4)
            {
                Name = __value0;
                IsLike = __value1;
                IsNotLike = __value2;
                IsRlike = __value3;
                IsNotRlike = __value4;
            }

            public override int Count => 5;
            public bool IsLike { get; private set; }
            public bool IsNotLike { get; private set; }
            public bool IsNotRlike { get; private set; }
            public bool IsRlike { get; private set; }
            public string Name { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Name = (string)value;
                        break;
                    case 1:
                        IsLike = (bool)value;
                        break;
                    case 2:
                        IsNotLike = (bool)value;
                        break;
                    case 3:
                        IsRlike = (bool)value;
                        break;
                    case 4:
                        IsNotRlike = (bool)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "IsLike" => true,
                "IsNotLike" => true,
                "IsRlike" => true,
                "IsNotRlike" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Name,
                1 => (object)IsLike,
                2 => (object)IsNotLike,
                3 => (object)IsRlike,
                4 => (object)IsNotRlike,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Name" => (object)Name,
                "IsLike" => (object)IsLike,
                "IsNotLike" => (object)IsNotLike,
                "IsRlike" => (object)IsRlike,
                "IsNotRlike" => (object)IsNotRlike,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string Name, bool IsLike, bool IsNotLike, bool IsRlike, bool IsNotRlike)
            {
                this.Name = Name;
                this.IsLike = IsLike;
                this.IsNotLike = IsNotLike;
                this.IsRlike = IsRlike;
                this.IsNotRlike = IsNotRlike;
            }

            public bool IsLike { get; }
            public bool IsNotLike { get; }
            public bool IsNotRlike { get; }
            public bool IsRlike { get; }
            public string Name { get; }
        }
    }
}
