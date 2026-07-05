/*
raw query string

with u as (
                      unpivot #A.entities() s
                      on Metric in (s.NullableValue as NullableValue, null as ExplicitNull)
                      using Value
                      keep s.Name + ':' + s.Country as Label
                  )
                  select Label, Metric, Value
                  from u
                  order by Label, Metric
                  skip 1
                  take 5
*/

/*
logical plan representation string

Cte
  Definition [u]
    MultiStatement
      Project [__unpivot.Label as Label, __unpivot.Metric as Metric, __unpivot.Value as Value]
        Unpivot [name: Metric; value: Value; entries: s.NullableValue as NullableValue, NULL as ExplicitNull; keep: ((s.Name || ':') || s.Country) as Label] as __unpivot
          SchemaScan [#A.entities() as s]
  Query
    MultiStatement
      Take [5]
        Skip [1]
          Sort [u.Label, u.Metric]
            Project [u.Label as Label, u.Metric as Metric, u.Value as Value]
              CteRef [u as u]
*/

/*
physical plan representation string

PhysicalCte
  Definition [u]
    PhysicalMultiStatement
      PhysicalProject [__unpivot.Label as Label, __unpivot.Metric as Metric, __unpivot.Value as Value]
        PhysicalUnpivot [name: Metric; value: Value; entries: s.NullableValue as NullableValue, NULL as ExplicitNull; keep: ((s.Name || ':') || s.Country) as Label] as __unpivot
          PhysicalSchemaScan [#A.entities() as s]
  Query
    PhysicalMultiStatement
      PhysicalTopOffset [skip 1, take 5] [u.Label, u.Metric]
        PhysicalProject [u.Label as Label, u.Metric as Metric, u.Value as Value]
          PhysicalCteRef [u as u]
*/

/*
intermediate representation

ExecutionPlan [compiled]
  Shapes
    SourceEntity [s: BasicEntity]
      Name: string <- property Name
      Country: string <- property Country
      NullableValue: int? <- property NullableValue
    UnknownShape [ValuesRowShape]
      Label: string <- field Label
      Metric: string <- field Metric
      Value: int? <- field Value
    Generated [Cte0Row0]
      Label: string <- field Label
      Metric: string <- field Metric
      Value: int? <- field Value
    TableRow [u]
      Label: string <- field Label
      Metric: string <- field Metric
      Value: int? <- field Value
    Generated [ResultRow0]
      Label: string <- field Label
      Metric: string <- field Metric
      Value: int? <- field Value

  Body
    SourceScan [s: BasicEntity] -> cte0_sRows
    CreateTable [cte0: Cte0Row0]
    ChunkedForEach [s in cte0_sRows]
      ScopedBlock
        CreateGeneratedRow [__unpivot <- __unpivotUnpivotB90BD0CARow0(Label: ((s.Name || ':') || s.Country), Metric: 'NullableValue', Value: s.NullableValue)]
        AppendRow [cte0 <- Cte0Row0(Label: __unpivot.Label, Metric: __unpivot.Metric, Value: __unpivot.Value)]
      ScopedBlock
        CreateGeneratedRow [__unpivot <- __unpivotUnpivotB90BD0CARow0(Label: ((s.Name || ':') || s.Country), Metric: 'ExplicitNull', Value: NULL)]
        AppendRow [cte0 <- Cte0Row0(Label: __unpivot.Label, Metric: __unpivot.Metric, Value: __unpivot.Value)]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [u in _cteRowResults.Slot0]
      AppendShape [result <- ResultShape0(Label: u.Label, Metric: u.Metric, Value: u.Value)]
    TopOffsetShapeRows [result -> resultTopOffset by Label ASC, Metric ASC, skip 1, take 5, BoundedHeap]
    ReturnDeferredTable [resultTopOffset: ResultRow0 <- ResultShape0]
*/

