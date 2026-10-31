// === Parsed Query ===
/*
with recursive wide (Id, Depth, A, B, C, D, E, F, Name, Flag, Amount, Code) as (select 1, 0, 10, 20, 30, 40, 50, 60, 'row', true, 1::Decimal, 'x' from values {( Seed: 1 )} seed union (Id) select w.Id + 1, w.Depth + 1, w.A, w.B, w.C, w.D, w.E, w.F, w.Name + 'x', w.Flag, (w.Amount + 1)::Decimal, w.Code from wide w where w.Depth < 2) select Id, Depth, A, B, C, D, E, F, Name, Flag, Amount, Code from wide order by Id
*/

// === Logical Plan ===
/*
Cte
  Definition [wide]
    RecursiveCte [wide] [Keyed: Id]
      Anchor
        MultiStatement
          Project [1 as Id, 0 as Depth, 10 as A, 20 as B, 30 as C, 40 as D, 50 as E, 60 as F, 'row' as Name, TRUE as Flag, 1::Decimal as Amount, 'x' as Code]
            ValuesScan [1 rows as seed]
      RecursiveMember
        MultiStatement
          Project [(w.Id + 1) as w.Id + 1, (w.Depth + 1) as w.Depth + 1, w.A as A, w.B as B, w.C as C, w.D as D, w.E as E, w.F as F, (w.Name || 'x') as w.Name + x, w.Flag as Flag, (w.Amount + 1)::Decimal as (w.Amount + 1)::Decimal, w.Code as Code]
            Filter [(w.Depth < 2)]
              CteRef [wide as w]
  Query
    MultiStatement
      Sort [wide.Id]
        Project [wide.Id as Id, wide.Depth as Depth, wide.A as A, wide.B as B, wide.C as C, wide.D as D, wide.E as E, wide.F as F, wide.Name as Name, wide.Flag as Flag, wide.Amount as Amount, wide.Code as Code]
          CteRef [wide as wide]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [wide]
    PhysicalRecursiveCte [wide] [Keyed: Id]
      Anchor
        PhysicalMultiStatement
          PhysicalProject [1 as Id, 0 as Depth, 10 as A, 20 as B, 30 as C, 40 as D, 50 as E, 60 as F, 'row' as Name, TRUE as Flag, 1::Decimal as Amount, 'x' as Code]
            PhysicalValuesScan [1 rows as seed]
      RecursiveMember
        PhysicalMultiStatement
          PhysicalProject [(w.Id + 1) as w.Id + 1, (w.Depth + 1) as w.Depth + 1, w.A as A, w.B as B, w.C as C, w.D as D, w.E as E, w.F as F, (w.Name || 'x') as w.Name + x, w.Flag as Flag, (w.Amount + 1)::Decimal as (w.Amount + 1)::Decimal, w.Code as Code]
            PhysicalFilter [(w.Depth < 2)]
              PhysicalCteRef [wide as w]
  Query
    PhysicalMultiStatement
      PhysicalSort [wide.Id]
        PhysicalProject [wide.Id as Id, wide.Depth as Depth, wide.A as A, wide.B as B, wide.C as C, wide.D as D, wide.E as E, wide.F as F, wide.Name as Name, wide.Flag as Flag, wide.Amount as Amount, wide.Code as Code]
          PhysicalCteRef [wide as wide]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    UnknownShape [ValuesRowShape]
      Seed: int <- field Seed
    Generated [Cte0Row0]
      Id: int <- field Id
      Depth: int <- field Depth
      A: int <- field A
      B: int <- field B
      C: int <- field C
      D: int <- field D
      E: int <- field E
      F: int <- field F
      Name: string <- field Name
      Flag: bool <- field Flag
      Amount: decimal? <- field Amount
      Code: string <- field Code
    TableRow [w]
      Id: int <- field Id
      Depth: int <- field Depth
      A: int <- field A
      B: int <- field B
      C: int <- field C
      D: int <- field D
      E: int <- field E
      F: int <- field F
      Name: string <- field Name
      Flag: bool <- field Flag
      Amount: decimal? <- field Amount
      Code: string <- field Code
    TableRow [wide]
      Id: int <- field Id
      Depth: int <- field Depth
      A: int <- field A
      B: int <- field B
      C: int <- field C
      D: int <- field D
      E: int <- field E
      F: int <- field F
      Name: string <- field Name
      Flag: bool <- field Flag
      Amount: decimal? <- field Amount
      Code: string <- field Code
    Generated [ResultRow0]
      Id: int <- field Id
      Depth: int <- field Depth
      A: int <- field A
      B: int <- field B
      C: int <- field C
      D: int <- field D
      E: int <- field E
      F: int <- field F
      Name: string <- field Name
      Flag: bool <- field Flag
      Amount: decimal? <- field Amount
      Code: string <- field Code

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [From:cte0]
    RecursiveCte [wide; result cte0; frontiers cte0CurrentFrontier, cte0NextFrontier; identity Keyed via cte0Seen (Id); max iterations 1000; max rows 10000000; max snapshot rows 10000000]
      Anchor
        CreateValuesRows [cte0CurrentFrontier_seedRows: seedValuesD6F9BDFERow0 x 1]
        ForEach [seed in cte0CurrentFrontier_seedRows]
          RecursiveAppend [cte0CurrentFrontier <- Cte0Row0(Id: 1, Depth: 0, A: 10, B: 20, C: 30, D: 40, E: 50, F: 60, Name: 'row', Flag: TRUE, Amount: 1::Decimal, Code: 'x'); identity cte0Seen (Id); guard cte0.Count + cte0CurrentFrontier.Count < 10000000]
      RecursiveMember
        ForEach [w in cte0CurrentFrontier]
          If [(w.Depth < 2)]
            RecursiveAppend [cte0NextFrontier <- Cte0Row0(Id: (w.Id + 1), Depth: (w.Depth + 1), A: w.A, B: w.B, C: w.C, D: w.D, E: w.E, F: w.F, Name: (w.Name || 'x'), Flag: w.Flag, Amount: (w.Amount + 1)::Decimal, Code: w.Code); identity cte0Seen (Id); guard cte0.Count + cte0NextFrontier.Count < 10000000]
    PhaseBoundary [Where:cte0]
    PhaseBoundary [Select:cte0]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [End:cte0]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [From]
    ForEach [wide in _cteRowResults.Slot0]
      AppendShape [result <- ResultShape0(Id: wide.Id, Depth: wide.Depth, A: wide.A, B: wide.B, C: wide.C, D: wide.D, E: wide.E, F: wide.F, Name: wide.Name, Flag: wide.Flag, Amount: wide.Amount, Code: wide.Code)]
    PhaseBoundary [Select]
    SortOwnedBuffer [result by Id ASC, stable sequence __musoqSequence]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q218_RecursiveWidePayload
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
        private static readonly Column[] __columns_compiled_result_0 = new Column[]
        {
            new Column("Id", typeof(int), 0),
            new Column("Depth", typeof(int), 1),
            new Column("A", typeof(int), 2),
            new Column("B", typeof(int), 3),
            new Column("C", typeof(int), 4),
            new Column("D", typeof(int), 5),
            new Column("E", typeof(int), 6),
            new Column("F", typeof(int), 7),
            new Column("Name", typeof(string), 8),
            new Column("Flag", typeof(bool), 9),
            new Column("Amount", typeof(decimal?), 10),
            new Column("Code", typeof(string), 11)
        };
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
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_0, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Id, __musoqShapeRow.Depth, __musoqShapeRow.A, __musoqShapeRow.B, __musoqShapeRow.C, __musoqShapeRow.D, __musoqShapeRow.E, __musoqShapeRow.F, __musoqShapeRow.Name, __musoqShapeRow.Flag, __musoqShapeRow.Amount, __musoqShapeRow.Code);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var _cteRowResults = new CteRowResults();
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
                try
                {
                    OnPhaseChanged("compiled:cte0", QueryPhase.From);
                    var cte0 = new List<Cte0Row0>();
                    var cte0CurrentFrontier = new List<Cte0Row0>();
                    var cte0NextFrontier = new List<Cte0Row0>();
                    var cte0Seen = new HashSet<int>();
                    int __cte0Iteration = 0;
                    int __cte0CancellationCounter = 0;
                    seedValuesD6F9BDFERow0[] cte0CurrentFrontier_seedRows = new seedValuesD6F9BDFERow0[]
                    {
                        new seedValuesD6F9BDFERow0(1)
                    };
                    foreach (var seed in cte0CurrentFrontier_seedRows)
                    {
                        token.ThrowIfCancellationRequested();
                        var __cte0CurrentFrontierCandidate0 = 1;
                        var __cte0CurrentFrontierCandidate1 = 0;
                        var __cte0CurrentFrontierCandidate2 = 10;
                        var __cte0CurrentFrontierCandidate3 = 20;
                        var __cte0CurrentFrontierCandidate4 = 30;
                        var __cte0CurrentFrontierCandidate5 = 40;
                        var __cte0CurrentFrontierCandidate6 = 50;
                        var __cte0CurrentFrontierCandidate7 = 60;
                        var __cte0CurrentFrontierCandidate8 = "row";
                        var __cte0CurrentFrontierCandidate9 = true;
                        var __cte0CurrentFrontierCandidate10 = global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToDecimal(1);
                        var __cte0CurrentFrontierCandidate11 = "x";
                        if (cte0Seen.Add(__cte0CurrentFrontierCandidate0))
                        {
                            if (cte0.Count + cte0CurrentFrontier.Count >= 10000000)
                            {
                                throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("wide", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                            }

                            cte0CurrentFrontier.Add(new Cte0Row0(__cte0CurrentFrontierCandidate0, __cte0CurrentFrontierCandidate1, __cte0CurrentFrontierCandidate2, __cte0CurrentFrontierCandidate3, __cte0CurrentFrontierCandidate4, __cte0CurrentFrontierCandidate5, __cte0CurrentFrontierCandidate6, __cte0CurrentFrontierCandidate7, __cte0CurrentFrontierCandidate8, __cte0CurrentFrontierCandidate9, __cte0CurrentFrontierCandidate10, __cte0CurrentFrontierCandidate11));
                        }
                    }

                    cte0.AddRange(cte0CurrentFrontier);
                    while (cte0CurrentFrontier.Count > 0)
                    {
                        if ((__cte0Iteration & 63) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        if (__cte0Iteration >= 1000)
                        {
                            throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("wide", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7007_RecursiveCteIterationLimitExceeded, 1000);
                        }

                        __cte0Iteration++;
                        cte0NextFrontier.Clear();
                        for (int cte0CurrentFrontierIndex = 0; cte0CurrentFrontierIndex < cte0CurrentFrontier.Count; ++cte0CurrentFrontierIndex)
                        {
                            if (cte0CurrentFrontierIndex != 0 && (cte0CurrentFrontierIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            Cte0Row0 w = (Cte0Row0)cte0CurrentFrontier[cte0CurrentFrontierIndex];
                            if ((w.Depth < 2))
                            {
                                ++__cte0CancellationCounter;
                                if ((__cte0CancellationCounter & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var __cte0NextFrontierCandidate0 = (w.Id + 1);
                                var __cte0NextFrontierCandidate1 = (w.Depth + 1);
                                var __cte0NextFrontierCandidate2 = w.A;
                                var __cte0NextFrontierCandidate3 = w.B;
                                var __cte0NextFrontierCandidate4 = w.C;
                                var __cte0NextFrontierCandidate5 = w.D;
                                var __cte0NextFrontierCandidate6 = w.E;
                                var __cte0NextFrontierCandidate7 = w.F;
                                var __cte0NextFrontierCandidate8 = (w.Name + "x");
                                var __cte0NextFrontierCandidate9 = w.Flag;
                                var __cte0NextFrontierCandidate10 = (decimal?)(w.Amount + 1);
                                var __cte0NextFrontierCandidate11 = w.Code;
                                if (cte0Seen.Add(__cte0NextFrontierCandidate0))
                                {
                                    if (cte0.Count + cte0NextFrontier.Count >= 10000000)
                                    {
                                        throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("wide", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                    }

                                    cte0NextFrontier.Add(new Cte0Row0(__cte0NextFrontierCandidate0, __cte0NextFrontierCandidate1, __cte0NextFrontierCandidate2, __cte0NextFrontierCandidate3, __cte0NextFrontierCandidate4, __cte0NextFrontierCandidate5, __cte0NextFrontierCandidate6, __cte0NextFrontierCandidate7, __cte0NextFrontierCandidate8, __cte0NextFrontierCandidate9, __cte0NextFrontierCandidate10, __cte0NextFrontierCandidate11));
                                }
                            }
                        }

                        cte0.AddRange(cte0NextFrontier);
                        var __cte0FrontierSwap = cte0CurrentFrontier;
                        cte0CurrentFrontier = cte0NextFrontier;
                        cte0NextFrontier = __cte0FrontierSwap;
                    }

                    OnPhaseChanged("compiled:cte0", QueryPhase.Where);
                    OnPhaseChanged("compiled:cte0", QueryPhase.Select);
                    _cteRowResults.Slot0 = cte0;
                }
                finally
                {
                    OnPhaseChanged("compiled:cte0", QueryPhase.End);
                }

                var result = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.From);
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte0Row0 wide = __storedTable0Rows[__storedTable0Index];
                    result.Add(new ResultShape0(wide.Id, wide.Depth, wide.A, wide.B, wide.C, wide.D, wide.E, wide.F, wide.Name, wide.Flag, wide.Amount, wide.Code) { __musoqSequence = result.Count });
                }

                OnPhaseChanged("compiled", QueryPhase.Select);
                result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.Id.CompareTo(right.Id);
                    if (comparison != 0)
                        return comparison;
                    return left.__musoqSequence.CompareTo(right.__musoqSequence);
                }));
                return result;
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

        private readonly struct Cte0Row0
        {
            public Cte0Row0(int Id, int Depth, int A, int B, int C, int D, int E, int F, string Name, bool Flag, decimal? Amount, string Code)
            {
                this.Id = Id;
                this.Depth = Depth;
                this.A = A;
                this.B = B;
                this.C = C;
                this.D = D;
                this.E = E;
                this.F = F;
                this.Name = Name;
                this.Flag = Flag;
                this.Amount = Amount;
                this.Code = Code;
            }

            public int Id { get; }
            public int Depth { get; }
            public int A { get; }
            public int B { get; }
            public int C { get; }
            public int D { get; }
            public int E { get; }
            public int F { get; }
            public string Name { get; }
            public bool Flag { get; }
            public decimal? Amount { get; }
            public string Code { get; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0, int __value1, int __value2, int __value3, int __value4, int __value5, int __value6, int __value7, string __value8, bool __value9, decimal? __value10, string __value11)
            {
                Id = __value0;
                Depth = __value1;
                A = __value2;
                B = __value3;
                C = __value4;
                D = __value5;
                E = __value6;
                F = __value7;
                Name = __value8;
                Flag = __value9;
                Amount = __value10;
                Code = __value11;
            }

            public int A { get; private set; }
            public decimal? Amount { get; private set; }
            public int B { get; private set; }
            public int C { get; private set; }
            public string Code { get; private set; }
            public override int Count => 12;
            public int D { get; private set; }
            public int Depth { get; private set; }
            public int E { get; private set; }
            public int F { get; private set; }
            public bool Flag { get; private set; }
            public int Id { get; private set; }
            public string Name { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Depth = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.A = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.B = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    5 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.D = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    6 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.E = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    7 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.F = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    8 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    9 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, bool>(this.Flag = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<bool, T>(value)),
                    10 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Amount = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    11 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Code = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Depth),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.A),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.B),
                4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C),
                5 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.D),
                6 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.E),
                7 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.F),
                8 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name),
                9 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, bool>(this.Flag),
                10 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Amount),
                11 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Code),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Depth" => true,
                "A" => true,
                "B" => true,
                "C" => true,
                "D" => true,
                "E" => true,
                "F" => true,
                "Name" => true,
                "Flag" => true,
                "Amount" => true,
                "Code" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Depth, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.A, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.B, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(4, this.C, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(5, this.D, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(6, this.E, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(7, this.F, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(8, this.Name, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(9, this.Flag, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(10, this.Amount, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(11, this.Code, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Id, int Depth, int A, int B, int C, int D, int E, int F, string Name, bool Flag, decimal? Amount, string Code)
            {
                this.Id = Id;
                this.Depth = Depth;
                this.A = A;
                this.B = B;
                this.C = C;
                this.D = D;
                this.E = E;
                this.F = F;
                this.Name = Name;
                this.Flag = Flag;
                this.Amount = Amount;
                this.Code = Code;
            }

            public int A { get; }
            public decimal? Amount { get; }
            public int B { get; }
            public int C { get; }
            public string Code { get; }
            public int D { get; }
            public int Depth { get; }
            public int E { get; }
            public int F { get; }
            public bool Flag { get; }
            public int Id { get; }
            public string Name { get; }
            public int __musoqSequence { get; init; }
        }

        private sealed class seedValuesD6F9BDFERow0 : Row
        {
            public seedValuesD6F9BDFERow0(int __value0)
            {
                Seed = __value0;
            }

            public override int Count => 1;
            public int Seed { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Seed = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Seed),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Seed" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Seed, ref visitor);
            }
        }
    }
}
