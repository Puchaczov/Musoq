using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Core.Schema;
using Musoq.Plugins;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests.Core
{
    public class TestBase
    {
        protected CancellationTokenSource TokenSource { get; } = new CancellationTokenSource();

        protected CompiledQuery CreateAndRunVirtualMachine<T>(
            string script,
            IDictionary<string, IEnumerable<T>> sources)
            where T : BasicEntity
        {
            return InstanceCreator.CompileForExecution(script, Guid.NewGuid().ToString(), new SchemaProvider<T>(sources));
        }

        protected void TestMethodTemplate<TResult>(string operation, TResult score)
        {
            var query = $"select {operation} from #A.Entities()";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {"#A", new[] {new BasicEntity("ABCAACBA")}}
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual(typeof(TResult), table.Columns.ElementAt(0).ColumnType);

            Assert.AreEqual(score, table[0][0]);
        }

        static TestBase()
        {
            new Plugins.Environment().SetValue(Constants.NetStandardDllEnvironmentName, EnvironmentUtils.GetOrCreateEnvironmentVariable());

            Culture.ApplyWithDefaultCulture();
        }
    }
}