// === SyntaxTree:  ===
namespace GeneratedSample_Q160_UnpivotCteNullableOrdering
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_cte0_1 = new Column[]
        {
            new Column("Label", typeof(string), 0),
            new Column("Metric", typeof(string), 1),
            new Column("Value", typeof(int?), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_s_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 10), new Column("Country", typeof(string), 12), new Column("NullableValue", typeof(int?), 19) });
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
        public Table Run(CancellationToken token)
        {
            return QueryRows.DeferredTable<ResultRow0>("resultTopOffset", __columns_compiled_cte0_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Label, __musoqShapeRow.Metric, __musoqShapeRow.Value);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.Select);
            try
            {
                var _cteRowResults = new CteRowResults();
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, OnDataSourceProgress, _cteRowResults);
                var result = new List<ResultShape0>();
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte0Row0 u = __storedTable0Rows[__storedTable0Index];
                    result.Add(new ResultShape0(u.Label, u.Metric, u.Value));
                }

                var resultTopOffsetRows = EvaluationHelper.SelectTopOffsetRecords(result, 1, 5, Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = StringComparer.Ordinal.Compare(left.Label, right.Label);
                    if (comparison != 0)
                        return comparison;
                    comparison = StringComparer.Ordinal.Compare(left.Metric, right.Metric);
                    if (comparison != 0)
                        return comparison;
                    return 0;
                }));
                foreach (var resultTopOffsetRowsRow in resultTopOffsetRows)
                {
                    __musoqFinalShapeRows.Add(resultTopOffsetRowsRow);
                }

                return __musoqFinalShapeRows;
            }
            finally
            {
                OnPhaseChanged("compiled:cte0", QueryPhase.End);
                OnPhaseChanged("compiled", QueryPhase.End);
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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Cte0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, CteRowResults _cteRowResults)
        {
            var __cte0_sSchema = provider.GetSchema("#A");
            var cte0_sRowsSource = __cte0_sSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("s:1", sourceExecutionPlans["s:1"], token, __schemaColumns_compiled_s_0, sourceRuntimeSettingsBySourceContextId["s:1"], logger, OnDataSourceProgress), Array.Empty<object>());
            var cte0_sRows = cte0_sRowsSource.Chunks;
            var cte0 = new List<Cte0Row0>();
            foreach (var sChunk in cte0_sRows)
            {
                if (sChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> sChunkView)
                {
                    if (sChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] sChunkViewArray)
                    {
                        int sChunkViewOffset = sChunkView.Offset;
                        for (int sIndex = 0, sIndexCount = sChunkView.Count; sIndex < sIndexCount; ++sIndex)
                        {
                            if ((sIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var s = sChunkViewArray[sChunkViewOffset + sIndex];
                            {
                                __unpivotUnpivotB90BD0CARow0 __unpivot = new __unpivotUnpivotB90BD0CARow0(((s.Name + ":") + s.Country), "NullableValue", s.NullableValue);
                                cte0.Add(new Cte0Row0(__unpivot.Label, __unpivot.Metric, __unpivot.Value));
                            }

                            {
                                __unpivotUnpivotB90BD0CARow0 __unpivot = new __unpivotUnpivotB90BD0CARow0(((s.Name + ":") + s.Country), "ExplicitNull", null);
                                cte0.Add(new Cte0Row0(__unpivot.Label, __unpivot.Metric, __unpivot.Value));
                            }
                        }

                        continue;
                    }

                    if (sChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> sChunkViewList)
                    {
                        int sChunkViewOffset = sChunkView.Offset;
                        for (int sIndex = 0, sIndexCount = sChunkView.Count; sIndex < sIndexCount; ++sIndex)
                        {
                            if ((sIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var s = sChunkViewList[sChunkViewOffset + sIndex];
                            {
                                __unpivotUnpivotB90BD0CARow0 __unpivot = new __unpivotUnpivotB90BD0CARow0(((s.Name + ":") + s.Country), "NullableValue", s.NullableValue);
                                cte0.Add(new Cte0Row0(__unpivot.Label, __unpivot.Metric, __unpivot.Value));
                            }

                            {
                                __unpivotUnpivotB90BD0CARow0 __unpivot = new __unpivotUnpivotB90BD0CARow0(((s.Name + ":") + s.Country), "ExplicitNull", null);
                                cte0.Add(new Cte0Row0(__unpivot.Label, __unpivot.Metric, __unpivot.Value));
                            }
                        }

                        continue;
                    }
                }

                for (int sIndex = 0, sIndexCount = sChunk.Count; sIndex < sIndexCount; ++sIndex)
                {
                    if ((sIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    var s = sChunk[sIndex];
                    {
                        __unpivotUnpivotB90BD0CARow0 __unpivot = new __unpivotUnpivotB90BD0CARow0(((s.Name + ":") + s.Country), "NullableValue", s.NullableValue);
                        cte0.Add(new Cte0Row0(__unpivot.Label, __unpivot.Metric, __unpivot.Value));
                    }

                    {
                        __unpivotUnpivotB90BD0CARow0 __unpivot = new __unpivotUnpivotB90BD0CARow0(((s.Name + ":") + s.Country), "ExplicitNull", null);
                        cte0.Add(new Cte0Row0(__unpivot.Label, __unpivot.Metric, __unpivot.Value));
                    }
                }
            }

            return cte0;
        }

        private sealed class Cte0Row0
        {
            public Cte0Row0(string __value0, string __value1, int? __value2)
            {
                Label = __value0;
                Metric = __value1;
                Value = __value2;
            }

            public string Label { get; }
            public string Metric { get; }
            public int? Value { get; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, string __value1, int? __value2)
            {
                Label = __value0;
                Metric = __value1;
                Value = __value2;
            }

            public override int Count => 3;
            public string Label { get; private set; }
            public string Metric { get; private set; }
            public int? Value { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Label = (string)value;
                        break;
                    case 1:
                        Metric = (string)value;
                        break;
                    case 2:
                        Value = (int?)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Label" => true,
                "Metric" => true,
                "Value" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Label,
                1 => (object)Metric,
                2 => (object)Value,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Label" => (object)Label,
                "Metric" => (object)Metric,
                "Value" => (object)Value,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string Label, string Metric, int? Value)
            {
                this.Label = Label;
                this.Metric = Metric;
                this.Value = Value;
            }

            public string Label { get; }
            public string Metric { get; }
            public int? Value { get; }
        }

        private sealed class __unpivotUnpivotB90BD0CARow0 : Row
        {
            public __unpivotUnpivotB90BD0CARow0(string __value0, string __value1, int? __value2)
            {
                Label = __value0;
                Metric = __value1;
                Value = __value2;
            }

            public override int Count => 3;
            public string Label { get; private set; }
            public string Metric { get; private set; }
            public int? Value { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Label = (string)value;
                        break;
                    case 1:
                        Metric = (string)value;
                        break;
                    case 2:
                        Value = (int?)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Label" => true,
                "Metric" => true,
                "Value" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Label,
                1 => (object)Metric,
                2 => (object)Value,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Label" => (object)Label,
                "Metric" => (object)Metric,
                "Value" => (object)Value,
                _ => throw new KeyNotFoundException(name)
            };
        }
    }
}
