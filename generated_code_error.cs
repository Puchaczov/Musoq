namespace Query.Compiled_891
{
    using System;
    using System.Threading;
    using Microsoft.Extensions.Logging;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Linq;
    using Musoq.Plugins;
    using Musoq.Schema;
    using Musoq.Evaluator;
    using Musoq.Parser.Nodes.From;
    using Musoq.Parser.Nodes;
    using Musoq.Evaluator.Tables;
    using Musoq.Evaluator.Helpers;
    using System.Dynamic;

    public class CompiledQuery : BaseOperations, IRunnable
    {
        private Table[] _tableResults = new Table[0];

        public class ko3ikoRow0 : Row
        {
            public long Item0;// 255 / 0

            public override object[] Contexts { get; }

            public ko3ikoRow0(long item0, object[] context0)
            {
                Item0 = item0;
                Contexts = EvaluationHelper.FlattenContexts(context0);
            }

            public override object this[int index]
            {
                get
                {
                    if (index == 0)
                        return Item0;
                    throw new IndexOutOfRangeException();
                }
            }

            public override int Count => 1;

            public override object[] Values => new object[] { Item0 };
        }

        private Table ComputeTable_ko3iko_0_0(ISchemaProvider provider, IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>> positionalEnvironmentVariables, IReadOnlyDictionary<string, (SchemaFromNode FromNode, IReadOnlyCollection<ISchemaColumn> UsedColumns, WhereNode WhereNode, bool HasExternallyProvidedTypes)> queriesInformation, ILogger logger, CancellationToken token)
        {
            var stats = new AmendableQueryStats();
            var ko3ikoScore = new Table("ko3ikoScore", new Column[] { new Column(@"255 / 0", typeof(long), 0) });
            var ko3ikoInferredInfoTable = new ISchemaColumn[] { new Column("Name", typeof(string), 10), new Column("City", typeof(string), 11), new Column("Country", typeof(string), 12), new Column("Population", typeof(decimal), 13), new Column("Self", typeof(Musoq.Evaluator.Tests.Schema.Basic.BasicEntity), 14), new Column("Money", typeof(decimal), 15), new Column("Month", typeof(string), 16), new Column("Time", typeof(System.DateTime), 17), new Column("Id", typeof(int), 18), new Column("NullableValue", typeof(System.Nullable<int>), 19), new Column("Array", typeof(System.Int32[]), 20), new Column("Other", typeof(Musoq.Evaluator.Tests.Schema.Basic.BasicEntity), 21), new Column("Dictionary", typeof(System.Collections.Generic.Dictionary<string, string>), 22) };
            var ko3iko = provider.GetSchema("#A");
            var ko3ikoRows = ko3iko.GetRowSource("Entities", new RuntimeContext(token, ko3ikoInferredInfoTable, positionalEnvironmentVariables[0], queriesInformation["ko3iko:1"], logger), new Object[] { });
            ;
            try
            {
                Parallel.ForEach(ko3ikoRows.Rows, (score) => { token.ThrowIfCancellationRequested(); var currentRowStats = stats.IncrementRowNumber(); ko3ikoScore.Add(new ko3ikoRow0((long)(((long)255L) / ((long)0L)), score.Contexts)); });
            }
            catch (AggregateException ex)
            {
                throw ex.InnerExceptions.First();
            }

            return ko3ikoScore;
        }

        public Table Run(CancellationToken token)
        {
            return ComputeTable_ko3iko_0_0(Provider, PositionalEnvironmentVariables, QueriesInformation, Logger, token);
        }

        public ISchemaProvider Provider { get; set; }
        public IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>> PositionalEnvironmentVariables { get; set; }
        public IReadOnlyDictionary<string, (SchemaFromNode FromNode, IReadOnlyCollection<ISchemaColumn> UsedColumns, WhereNode WhereNode, bool HasExternallyProvidedTypes)> QueriesInformation { get; set; }
        public ILogger Logger { get; set; }
    }
}