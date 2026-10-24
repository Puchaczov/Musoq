// === Parsed Query ===
/*
SELECT Id as C01,
                     Name as C02,
                     FirstName as C03,
                     LastName as C04,
                     Email as C05,
                     Value as C06,
                     Category as C07,
                     Department as C08,
                     Salary as C09,
                     Value + 1 as C10,
                     Value + 2 as C11,
                     Value + 3 as C12,
                     Value + 4 as C13,
                     Value + 5 as C14,
                     Value + 6 as C15,
                     Value + 7 as C16,
                     Value + 8 as C17,
                     Value + 9 as C18,
                     Value + 10 as C19,
                     Value + 11 as C20,
                     Salary + 1 as C21,
                     Salary + 2 as C22,
                     Salary + 3 as C23,
                     Salary + 4 as C24,
                     Salary + 5 as C25,
                     Salary + 6 as C26,
                     Salary + 7 as C27,
                     Salary + 8 as C28,
                     Salary + 9 as C29,
                     Salary + 10 as C30,
                     Name + '-' + Category as C31,
                     FirstName + ' ' + LastName as C32,
                     Department + ':' + Category as C33,
                     Email + ':' + Name as C34,
                     Value * 2 as C35,
                     Value * 3 as C36,
                     Value * 4 as C37,
                     Salary * 2 as C38,
                     Salary * 3 as C39,
                     Salary * 4 as C40,
                     Value - Salary as C41,
                     Salary - Value as C42,
                     Value + Salary as C43,
                     Value % 10 as C44,
                     Salary % 10 as C45,
                     Value > 100 as C46,
                     Salary > 1000 as C47,
                     (Category = 'A' or Category = 'B' or Category = 'C') as C48,
                     CASE WHEN Value > 100 THEN 'High' ELSE 'Low' END as C49,
                     CASE WHEN Salary > 1000 THEN 'Large' ELSE 'Small' END as C50
              FROM #test.entities()
*/

