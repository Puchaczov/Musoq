// === Parsed Query ===
/*
unpivot #A.entities() s
                  on Metric in (s.Population as Population, s.Money as Money)
                  using Amount
                  keep s.Country as Country
*/

// === Logical Plan ===
/*
MultiStatement
  Project [__unpivot.Country as Country, __unpivot.Metric as Metric, __unpivot.Amount as Amount]
    Unpivot [name: Metric; value: Amount; entries: s.Population as Population, s.Money as Money; keep: s.Country as Country] as __unpivot
      SchemaScan [#A.entities() as s]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [__unpivot.Country as Country, __unpivot.Metric as Metric, __unpivot.Amount as Amount]
    PhysicalUnpivot [name: Metric; value: Amount; entries: s.Population as Population, s.Money as Money; keep: s.Country as Country] as __unpivot
      PhysicalSchemaScan [#A.entities() as s]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [s: BasicEntity]
      Country: string <- property Country
      Population: decimal <- property Population
      Money: decimal <- property Money
    UnknownShape [ValuesRowShape]
      Country: string <- field Country
      Metric: string <- field Metric
      Amount: decimal <- field Amount
    Generated [ResultRow0]
      Country: string <- field Country
      Metric: string <- field Metric
      Amount: decimal <- field Amount

  Body
    SourceScan [s: BasicEntity] -> sRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ChunkedForEach [s in sRows]
      ScopedBlock
        CreateGeneratedRow [__unpivot <- __unpivotUnpivot529449A3Row0(Country: s.Country, Metric: 'Population', Amount: s.Population)]
        AppendShape [result <- ResultShape0(Country: __unpivot.Country, Metric: __unpivot.Metric, Amount: __unpivot.Amount)]
      ScopedBlock
        CreateGeneratedRow [__unpivot <- __unpivotUnpivot529449A3Row0(Country: s.Country, Metric: 'Money', Amount: s.Money)]
        AppendShape [result <- ResultShape0(Country: __unpivot.Country, Metric: __unpivot.Metric, Amount: __unpivot.Amount)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q159_UnpivotBasicStreaming
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
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Country", typeof(string), 0),
            new Column("Metric", typeof(string), 1),
            new Column("Amount", typeof(decimal), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_s_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Country", typeof(string), 12), new Column("Population", typeof(decimal), 13), new Column("Money", typeof(decimal), 15) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Country, __musoqShapeRow.Metric, __musoqShapeRow.Amount);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            OnPhaseChanged("compiled", QueryPhase.Select);
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                var __sSchema = provider.GetSchema("#A");
                var sRowsSource = __sSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("s:1", sourceExecutionPlans["s:1"], token, __schemaColumns_compiled_s_0, sourceRuntimeSettingsBySourceContextId["s:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var sRows = sRowsSource.Chunks;
                foreach (var sChunk in sRows)
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
                                    __unpivotUnpivot529449A3Row0 __unpivot = new __unpivotUnpivot529449A3Row0(s.Country, "Population", s.Population);
                                    __musoqFinalShapeRows.Add(new ResultShape0(__unpivot.Country, __unpivot.Metric, __unpivot.Amount));
                                }

                                {
                                    __unpivotUnpivot529449A3Row0 __unpivot = new __unpivotUnpivot529449A3Row0(s.Country, "Money", s.Money);
                                    __musoqFinalShapeRows.Add(new ResultShape0(__unpivot.Country, __unpivot.Metric, __unpivot.Amount));
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
                                    __unpivotUnpivot529449A3Row0 __unpivot = new __unpivotUnpivot529449A3Row0(s.Country, "Population", s.Population);
                                    __musoqFinalShapeRows.Add(new ResultShape0(__unpivot.Country, __unpivot.Metric, __unpivot.Amount));
                                }

                                {
                                    __unpivotUnpivot529449A3Row0 __unpivot = new __unpivotUnpivot529449A3Row0(s.Country, "Money", s.Money);
                                    __musoqFinalShapeRows.Add(new ResultShape0(__unpivot.Country, __unpivot.Metric, __unpivot.Amount));
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
                            __unpivotUnpivot529449A3Row0 __unpivot = new __unpivotUnpivot529449A3Row0(s.Country, "Population", s.Population);
                            __musoqFinalShapeRows.Add(new ResultShape0(__unpivot.Country, __unpivot.Metric, __unpivot.Amount));
                        }

                        {
                            __unpivotUnpivot529449A3Row0 __unpivot = new __unpivotUnpivot529449A3Row0(s.Country, "Money", s.Money);
                            __musoqFinalShapeRows.Add(new ResultShape0(__unpivot.Country, __unpivot.Metric, __unpivot.Amount));
                        }
                    }
                }

                return __musoqFinalShapeRows;
            }
            finally
            {
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

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, string __value1, decimal __value2)
            {
                Country = __value0;
                Metric = __value1;
                Amount = __value2;
            }

            public decimal Amount { get; private set; }
            public override int Count => 3;
            public string Country { get; private set; }
            public string Metric { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Country = (string)value;
                        break;
                    case 1:
                        Metric = (string)value;
                        break;
                    case 2:
                        Amount = (decimal)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Country" => true,
                "Metric" => true,
                "Amount" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Country,
                1 => (object)Metric,
                2 => (object)Amount,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Country" => (object)Country,
                "Metric" => (object)Metric,
                "Amount" => (object)Amount,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string Country, string Metric, decimal Amount)
            {
                this.Country = Country;
                this.Metric = Metric;
                this.Amount = Amount;
            }

            public decimal Amount { get; }
            public string Country { get; }
            public string Metric { get; }
        }

        private sealed class __unpivotUnpivot529449A3Row0 : Row
        {
            public __unpivotUnpivot529449A3Row0(string __value0, string __value1, decimal __value2)
            {
                Country = __value0;
                Metric = __value1;
                Amount = __value2;
            }

            public decimal Amount { get; private set; }
            public override int Count => 3;
            public string Country { get; private set; }
            public string Metric { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Country = (string)value;
                        break;
                    case 1:
                        Metric = (string)value;
                        break;
                    case 2:
                        Amount = (decimal)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Country" => true,
                "Metric" => true,
                "Amount" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Country,
                1 => (object)Metric,
                2 => (object)Amount,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Country" => (object)Country,
                "Metric" => (object)Metric,
                "Amount" => (object)Amount,
                _ => throw new KeyNotFoundException(name)
            };
        }
    }
}
