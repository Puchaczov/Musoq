// === Parsed Query ===
/*
param(names: string[]) select Name from #A.entities() where Name in ('Alice', 'Bob', 'Cara', 'Dora', 'Eve', 'Fay', 'Gina', 'Hana', 'Ivy', 'Jill', 'Kara', 'Liam', 'Mona', 'Nora', 'Owen', 'Pia', 'Quin', 'Rita', 'Sara', 'Tara') and Name not in $names and Name contains ('a')
*/

// === Logical Plan ===
/*
MultiStatement
  Project [ko3iko.Name as Name]
    Filter [((ko3iko.Name IN ('Alice', 'Bob', 'Cara', 'Dora', 'Eve', 'Fay', 'Gina', 'Hana', 'Ivy', 'Jill', 'Kara', 'Liam', 'Mona', 'Nora', 'Owen', 'Pia', 'Quin', 'Rita', 'Sara', 'Tara') AND NOT ko3iko.Name IN $names) AND ko3iko.Name IN ('a'))]
      SchemaScan [#A.entities() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [ko3iko.Name as Name]
    PhysicalFilter [((ko3iko.Name IN ('Alice', 'Bob', 'Cara', 'Dora', 'Eve', 'Fay', 'Gina', 'Hana', 'Ivy', 'Jill', 'Kara', 'Liam', 'Mona', 'Nora', 'Owen', 'Pia', 'Quin', 'Rita', 'Sara', 'Tara') AND NOT ko3iko.Name IN $names) AND ko3iko.Name IN ('a'))]
      PhysicalSchemaScan [#A.entities() as ko3iko] [pushdown: ko3iko.Name IN ('Alice', 'Bob', 'Cara', 'Dora', 'Eve', 'Fay', 'Gina', 'Hana', 'Ivy', 'Jill', 'Kara', 'Liam', 'Mona', 'Nora', 'Owen', 'Pia', 'Quin', 'Rita', 'Sara', 'Tara'), NOT ko3iko.Name IN $names, ko3iko.Name IN ('a')]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: BasicEntity]
      Name: string <- property Name
    Generated [ResultRow0]
      Name: string <- field Name

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: BasicEntity] -> ko3ikoRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [Where]
    PhaseBoundary [Select]
    ChunkedForEach [ko3iko in ko3ikoRows]
      Let [name: string = ko3iko.Name]
      If [((name IN ('Alice', 'Bob', 'Cara', 'Dora', 'Eve', 'Fay', 'Gina', 'Hana', 'Ivy', 'Jill', 'Kara', 'Liam', 'Mona', 'Nora', 'Owen', 'Pia', 'Quin', 'Rita', 'Sara', 'Tara') AND NOT name IN $names) AND name IN ('a'))]
        AppendShape [result <- ResultRow0(Name: name)]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q270_SpecCoreMembershipPredicates
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
            new Column("Name", typeof(string), 0)
        };
        private static readonly string[] __inSet_compiled_0 = new string[]
        {
            "a"
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 0) });
        public ILogger Logger { get; set; }
        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = new ScriptParameterContract[]
        {
            new ScriptParameterContract("names", "string[]", "string[]", typeof(string[]), false, true, typeof(string), "string", false, ScriptParameterDefaultKind.None, null)
        };
        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = new ScriptParameterDefinition[]
        {
            new ScriptParameterDefinition(new ScriptParameterContract("names", "string[]", "string[]", typeof(string[]), false, true, typeof(string), "string", false, ScriptParameterDefaultKind.None, null))
        };
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
                var paramNames = ScriptParameterBinder.GetRequiredCollection<string>(__musoqExecutionState, "names");
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, new string[] { "names" });
                ScriptParameterBinder.ValidateMembershipCollection(paramNames, "names");
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
                                if (((Musoq.Evaluator.Operators.SqlIn(name, membershipValue => (membershipValue switch
                                {
                                    "Alice" or "Bob" or "Cara" or "Dora" or "Eve" or "Fay" or "Gina" or "Hana" or "Ivy" or "Jill" or "Kara" or "Liam" or "Mona" or "Nora" or "Owen" or "Pia" or "Quin" or "Rita" or "Sara" or "Tara" => true,
                                    _ => false

                                }), false) & EvaluationHelper.CollectionParameterNotContains<string>(name, paramNames)) & (Array.IndexOf(__inSet_compiled_0, name) >= 0)) == true)
                                {
                                    yield return new ResultRow0(name);
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
                                if (((Musoq.Evaluator.Operators.SqlIn(name, membershipValue => (membershipValue switch
                                {
                                    "Alice" or "Bob" or "Cara" or "Dora" or "Eve" or "Fay" or "Gina" or "Hana" or "Ivy" or "Jill" or "Kara" or "Liam" or "Mona" or "Nora" or "Owen" or "Pia" or "Quin" or "Rita" or "Sara" or "Tara" => true,
                                    _ => false

                                }), false) & EvaluationHelper.CollectionParameterNotContains<string>(name, paramNames)) & (Array.IndexOf(__inSet_compiled_0, name) >= 0)) == true)
                                {
                                    yield return new ResultRow0(name);
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
                        if (((Musoq.Evaluator.Operators.SqlIn(name, membershipValue => (membershipValue switch
                        {
                            "Alice" or "Bob" or "Cara" or "Dora" or "Eve" or "Fay" or "Gina" or "Hana" or "Ivy" or "Jill" or "Kara" or "Liam" or "Mona" or "Nora" or "Owen" or "Pia" or "Quin" or "Rita" or "Sara" or "Tara" => true,
                            _ => false

                        }), false) & EvaluationHelper.CollectionParameterNotContains<string>(name, paramNames)) & (Array.IndexOf(__inSet_compiled_0, name) >= 0)) == true)
                        {
                            yield return new ResultRow0(name);
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
            public ResultRow0(string __value0)
            {
                Name = __value0;
            }

            public override int Count => 1;
            public string Name { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Name, ref visitor);
            }
        }
    }
}
