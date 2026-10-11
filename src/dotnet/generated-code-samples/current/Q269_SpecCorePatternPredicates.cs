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
      PhysicalSchemaScan [#A.entities() as ko3iko] [pushdown: (ko3iko.Name LIKE '%a%' OR ko3iko.City LIKE '%a%'), NOT ko3iko.Name LIKE '%z%', NOT ko3iko.City LIKE '%z%']
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
    Let [__rlikeMatcher0: PreparedRLikeMatcher = PREPARE_RLIKE('^[A-Z]', strategy=regex, anchors=none, literal-span=none, fallback=RegexMetacharacter)]
    Let [__rlikeMatcher1: PreparedRLikeMatcher = PREPARE_RLIKE('^$', strategy=regex, anchors=none, literal-span=none, fallback=RegexMetacharacter)]
    PhaseBoundary [From]
    SourceScan [ko3iko: BasicEntity] -> ko3ikoRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [Where]
    PhaseBoundary [Select]
    ChunkedForEach [ko3iko in ko3ikoRows]
      Let [name: string = ko3iko.Name]
      Let [city: string = ko3iko.City]
      If [((STRING_MATCH(name, pattern='%a%', needle='a', kind=Contains, comparison=LikeIgnoreCase) OR STRING_MATCH(city, pattern='%a%', needle='a', kind=Contains, comparison=LikeIgnoreCase)) AND (NOT STRING_MATCH(name, pattern='%z%', needle='z', kind=Contains, comparison=LikeIgnoreCase) AND NOT STRING_MATCH(city, pattern='%z%', needle='z', kind=Contains, comparison=LikeIgnoreCase)))]
        AppendShape [result <- ResultRow0(Name: name, IsLike: STRING_MATCH(name, pattern='A%', needle='A', kind=Prefix, comparison=LikeIgnoreCase), IsNotLike: NOT STRING_MATCH(name, pattern='Z%', needle='Z', kind=Prefix, comparison=LikeIgnoreCase), IsRlike: PREPARED_RLIKE(name, __rlikeMatcher0), IsNotRlike: NOT PREPARED_RLIKE(name, __rlikeMatcher1))]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Name", typeof(string), 0),
            new Column("IsLike", typeof(bool), 1),
            new Column("IsNotLike", typeof(bool), 2),
            new Column("IsRlike", typeof(bool), 3),
            new Column("IsNotRlike", typeof(bool), 4)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 0), new Column("City", typeof(string), 1) });
        public ILogger Logger { get; set; }
        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = Array.Empty<ScriptParameterContract>();
        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = Array.Empty<ScriptParameterDefinition>();
        public ISchemaProvider Provider { get; set; }
        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; }
        public TypedParameterSnapshot TypedParameters { get; set; } = TypedParameterSnapshot.Empty;

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
                yield return __musoqShapeRow;
            }
        }

        private IEnumerable<ResultRow0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                Musoq.Evaluator.PreparedRLikeMatcher __rlikeMatcher0 = Operators.PrepareRLike("^[A-Z]", TimeSpan.FromTicks(20000000L));
                Musoq.Evaluator.PreparedRLikeMatcher __rlikeMatcher1 = Operators.PrepareRLike("^$", TimeSpan.FromTicks(20000000L));
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __ko3ikoSchema = provider.GetSchema("#A");
                var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__ko3ikoSchema, "entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
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
                                if ((((name is string __musoqStringMatch1 ? (__musoqStringMatch1.Length >= 1) && __musoqStringMatch1.Contains("a", StringComparison.OrdinalIgnoreCase) : false) || (city is string __musoqStringMatch2 ? (__musoqStringMatch2.Length >= 1) && __musoqStringMatch2.Contains("a", StringComparison.OrdinalIgnoreCase) : false)) && ((!(name is string __musoqStringMatch3 ? (__musoqStringMatch3.Length >= 1) && __musoqStringMatch3.Contains("z", StringComparison.OrdinalIgnoreCase) : false)) && (!(city is string __musoqStringMatch4 ? (__musoqStringMatch4.Length >= 1) && __musoqStringMatch4.Contains("z", StringComparison.OrdinalIgnoreCase) : false)))))
                                {
                                    yield return new ResultRow0(name, (name is string __musoqStringMatch5 ? (__musoqStringMatch5.Length >= 1) && ((__musoqStringMatch5[0] == 'A') || (((__musoqStringMatch5[0] | 32) == 97) && (((__musoqStringMatch5[0] | 32) - 97) <= 25))) : false), (!(name is string __musoqStringMatch6 ? (__musoqStringMatch6.Length >= 1) && ((__musoqStringMatch6[0] == 'Z') || (((__musoqStringMatch6[0] | 32) == 122) && (((__musoqStringMatch6[0] | 32) - 97) <= 25))) : false)), Operators.RLikePrepared(name, __rlikeMatcher0), (!Operators.RLikePrepared(name, __rlikeMatcher1)));
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
                                if ((((name is string __musoqStringMatch7 ? (__musoqStringMatch7.Length >= 1) && __musoqStringMatch7.Contains("a", StringComparison.OrdinalIgnoreCase) : false) || (city is string __musoqStringMatch8 ? (__musoqStringMatch8.Length >= 1) && __musoqStringMatch8.Contains("a", StringComparison.OrdinalIgnoreCase) : false)) && ((!(name is string __musoqStringMatch9 ? (__musoqStringMatch9.Length >= 1) && __musoqStringMatch9.Contains("z", StringComparison.OrdinalIgnoreCase) : false)) && (!(city is string __musoqStringMatch10 ? (__musoqStringMatch10.Length >= 1) && __musoqStringMatch10.Contains("z", StringComparison.OrdinalIgnoreCase) : false)))))
                                {
                                    yield return new ResultRow0(name, (name is string __musoqStringMatch11 ? (__musoqStringMatch11.Length >= 1) && ((__musoqStringMatch11[0] == 'A') || (((__musoqStringMatch11[0] | 32) == 97) && (((__musoqStringMatch11[0] | 32) - 97) <= 25))) : false), (!(name is string __musoqStringMatch12 ? (__musoqStringMatch12.Length >= 1) && ((__musoqStringMatch12[0] == 'Z') || (((__musoqStringMatch12[0] | 32) == 122) && (((__musoqStringMatch12[0] | 32) - 97) <= 25))) : false)), Operators.RLikePrepared(name, __rlikeMatcher0), (!Operators.RLikePrepared(name, __rlikeMatcher1)));
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
                        if ((((name is string __musoqStringMatch13 ? (__musoqStringMatch13.Length >= 1) && __musoqStringMatch13.Contains("a", StringComparison.OrdinalIgnoreCase) : false) || (city is string __musoqStringMatch14 ? (__musoqStringMatch14.Length >= 1) && __musoqStringMatch14.Contains("a", StringComparison.OrdinalIgnoreCase) : false)) && ((!(name is string __musoqStringMatch15 ? (__musoqStringMatch15.Length >= 1) && __musoqStringMatch15.Contains("z", StringComparison.OrdinalIgnoreCase) : false)) && (!(city is string __musoqStringMatch16 ? (__musoqStringMatch16.Length >= 1) && __musoqStringMatch16.Contains("z", StringComparison.OrdinalIgnoreCase) : false)))))
                        {
                            yield return new ResultRow0(name, (name is string __musoqStringMatch17 ? (__musoqStringMatch17.Length >= 1) && ((__musoqStringMatch17[0] == 'A') || (((__musoqStringMatch17[0] | 32) == 97) && (((__musoqStringMatch17[0] | 32) - 97) <= 25))) : false), (!(name is string __musoqStringMatch18 ? (__musoqStringMatch18.Length >= 1) && ((__musoqStringMatch18[0] == 'Z') || (((__musoqStringMatch18[0] | 32) == 122) && (((__musoqStringMatch18[0] | 32) - 97) <= 25))) : false)), Operators.RLikePrepared(name, __rlikeMatcher0), (!Operators.RLikePrepared(name, __rlikeMatcher1)));
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

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, bool>(this.IsLike = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<bool, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, bool>(this.IsNotLike = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<bool, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, bool>(this.IsRlike = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<bool, T>(value)),
                    4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, bool>(this.IsNotRlike = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<bool, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, bool>(this.IsLike),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, bool>(this.IsNotLike),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, bool>(this.IsRlike),
                4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, bool>(this.IsNotRlike),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "IsLike" => true,
                "IsNotLike" => true,
                "IsRlike" => true,
                "IsNotRlike" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Name, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.IsLike, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.IsNotLike, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.IsRlike, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(4, this.IsNotRlike, ref visitor);
            }
        }
    }
}