// === Logical Plan ===
/*
MultiStatement
  Project [ko3iko.Id as C01, ko3iko.Name as C02, ko3iko.FirstName as C03, ko3iko.LastName as C04, ko3iko.Email as C05, ko3iko.Value as C06, ko3iko.Category as C07, ko3iko.Department as C08, ko3iko.Salary as C09, (ko3iko.Value + 1) as C10, (ko3iko.Value + 2) as C11, (ko3iko.Value + 3) as C12, (ko3iko.Value + 4) as C13, (ko3iko.Value + 5) as C14, (ko3iko.Value + 6) as C15, (ko3iko.Value + 7) as C16, (ko3iko.Value + 8) as C17, (ko3iko.Value + 9) as C18, (ko3iko.Value + 10) as C19, (ko3iko.Value + 11) as C20, (ko3iko.Salary + 1) as C21, (ko3iko.Salary + 2) as C22, (ko3iko.Salary + 3) as C23, (ko3iko.Salary + 4) as C24, (ko3iko.Salary + 5) as C25, (ko3iko.Salary + 6) as C26, (ko3iko.Salary + 7) as C27, (ko3iko.Salary + 8) as C28, (ko3iko.Salary + 9) as C29, (ko3iko.Salary + 10) as C30, ((ko3iko.Name || '-') || ko3iko.Category) as C31, ((ko3iko.FirstName || ' ') || ko3iko.LastName) as C32, ((ko3iko.Department || ':') || ko3iko.Category) as C33, ((ko3iko.Email || ':') || ko3iko.Name) as C34, (ko3iko.Value * 2) as C35, (ko3iko.Value * 3) as C36, (ko3iko.Value * 4) as C37, (ko3iko.Salary * 2) as C38, (ko3iko.Salary * 3) as C39, (ko3iko.Salary * 4) as C40, (ko3iko.Value - ko3iko.Salary) as C41, (ko3iko.Salary - ko3iko.Value) as C42, (ko3iko.Value + ko3iko.Salary) as C43, (ko3iko.Value % 10) as C44, (ko3iko.Salary % 10) as C45, (ko3iko.Value > 100) as C46, (ko3iko.Salary > 1000) as C47, (((ko3iko.Category = 'A') OR (ko3iko.Category = 'B')) OR (ko3iko.Category = 'C')) as C48, CASE WHEN (ko3iko.Value > 100) THEN 'High' ELSE 'Low' END as C49, CASE WHEN (ko3iko.Salary > 1000) THEN 'Large' ELSE 'Small' END as C50]
    SchemaScan [#test.entities() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [ko3iko.Id as C01, ko3iko.Name as C02, ko3iko.FirstName as C03, ko3iko.LastName as C04, ko3iko.Email as C05, ko3iko.Value as C06, ko3iko.Category as C07, ko3iko.Department as C08, ko3iko.Salary as C09, (ko3iko.Value + 1) as C10, (ko3iko.Value + 2) as C11, (ko3iko.Value + 3) as C12, (ko3iko.Value + 4) as C13, (ko3iko.Value + 5) as C14, (ko3iko.Value + 6) as C15, (ko3iko.Value + 7) as C16, (ko3iko.Value + 8) as C17, (ko3iko.Value + 9) as C18, (ko3iko.Value + 10) as C19, (ko3iko.Value + 11) as C20, (ko3iko.Salary + 1) as C21, (ko3iko.Salary + 2) as C22, (ko3iko.Salary + 3) as C23, (ko3iko.Salary + 4) as C24, (ko3iko.Salary + 5) as C25, (ko3iko.Salary + 6) as C26, (ko3iko.Salary + 7) as C27, (ko3iko.Salary + 8) as C28, (ko3iko.Salary + 9) as C29, (ko3iko.Salary + 10) as C30, ((ko3iko.Name || '-') || ko3iko.Category) as C31, ((ko3iko.FirstName || ' ') || ko3iko.LastName) as C32, ((ko3iko.Department || ':') || ko3iko.Category) as C33, ((ko3iko.Email || ':') || ko3iko.Name) as C34, (ko3iko.Value * 2) as C35, (ko3iko.Value * 3) as C36, (ko3iko.Value * 4) as C37, (ko3iko.Salary * 2) as C38, (ko3iko.Salary * 3) as C39, (ko3iko.Salary * 4) as C40, (ko3iko.Value - ko3iko.Salary) as C41, (ko3iko.Salary - ko3iko.Value) as C42, (ko3iko.Value + ko3iko.Salary) as C43, (ko3iko.Value % 10) as C44, (ko3iko.Salary % 10) as C45, (ko3iko.Value > 100) as C46, (ko3iko.Salary > 1000) as C47, (((ko3iko.Category = 'A') OR (ko3iko.Category = 'B')) OR (ko3iko.Category = 'C')) as C48, CASE WHEN (ko3iko.Value > 100) THEN 'High' ELSE 'Low' END as C49, CASE WHEN (ko3iko.Salary > 1000) THEN 'Large' ELSE 'Small' END as C50]
    PhysicalSchemaScan [#test.entities() as ko3iko]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: RuntimeV2RegressionEntity]
      Id: int <- property Id
      Name: string <- property Name
      FirstName: string <- property FirstName
      LastName: string <- property LastName
      Email: string <- property Email
      Value: int <- property Value
      Category: string <- property Category
      Department: string <- property Department
      Salary: int <- property Salary
    Generated [ResultRow0]
      C01: int <- field C01
      C02: string <- field C02
      C03: string <- field C03
      C04: string <- field C04
      C05: string <- field C05
      C06: int <- field C06
      C07: string <- field C07
      C08: string <- field C08
      C09: int <- field C09
      C10: int <- field C10
      C11: int <- field C11
      C12: int <- field C12
      C13: int <- field C13
      C14: int <- field C14
      C15: int <- field C15
      C16: int <- field C16
      C17: int <- field C17
      C18: int <- field C18
      C19: int <- field C19
      C20: int <- field C20
      C21: int <- field C21
      C22: int <- field C22
      C23: int <- field C23
      C24: int <- field C24
      C25: int <- field C25
      C26: int <- field C26
      C27: int <- field C27
      C28: int <- field C28
      C29: int <- field C29
      C30: int <- field C30
      C31: string <- field C31
      C32: string <- field C32
      C33: string <- field C33
      C34: string <- field C34
      C35: int <- field C35
      C36: int <- field C36
      C37: int <- field C37
      C38: int <- field C38
      C39: int <- field C39
      C40: int <- field C40
      C41: int <- field C41
      C42: int <- field C42
      C43: int <- field C43
      C44: int <- field C44
      C45: int <- field C45
      C46: bool <- field C46
      C47: bool <- field C47
      C48: bool? <- field C48
      C49: string <- field C49
      C50: string <- field C50

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: RuntimeV2RegressionEntity] -> ko3ikoRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [Select]
    ChunkedForEach [ko3iko in ko3ikoRows]
      Let [name: string = ko3iko.Name]
      Let [firstName: string = ko3iko.FirstName]
      Let [lastName: string = ko3iko.LastName]
      Let [email: string = ko3iko.Email]
      Let [value: int = ko3iko.Value]
      Let [category: string = ko3iko.Category]
      Let [department: string = ko3iko.Department]
      Let [salary: int = ko3iko.Salary]
      Let [__expr: bool = (value > 100)]
      Let [__expr1: bool = (salary > 1000)]
      AppendShape [result <- ResultRow0(C01: ko3iko.Id, C02: name, C03: firstName, C04: lastName, C05: email, C06: value, C07: category, C08: department, C09: salary, C10: (value + 1), C11: (value + 2), C12: (value + 3), C13: (value + 4), C14: (value + 5), C15: (value + 6), C16: (value + 7), C17: (value + 8), C18: (value + 9), C19: (value + 10), C20: (value + 11), C21: (salary + 1), C22: (salary + 2), C23: (salary + 3), C24: (salary + 4), C25: (salary + 5), C26: (salary + 6), C27: (salary + 7), C28: (salary + 8), C29: (salary + 9), C30: (salary + 10), C31: ((name || '-') || category), C32: ((firstName || ' ') || lastName), C33: ((department || ':') || category), C34: ((email || ':') || name), C35: (value * 2), C36: (value * 3), C37: (value * 4), C38: (salary * 2), C39: (salary * 3), C40: (salary * 4), C41: (value - salary), C42: (salary - value), C43: (value + salary), C44: (value % 10), C45: (salary % 10), C46: __expr, C47: __expr1, C48: (((category = 'A') OR (category = 'B')) OR (category = 'C')), C49: CASE WHEN __expr THEN 'High' ELSE 'Low' END, C50: CASE WHEN __expr1 THEN 'Large' ELSE 'Small' END)]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q107_RuntimeV2LexerManyColumns
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
            new Column("C01", typeof(int), 0),
            new Column("C02", typeof(string), 1),
            new Column("C03", typeof(string), 2),
            new Column("C04", typeof(string), 3),
            new Column("C05", typeof(string), 4),
            new Column("C06", typeof(int), 5),
            new Column("C07", typeof(string), 6),
            new Column("C08", typeof(string), 7),
            new Column("C09", typeof(int), 8),
            new Column("C10", typeof(int), 9),
            new Column("C11", typeof(int), 10),
            new Column("C12", typeof(int), 11),
            new Column("C13", typeof(int), 12),
            new Column("C14", typeof(int), 13),
            new Column("C15", typeof(int), 14),
            new Column("C16", typeof(int), 15),
            new Column("C17", typeof(int), 16),
            new Column("C18", typeof(int), 17),
            new Column("C19", typeof(int), 18),
            new Column("C20", typeof(int), 19),
            new Column("C21", typeof(int), 20),
            new Column("C22", typeof(int), 21),
            new Column("C23", typeof(int), 22),
            new Column("C24", typeof(int), 23),
            new Column("C25", typeof(int), 24),
            new Column("C26", typeof(int), 25),
            new Column("C27", typeof(int), 26),
            new Column("C28", typeof(int), 27),
            new Column("C29", typeof(int), 28),
            new Column("C30", typeof(int), 29),
            new Column("C31", typeof(string), 30),
            new Column("C32", typeof(string), 31),
            new Column("C33", typeof(string), 32),
            new Column("C34", typeof(string), 33),
            new Column("C35", typeof(int), 34),
            new Column("C36", typeof(int), 35),
            new Column("C37", typeof(int), 36),
            new Column("C38", typeof(int), 37),
            new Column("C39", typeof(int), 38),
            new Column("C40", typeof(int), 39),
            new Column("C41", typeof(int), 40),
            new Column("C42", typeof(int), 41),
            new Column("C43", typeof(int), 42),
            new Column("C44", typeof(int), 43),
            new Column("C45", typeof(int), 44),
            new Column("C46", typeof(bool), 45),
            new Column("C47", typeof(bool), 46),
            new Column("C48", typeof(bool?), 47),
            new Column("C49", typeof(string), 48),
            new Column("C50", typeof(string), 49)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0), new Column("Name", typeof(string), 1), new Column("FirstName", typeof(string), 2), new Column("LastName", typeof(string), 3), new Column("Email", typeof(string), 4), new Column("Value", typeof(int), 5), new Column("Category", typeof(string), 6), new Column("Department", typeof(string), 7), new Column("Salary", typeof(int), 8) });
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
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __ko3ikoSchema = provider.GetSchema("#test");
                var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity>.Open(__ko3ikoSchema, "entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var ko3ikoChunk in ko3ikoRows)
                {
                    if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity> ko3ikoChunkView)
                    {
                        if (ko3ikoChunkView.Source is Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity[] ko3ikoChunkViewArray)
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
                                string firstName = ko3iko.FirstName;
                                string lastName = ko3iko.LastName;
                                string email = ko3iko.Email;
                                int value = ko3iko.Value;
                                string category = ko3iko.Category;
                                string department = ko3iko.Department;
                                int salary = ko3iko.Salary;
                                bool __expr = (value > 100);
                                bool __expr1 = (salary > 1000);
                                yield return new ResultRow0(ko3iko.Id, name, firstName, lastName, email, value, category, department, salary, (value + 1), (value + 2), (value + 3), (value + 4), (value + 5), (value + 6), (value + 7), (value + 8), (value + 9), (value + 10), (value + 11), (salary + 1), (salary + 2), (salary + 3), (salary + 4), (salary + 5), (salary + 6), (salary + 7), (salary + 8), (salary + 9), (salary + 10), ((name + "-") + category), ((firstName + " ") + lastName), ((department + ":") + category), ((email + ":") + name), (value * 2), (value * 3), (value * 4), (salary * 2), (salary * 3), (salary * 4), (value - salary), (salary - value), (value + salary), (value % ((int?)10).GetValueOrDefault()), (salary % ((int?)10).GetValueOrDefault()), __expr, __expr1, (((bool?)((((bool?)(Operators.SqlCompare<string, string>(category, "A", (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)))) switch
                                {
                                    true => (bool?)true,
                                    false => (bool?)Operators.SqlCompare<string, string>(category, "B", (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)),
                                    _ => (bool?)Operators.SqlCompare<string, string>(category, "B", (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) | (bool?)null

                                }))) switch
                                {
                                    true => (bool?)true,
                                    false => (bool?)Operators.SqlCompare<string, string>(category, "C", (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)),
                                    _ => (bool?)Operators.SqlCompare<string, string>(category, "C", (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) | (bool?)null

                                }), __expr ? (string)"High" : (string)"Low", __expr1 ? (string)"Large" : (string)"Small");
                            }

                            continue;
                        }

                        if (ko3ikoChunkView.Source is List<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity> ko3ikoChunkViewList)
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
                                string firstName = ko3iko.FirstName;
                                string lastName = ko3iko.LastName;
                                string email = ko3iko.Email;
                                int value = ko3iko.Value;
                                string category = ko3iko.Category;
                                string department = ko3iko.Department;
                                int salary = ko3iko.Salary;
                                bool __expr = (value > 100);
                                bool __expr1 = (salary > 1000);
                                yield return new ResultRow0(ko3iko.Id, name, firstName, lastName, email, value, category, department, salary, (value + 1), (value + 2), (value + 3), (value + 4), (value + 5), (value + 6), (value + 7), (value + 8), (value + 9), (value + 10), (value + 11), (salary + 1), (salary + 2), (salary + 3), (salary + 4), (salary + 5), (salary + 6), (salary + 7), (salary + 8), (salary + 9), (salary + 10), ((name + "-") + category), ((firstName + " ") + lastName), ((department + ":") + category), ((email + ":") + name), (value * 2), (value * 3), (value * 4), (salary * 2), (salary * 3), (salary * 4), (value - salary), (salary - value), (value + salary), (value % ((int?)10).GetValueOrDefault()), (salary % ((int?)10).GetValueOrDefault()), __expr, __expr1, (((bool?)((((bool?)(Operators.SqlCompare<string, string>(category, "A", (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)))) switch
                                {
                                    true => (bool?)true,
                                    false => (bool?)Operators.SqlCompare<string, string>(category, "B", (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)),
                                    _ => (bool?)Operators.SqlCompare<string, string>(category, "B", (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) | (bool?)null

                                }))) switch
                                {
                                    true => (bool?)true,
                                    false => (bool?)Operators.SqlCompare<string, string>(category, "C", (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)),
                                    _ => (bool?)Operators.SqlCompare<string, string>(category, "C", (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) | (bool?)null

                                }), __expr ? (string)"High" : (string)"Low", __expr1 ? (string)"Large" : (string)"Small");
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
                        string firstName = ko3iko.FirstName;
                        string lastName = ko3iko.LastName;
                        string email = ko3iko.Email;
                        int value = ko3iko.Value;
                        string category = ko3iko.Category;
                        string department = ko3iko.Department;
                        int salary = ko3iko.Salary;
                        bool __expr = (value > 100);
                        bool __expr1 = (salary > 1000);
                        yield return new ResultRow0(ko3iko.Id, name, firstName, lastName, email, value, category, department, salary, (value + 1), (value + 2), (value + 3), (value + 4), (value + 5), (value + 6), (value + 7), (value + 8), (value + 9), (value + 10), (value + 11), (salary + 1), (salary + 2), (salary + 3), (salary + 4), (salary + 5), (salary + 6), (salary + 7), (salary + 8), (salary + 9), (salary + 10), ((name + "-") + category), ((firstName + " ") + lastName), ((department + ":") + category), ((email + ":") + name), (value * 2), (value * 3), (value * 4), (salary * 2), (salary * 3), (salary * 4), (value - salary), (salary - value), (value + salary), (value % ((int?)10).GetValueOrDefault()), (salary % ((int?)10).GetValueOrDefault()), __expr, __expr1, (((bool?)((((bool?)(Operators.SqlCompare<string, string>(category, "A", (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)))) switch
                        {
                            true => (bool?)true,
                            false => (bool?)Operators.SqlCompare<string, string>(category, "B", (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)),
                            _ => (bool?)Operators.SqlCompare<string, string>(category, "B", (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) | (bool?)null

                        }))) switch
                        {
                            true => (bool?)true,
                            false => (bool?)Operators.SqlCompare<string, string>(category, "C", (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)),
                            _ => (bool?)Operators.SqlCompare<string, string>(category, "C", (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) | (bool?)null

                        }), __expr ? (string)"High" : (string)"Low", __expr1 ? (string)"Large" : (string)"Small");
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
            private const string __columnIndexPairs = "C01\n0\nC02\n1\nC03\n2\nC04\n3\nC05\n4\nC06\n5\nC07\n6\nC08\n7\nC09\n8\nC10\n9\nC11\n10\nC12\n11\nC13\n12\nC14\n13\nC15\n14\nC16\n15\nC17\n16\nC18\n17\nC19\n18\nC20\n19\nC21\n20\nC22\n21\nC23\n22\nC24\n23\nC25\n24\nC26\n25\nC27\n26\nC28\n27\nC29\n28\nC30\n29\nC31\n30\nC32\n31\nC33\n32\nC34\n33\nC35\n34\nC36\n35\nC37\n36\nC38\n37\nC39\n38\nC40\n39\nC41\n40\nC42\n41\nC43\n42\nC44\n43\nC45\n44\nC46\n45\nC47\n46\nC48\n47\nC49\n48\nC50\n49";
            private static readonly Dictionary<string, int> __columnIndexes = CreateColumnIndexes();
            public ResultRow0(int __value0, string __value1, string __value2, string __value3, string __value4, int __value5, string __value6, string __value7, int __value8, int __value9, int __value10, int __value11, int __value12, int __value13, int __value14, int __value15, int __value16, int __value17, int __value18, int __value19, int __value20, int __value21, int __value22, int __value23, int __value24, int __value25, int __value26, int __value27, int __value28, int __value29, string __value30, string __value31, string __value32, string __value33, int __value34, int __value35, int __value36, int __value37, int __value38, int __value39, int __value40, int __value41, int __value42, int __value43, int __value44, bool __value45, bool __value46, bool? __value47, string __value48, string __value49)
            {
                C01 = __value0;
                C02 = __value1;
                C03 = __value2;
                C04 = __value3;
                C05 = __value4;
                C06 = __value5;
                C07 = __value6;
                C08 = __value7;
                C09 = __value8;
                C10 = __value9;
                C11 = __value10;
                C12 = __value11;
                C13 = __value12;
                C14 = __value13;
                C15 = __value14;
                C16 = __value15;
                C17 = __value16;
                C18 = __value17;
                C19 = __value18;
                C20 = __value19;
                C21 = __value20;
                C22 = __value21;
                C23 = __value22;
                C24 = __value23;
                C25 = __value24;
                C26 = __value25;
                C27 = __value26;
                C28 = __value27;
                C29 = __value28;
                C30 = __value29;
                C31 = __value30;
                C32 = __value31;
                C33 = __value32;
                C34 = __value33;
                C35 = __value34;
                C36 = __value35;
                C37 = __value36;
                C38 = __value37;
                C39 = __value38;
                C40 = __value39;
                C41 = __value40;
                C42 = __value41;
                C43 = __value42;
                C44 = __value43;
                C45 = __value44;
                C46 = __value45;
                C47 = __value46;
                C48 = __value47;
                C49 = __value48;
                C50 = __value49;
            }

            public int C01 { get; private set; }
            public string C02 { get; private set; }
            public string C03 { get; private set; }
            public string C04 { get; private set; }
            public string C05 { get; private set; }
            public int C06 { get; private set; }
            public string C07 { get; private set; }
            public string C08 { get; private set; }
            public int C09 { get; private set; }
            public int C10 { get; private set; }
            public int C11 { get; private set; }
            public int C12 { get; private set; }
            public int C13 { get; private set; }
            public int C14 { get; private set; }
            public int C15 { get; private set; }
            public int C16 { get; private set; }
            public int C17 { get; private set; }
            public int C18 { get; private set; }
            public int C19 { get; private set; }
            public int C20 { get; private set; }
            public int C21 { get; private set; }
            public int C22 { get; private set; }
            public int C23 { get; private set; }
            public int C24 { get; private set; }
            public int C25 { get; private set; }
            public int C26 { get; private set; }
            public int C27 { get; private set; }
            public int C28 { get; private set; }
            public int C29 { get; private set; }
            public int C30 { get; private set; }
            public string C31 { get; private set; }
            public string C32 { get; private set; }
            public string C33 { get; private set; }
            public string C34 { get; private set; }
            public int C35 { get; private set; }
            public int C36 { get; private set; }
            public int C37 { get; private set; }
            public int C38 { get; private set; }
            public int C39 { get; private set; }
            public int C40 { get; private set; }
            public int C41 { get; private set; }
            public int C42 { get; private set; }
            public int C43 { get; private set; }
            public int C44 { get; private set; }
            public int C45 { get; private set; }
            public bool C46 { get; private set; }
            public bool C47 { get; private set; }
            public bool? C48 { get; private set; }
            public string C49 { get; private set; }
            public string C50 { get; private set; }
            public override int Count => 50;

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C01 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.C02 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.C03 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.C04 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.C05 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    5 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C06 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    6 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.C07 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    7 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.C08 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    8 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C09 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    9 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C10 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    10 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C11 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    11 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C12 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    12 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C13 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    13 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C14 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    14 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C15 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    15 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C16 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    16 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C17 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    17 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C18 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    18 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C19 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    19 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C20 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    20 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C21 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    21 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C22 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    22 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C23 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    23 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C24 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    24 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C25 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    25 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C26 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    26 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C27 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    27 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C28 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    28 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C29 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    29 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C30 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    30 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.C31 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    31 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.C32 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    32 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.C33 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    33 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.C34 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    34 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C35 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    35 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C36 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    36 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C37 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    37 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C38 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    38 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C39 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    39 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C40 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    40 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C41 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    41 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C42 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    42 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C43 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    43 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C44 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    44 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C45 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    45 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, bool>(this.C46 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<bool, T>(value)),
                    46 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, bool>(this.C47 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<bool, T>(value)),
                    47 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, bool?>(this.C48 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<bool?, T>(value)),
                    48 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.C49 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    49 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.C50 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C01),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.C02),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.C03),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.C04),
                4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.C05),
                5 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C06),
                6 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.C07),
                7 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.C08),
                8 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C09),
                9 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C10),
                10 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C11),
                11 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C12),
                12 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C13),
                13 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C14),
                14 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C15),
                15 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C16),
                16 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C17),
                17 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C18),
                18 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C19),
                19 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C20),
                20 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C21),
                21 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C22),
                22 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C23),
                23 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C24),
                24 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C25),
                25 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C26),
                26 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C27),
                27 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C28),
                28 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C29),
                29 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C30),
                30 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.C31),
                31 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.C32),
                32 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.C33),
                33 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.C34),
                34 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C35),
                35 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C36),
                36 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C37),
                37 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C38),
                38 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C39),
                39 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C40),
                40 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C41),
                41 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C42),
                42 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C43),
                43 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C44),
                44 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.C45),
                45 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, bool>(this.C46),
                46 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, bool>(this.C47),
                47 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, bool?>(this.C48),
                48 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.C49),
                49 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.C50),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => __columnIndexes.ContainsKey(name);
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.C01, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.C02, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.C03, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.C04, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(4, this.C05, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(5, this.C06, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(6, this.C07, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(7, this.C08, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(8, this.C09, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(9, this.C10, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(10, this.C11, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(11, this.C12, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(12, this.C13, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(13, this.C14, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(14, this.C15, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(15, this.C16, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(16, this.C17, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(17, this.C18, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(18, this.C19, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(19, this.C20, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(20, this.C21, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(21, this.C22, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(22, this.C23, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(23, this.C24, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(24, this.C25, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(25, this.C26, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(26, this.C27, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(27, this.C28, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(28, this.C29, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(29, this.C30, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(30, this.C31, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(31, this.C32, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(32, this.C33, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(33, this.C34, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(34, this.C35, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(35, this.C36, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(36, this.C37, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(37, this.C38, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(38, this.C39, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(39, this.C40, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(40, this.C41, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(41, this.C42, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(42, this.C43, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(43, this.C44, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(44, this.C45, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(45, this.C46, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(46, this.C47, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(47, this.C48, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(48, this.C49, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(49, this.C50, ref visitor);
            }

            private static Dictionary<string, int> CreateColumnIndexes()
            {
                var pairs = __columnIndexPairs.Split('\n');
                var indexes = new Dictionary<string, int>(pairs.Length / 2, StringComparer.Ordinal);
                for (var index = 0; index < pairs.Length; index += 2)
                    indexes.Add(pairs[index], int.Parse(pairs[index + 1], System.Globalization.CultureInfo.InvariantCulture));
                return indexes;
            }
        }
    }
}
