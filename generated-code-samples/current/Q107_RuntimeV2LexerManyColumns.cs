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
      C48: bool <- field C48
      C49: string <- field C49
      C50: string <- field C50

  Body
    SourceScan [ko3iko: RuntimeV2RegressionEntity] -> ko3ikoRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
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
      AppendShape [result <- ResultShape0(C01: ko3iko.Id, C02: name, C03: firstName, C04: lastName, C05: email, C06: value, C07: category, C08: department, C09: salary, C10: (value + 1), C11: (value + 2), C12: (value + 3), C13: (value + 4), C14: (value + 5), C15: (value + 6), C16: (value + 7), C17: (value + 8), C18: (value + 9), C19: (value + 10), C20: (value + 11), C21: (salary + 1), C22: (salary + 2), C23: (salary + 3), C24: (salary + 4), C25: (salary + 5), C26: (salary + 6), C27: (salary + 7), C28: (salary + 8), C29: (salary + 9), C30: (salary + 10), C31: ((name || '-') || category), C32: ((firstName || ' ') || lastName), C33: ((department || ':') || category), C34: ((email || ':') || name), C35: (value * 2), C36: (value * 3), C37: (value * 4), C38: (salary * 2), C39: (salary * 3), C40: (salary * 4), C41: (value - salary), C42: (salary - value), C43: (value + salary), C44: (value % 10), C45: (salary % 10), C46: __expr, C47: __expr1, C48: (((category = 'A') OR (category = 'B')) OR (category = 'C')), C49: CASE WHEN __expr THEN 'High' ELSE 'Low' END, C50: CASE WHEN __expr1 THEN 'Large' ELSE 'Small' END)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IParameterizedRunnable
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
            new Column("C48", typeof(bool), 47),
            new Column("C49", typeof(string), 48),
            new Column("C50", typeof(string), 49)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0), new Column("Name", typeof(string), 1), new Column("FirstName", typeof(string), 2), new Column("LastName", typeof(string), 3), new Column("Email", typeof(string), 4), new Column("Value", typeof(int), 5), new Column("Category", typeof(string), 6), new Column("Department", typeof(string), 7), new Column("Salary", typeof(int), 8) });
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
                yield return new ResultRow0(__musoqShapeRow.C01, __musoqShapeRow.C02, __musoqShapeRow.C03, __musoqShapeRow.C04, __musoqShapeRow.C05, __musoqShapeRow.C06, __musoqShapeRow.C07, __musoqShapeRow.C08, __musoqShapeRow.C09, __musoqShapeRow.C10, __musoqShapeRow.C11, __musoqShapeRow.C12, __musoqShapeRow.C13, __musoqShapeRow.C14, __musoqShapeRow.C15, __musoqShapeRow.C16, __musoqShapeRow.C17, __musoqShapeRow.C18, __musoqShapeRow.C19, __musoqShapeRow.C20, __musoqShapeRow.C21, __musoqShapeRow.C22, __musoqShapeRow.C23, __musoqShapeRow.C24, __musoqShapeRow.C25, __musoqShapeRow.C26, __musoqShapeRow.C27, __musoqShapeRow.C28, __musoqShapeRow.C29, __musoqShapeRow.C30, __musoqShapeRow.C31, __musoqShapeRow.C32, __musoqShapeRow.C33, __musoqShapeRow.C34, __musoqShapeRow.C35, __musoqShapeRow.C36, __musoqShapeRow.C37, __musoqShapeRow.C38, __musoqShapeRow.C39, __musoqShapeRow.C40, __musoqShapeRow.C41, __musoqShapeRow.C42, __musoqShapeRow.C43, __musoqShapeRow.C44, __musoqShapeRow.C45, __musoqShapeRow.C46, __musoqShapeRow.C47, __musoqShapeRow.C48, __musoqShapeRow.C49, __musoqShapeRow.C50);
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
                var __ko3ikoSchema = provider.GetSchema("#test");
                var ko3ikoRowsSource = __ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity>("entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var ko3ikoRows = ko3ikoRowsSource.Chunks;
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
                                yield return new ResultShape0(ko3iko.Id, name, firstName, lastName, email, value, category, department, salary, (value + 1), (value + 2), (value + 3), (value + 4), (value + 5), (value + 6), (value + 7), (value + 8), (value + 9), (value + 10), (value + 11), (salary + 1), (salary + 2), (salary + 3), (salary + 4), (salary + 5), (salary + 6), (salary + 7), (salary + 8), (salary + 9), (salary + 10), ((name + "-") + category), ((firstName + " ") + lastName), ((department + ":") + category), ((email + ":") + name), (value * 2), (value * 3), (value * 4), (salary * 2), (salary * 3), (salary * 4), (value - salary), (salary - value), (value + salary), (value % 10), (salary % 10), __expr, __expr1, (((category == "A") || (category == "B")) || (category == "C")), __expr ? (string)"High" : (string)"Low", __expr1 ? (string)"Large" : (string)"Small");
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
                                yield return new ResultShape0(ko3iko.Id, name, firstName, lastName, email, value, category, department, salary, (value + 1), (value + 2), (value + 3), (value + 4), (value + 5), (value + 6), (value + 7), (value + 8), (value + 9), (value + 10), (value + 11), (salary + 1), (salary + 2), (salary + 3), (salary + 4), (salary + 5), (salary + 6), (salary + 7), (salary + 8), (salary + 9), (salary + 10), ((name + "-") + category), ((firstName + " ") + lastName), ((department + ":") + category), ((email + ":") + name), (value * 2), (value * 3), (value * 4), (salary * 2), (salary * 3), (salary * 4), (value - salary), (salary - value), (value + salary), (value % 10), (salary % 10), __expr, __expr1, (((category == "A") || (category == "B")) || (category == "C")), __expr ? (string)"High" : (string)"Low", __expr1 ? (string)"Large" : (string)"Small");
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
                        yield return new ResultShape0(ko3iko.Id, name, firstName, lastName, email, value, category, department, salary, (value + 1), (value + 2), (value + 3), (value + 4), (value + 5), (value + 6), (value + 7), (value + 8), (value + 9), (value + 10), (value + 11), (salary + 1), (salary + 2), (salary + 3), (salary + 4), (salary + 5), (salary + 6), (salary + 7), (salary + 8), (salary + 9), (salary + 10), ((name + "-") + category), ((firstName + " ") + lastName), ((department + ":") + category), ((email + ":") + name), (value * 2), (value * 3), (value * 4), (salary * 2), (salary * 3), (salary * 4), (value - salary), (salary - value), (value + salary), (value % 10), (salary % 10), __expr, __expr1, (((category == "A") || (category == "B")) || (category == "C")), __expr ? (string)"High" : (string)"Low", __expr1 ? (string)"Large" : (string)"Small");
                    }
                }
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
            private static readonly Action<ResultRow0, object>[] __assigners = new Action<ResultRow0, object>[]
            {
                static (row, value) => row.C01 = (int)value,
                static (row, value) => row.C02 = (string)value,
                static (row, value) => row.C03 = (string)value,
                static (row, value) => row.C04 = (string)value,
                static (row, value) => row.C05 = (string)value,
                static (row, value) => row.C06 = (int)value,
                static (row, value) => row.C07 = (string)value,
                static (row, value) => row.C08 = (string)value,
                static (row, value) => row.C09 = (int)value,
                static (row, value) => row.C10 = (int)value,
                static (row, value) => row.C11 = (int)value,
                static (row, value) => row.C12 = (int)value,
                static (row, value) => row.C13 = (int)value,
                static (row, value) => row.C14 = (int)value,
                static (row, value) => row.C15 = (int)value,
                static (row, value) => row.C16 = (int)value,
                static (row, value) => row.C17 = (int)value,
                static (row, value) => row.C18 = (int)value,
                static (row, value) => row.C19 = (int)value,
                static (row, value) => row.C20 = (int)value,
                static (row, value) => row.C21 = (int)value,
                static (row, value) => row.C22 = (int)value,
                static (row, value) => row.C23 = (int)value,
                static (row, value) => row.C24 = (int)value,
                static (row, value) => row.C25 = (int)value,
                static (row, value) => row.C26 = (int)value,
                static (row, value) => row.C27 = (int)value,
                static (row, value) => row.C28 = (int)value,
                static (row, value) => row.C29 = (int)value,
                static (row, value) => row.C30 = (int)value,
                static (row, value) => row.C31 = (string)value,
                static (row, value) => row.C32 = (string)value,
                static (row, value) => row.C33 = (string)value,
                static (row, value) => row.C34 = (string)value,
                static (row, value) => row.C35 = (int)value,
                static (row, value) => row.C36 = (int)value,
                static (row, value) => row.C37 = (int)value,
                static (row, value) => row.C38 = (int)value,
                static (row, value) => row.C39 = (int)value,
                static (row, value) => row.C40 = (int)value,
                static (row, value) => row.C41 = (int)value,
                static (row, value) => row.C42 = (int)value,
                static (row, value) => row.C43 = (int)value,
                static (row, value) => row.C44 = (int)value,
                static (row, value) => row.C45 = (int)value,
                static (row, value) => row.C46 = (bool)value,
                static (row, value) => row.C47 = (bool)value,
                static (row, value) => row.C48 = (bool)value,
                static (row, value) => row.C49 = (string)value,
                static (row, value) => row.C50 = (string)value
            };
            private const string __columnIndexPairs = "C01\n0\nC02\n1\nC03\n2\nC04\n3\nC05\n4\nC06\n5\nC07\n6\nC08\n7\nC09\n8\nC10\n9\nC11\n10\nC12\n11\nC13\n12\nC14\n13\nC15\n14\nC16\n15\nC17\n16\nC18\n17\nC19\n18\nC20\n19\nC21\n20\nC22\n21\nC23\n22\nC24\n23\nC25\n24\nC26\n25\nC27\n26\nC28\n27\nC29\n28\nC30\n29\nC31\n30\nC32\n31\nC33\n32\nC34\n33\nC35\n34\nC36\n35\nC37\n36\nC38\n37\nC39\n38\nC40\n39\nC41\n40\nC42\n41\nC43\n42\nC44\n43\nC45\n44\nC46\n45\nC47\n46\nC48\n47\nC49\n48\nC50\n49";
            private static readonly Dictionary<string, int> __columnIndexes = CreateColumnIndexes();
            public ResultRow0(int __value0, string __value1, string __value2, string __value3, string __value4, int __value5, string __value6, string __value7, int __value8, int __value9, int __value10, int __value11, int __value12, int __value13, int __value14, int __value15, int __value16, int __value17, int __value18, int __value19, int __value20, int __value21, int __value22, int __value23, int __value24, int __value25, int __value26, int __value27, int __value28, int __value29, string __value30, string __value31, string __value32, string __value33, int __value34, int __value35, int __value36, int __value37, int __value38, int __value39, int __value40, int __value41, int __value42, int __value43, int __value44, bool __value45, bool __value46, bool __value47, string __value48, string __value49)
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
            public bool C48 { get; private set; }
            public string C49 { get; private set; }
            public string C50 { get; private set; }
            public override int Count => 50;

            public override void AssignValue(int columnNumber, object value)
            {
                if ((uint)columnNumber >= (uint)__assigners.Length)
                    throw new IndexOutOfRangeException();
                __assigners[columnNumber](this, value);
            }

            public override bool HasColumn(string name) => __columnIndexes.ContainsKey(name);
            private static Dictionary<string, int> CreateColumnIndexes()
            {
                var pairs = __columnIndexPairs.Split('\n');
                var indexes = new Dictionary<string, int>(pairs.Length / 2, StringComparer.Ordinal);
                for (var index = 0; index < pairs.Length; index += 2)
                    indexes.Add(pairs[index], int.Parse(pairs[index + 1], System.Globalization.CultureInfo.InvariantCulture));
                return indexes;
            }

            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)C01,
                1 => (object)C02,
                2 => (object)C03,
                3 => (object)C04,
                4 => (object)C05,
                5 => (object)C06,
                6 => (object)C07,
                7 => (object)C08,
                8 => (object)C09,
                9 => (object)C10,
                10 => (object)C11,
                11 => (object)C12,
                12 => (object)C13,
                13 => (object)C14,
                14 => (object)C15,
                15 => (object)C16,
                16 => (object)C17,
                17 => (object)C18,
                18 => (object)C19,
                19 => (object)C20,
                20 => (object)C21,
                21 => (object)C22,
                22 => (object)C23,
                23 => (object)C24,
                24 => (object)C25,
                25 => (object)C26,
                26 => (object)C27,
                27 => (object)C28,
                28 => (object)C29,
                29 => (object)C30,
                30 => (object)C31,
                31 => (object)C32,
                32 => (object)C33,
                33 => (object)C34,
                34 => (object)C35,
                35 => (object)C36,
                36 => (object)C37,
                37 => (object)C38,
                38 => (object)C39,
                39 => (object)C40,
                40 => (object)C41,
                41 => (object)C42,
                42 => (object)C43,
                43 => (object)C44,
                44 => (object)C45,
                45 => (object)C46,
                46 => (object)C47,
                47 => (object)C48,
                48 => (object)C49,
                49 => (object)C50,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => __columnIndexes.TryGetValue(name, out var columnIndex) ? this[columnIndex] : throw new KeyNotFoundException(name);
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int C01, string C02, string C03, string C04, string C05, int C06, string C07, string C08, int C09, int C10, int C11, int C12, int C13, int C14, int C15, int C16, int C17, int C18, int C19, int C20, int C21, int C22, int C23, int C24, int C25, int C26, int C27, int C28, int C29, int C30, string C31, string C32, string C33, string C34, int C35, int C36, int C37, int C38, int C39, int C40, int C41, int C42, int C43, int C44, int C45, bool C46, bool C47, bool C48, string C49, string C50)
            {
                this.C01 = C01;
                this.C02 = C02;
                this.C03 = C03;
                this.C04 = C04;
                this.C05 = C05;
                this.C06 = C06;
                this.C07 = C07;
                this.C08 = C08;
                this.C09 = C09;
                this.C10 = C10;
                this.C11 = C11;
                this.C12 = C12;
                this.C13 = C13;
                this.C14 = C14;
                this.C15 = C15;
                this.C16 = C16;
                this.C17 = C17;
                this.C18 = C18;
                this.C19 = C19;
                this.C20 = C20;
                this.C21 = C21;
                this.C22 = C22;
                this.C23 = C23;
                this.C24 = C24;
                this.C25 = C25;
                this.C26 = C26;
                this.C27 = C27;
                this.C28 = C28;
                this.C29 = C29;
                this.C30 = C30;
                this.C31 = C31;
                this.C32 = C32;
                this.C33 = C33;
                this.C34 = C34;
                this.C35 = C35;
                this.C36 = C36;
                this.C37 = C37;
                this.C38 = C38;
                this.C39 = C39;
                this.C40 = C40;
                this.C41 = C41;
                this.C42 = C42;
                this.C43 = C43;
                this.C44 = C44;
                this.C45 = C45;
                this.C46 = C46;
                this.C47 = C47;
                this.C48 = C48;
                this.C49 = C49;
                this.C50 = C50;
            }

            public int C01 { get; }
            public string C02 { get; }
            public string C03 { get; }
            public string C04 { get; }
            public string C05 { get; }
            public int C06 { get; }
            public string C07 { get; }
            public string C08 { get; }
            public int C09 { get; }
            public int C10 { get; }
            public int C11 { get; }
            public int C12 { get; }
            public int C13 { get; }
            public int C14 { get; }
            public int C15 { get; }
            public int C16 { get; }
            public int C17 { get; }
            public int C18 { get; }
            public int C19 { get; }
            public int C20 { get; }
            public int C21 { get; }
            public int C22 { get; }
            public int C23 { get; }
            public int C24 { get; }
            public int C25 { get; }
            public int C26 { get; }
            public int C27 { get; }
            public int C28 { get; }
            public int C29 { get; }
            public int C30 { get; }
            public string C31 { get; }
            public string C32 { get; }
            public string C33 { get; }
            public string C34 { get; }
            public int C35 { get; }
            public int C36 { get; }
            public int C37 { get; }
            public int C38 { get; }
            public int C39 { get; }
            public int C40 { get; }
            public int C41 { get; }
            public int C42 { get; }
            public int C43 { get; }
            public int C44 { get; }
            public int C45 { get; }
            public bool C46 { get; }
            public bool C47 { get; }
            public bool C48 { get; }
            public string C49 { get; }
            public string C50 { get; }
        }
    }
}
