/*
raw query string

param(ids: int[])
              select Name, Id
              from #A.entities()
              where Id in $ids
              order by Name
*/

/*
logical plan representation string

MultiStatement
  Sort [ko3iko.Name]
    Project [ko3iko.Name as Name, ko3iko.Id as Id]
      Filter [ko3iko.Id IN $ids]
        SchemaScan [#A.entities() as ko3iko]
*/

/*
physical plan representation string

PhysicalMultiStatement
  PhysicalSort [ko3iko.Name]
    PhysicalProject [ko3iko.Name as Name, ko3iko.Id as Id]
      PhysicalFilter [ko3iko.Id IN $ids]
        PhysicalSchemaScan [#A.entities() as ko3iko] [pushdown: ko3iko.Id IN $ids]
*/

/*
intermediate representation

ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: BasicEntity]
      Name: string <- property Name
      Id: int <- property Id
    GeneratedRecord [ResultRow0WithSortKeys]
      Name: string <- field Name
      Id: int <- field Id
      __ordinal: int <- field __ordinal
    Generated [ResultRow0]
      Name: string <- field Name
      Id: int <- field Id

  Body
    SourceScan [ko3iko: BasicEntity] -> ko3ikoRows
    CreateRecordList [resultOrderRecords: ResultRow0WithSortKeys]
    ChunkedForEach [ko3iko in ko3ikoRows]
      Let [id: int = ko3iko.Id]
      If [id IN $ids]
        AppendRecord [resultOrderRecords <- ResultRow0WithSortKeys(Name: ko3iko.Name, Id: id)]
    OrderRecordList [resultOrderRecords: ResultRow0WithSortKeys by Name ASC]
    MaterializeRecordListToShapeRows [resultOrderRecords -> result: ResultShape0 fields 0, 1]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === SyntaxTree:  ===
namespace GeneratedSample_Q172_CollectionParameterInMembership
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
            new Column("Name", typeof(string), 0),
            new Column("Id", typeof(int), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 10), new Column("Id", typeof(int), 18) });
        public ILogger Logger { get; set; }
        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = new ScriptParameterContract[]
        {
            new ScriptParameterContract("ids", "int[]", "int[]", typeof(int[]), false, true, typeof(int), "int", false, ScriptParameterDefaultKind.None, null)
        };
        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = new ScriptParameterDefinition[]
        {
            new ScriptParameterDefinition(new ScriptParameterContract("ids", "int[]", "int[]", typeof(int[]), false, true, typeof(int), "int", false, ScriptParameterDefaultKind.None, null))
        };
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
                yield return new ResultRow0(__musoqShapeRow.Name, __musoqShapeRow.Id);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            OnPhaseChanged("compiled", QueryPhase.Where);
            OnPhaseChanged("compiled", QueryPhase.Select);
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                var paramIds = ScriptParameterBinder.GetRequiredCollection<int>(__musoqExecutionState.Parameters, "ids");
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, new string[] { "ids" });
                var __musoqFinalShapeRows = new List<ResultShape0>();
                var __ko3ikoSchema = provider.GetSchema("#A");
                var ko3ikoRowsSource = __ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var ko3ikoRows = ko3ikoRowsSource.Chunks;
                var resultOrderRecords = new List<ResultRow0WithSortKeys>();
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
                                int id = ko3iko.Id;
                                if (CollectionParameterContains<int>(id, paramIds))
                                {
                                    resultOrderRecords.Add(new ResultRow0WithSortKeys(ko3iko.Name, id, resultOrderRecords.Count));
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
                                int id = ko3iko.Id;
                                if (CollectionParameterContains<int>(id, paramIds))
                                {
                                    resultOrderRecords.Add(new ResultRow0WithSortKeys(ko3iko.Name, id, resultOrderRecords.Count));
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
                        int id = ko3iko.Id;
                        if (CollectionParameterContains<int>(id, paramIds))
                        {
                            resultOrderRecords.Add(new ResultRow0WithSortKeys(ko3iko.Name, id, resultOrderRecords.Count));
                        }
                    }
                }

                resultOrderRecords.Sort(ResultRow0WithSortKeysComparer.Instance);
                foreach (var resultRecord in resultOrderRecords)
                {
                    __musoqFinalShapeRows.Add(new ResultShape0(resultRecord.Name, resultRecord.Id));
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

        private static bool CollectionParameterContains<T>(T value, IReadOnlyList<T> values)
        {
            var comparer = EqualityComparer<T>.Default;
            for (var index = 0; index < values.Count; index++)
            {
                if (comparer.Equals(value, values[index]))
                    return true;
            }

            return false;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, int __value1)
            {
                Name = __value0;
                Id = __value1;
            }

            public override int Count => 2;
            public int Id { get; private set; }
            public string Name { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Name = (string)value;
                        break;
                    case 1:
                        Id = (int)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "Id" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Name,
                1 => (object)Id,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Name" => (object)Name,
                "Id" => (object)Id,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultRow0WithSortKeys
        {
            public ResultRow0WithSortKeys(string Name, int Id, int __ordinal)
            {
                this.Name = Name;
                this.Id = Id;
                this.__ordinal = __ordinal;
            }

            public int Id { get; }
            public string Name { get; }
            public int __ordinal { get; }
        }

        private sealed class ResultRow0WithSortKeysComparer : IComparer<ResultRow0WithSortKeys>
        {
            public static readonly ResultRow0WithSortKeysComparer Instance = new ResultRow0WithSortKeysComparer();
            public int Compare(ResultRow0WithSortKeys left, ResultRow0WithSortKeys right)
            {
                var comparison = StringComparer.Ordinal.Compare(left.Name, right.Name);
                if (comparison != 0)
                    return comparison;
                return left.__ordinal.CompareTo(right.__ordinal);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string Name, int Id)
            {
                this.Name = Name;
                this.Id = Id;
            }

            public int Id { get; }
            public string Name { get; }
        }
    }
}
