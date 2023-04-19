using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Musoq.Converter;
using Musoq.Converter.Build;
using Musoq.Plugins;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests.Schema.Basic
{
    public class BasicEntityTestBase
    {
        protected CancellationTokenSource TokenSource { get; } = new();
        
        protected BuildItems CreateBuildItems<T>(
            string script)
            where T : UsedColumnsOrUsedWhereEntity
        {
            var mock = new Mock<IDictionary<string, IEnumerable<T>>>();
            mock.Setup(f => f[It.IsAny<string>()]).Returns(new List<T>());
            return InstanceCreator.CreateForAnalyze(
                script, 
                Guid.NewGuid().ToString(), 
                new UsedColumnsOrUsedWhereSchemaProvider<T>(mock.Object));
        }

        protected CompiledQuery CreateAndRunVirtualMachine<T>(
            string script,
            IDictionary<string, IEnumerable<T>> sources,
            IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>> positionalEnvironmentVariables = null)
            where T : BasicEntity
        {
            return InstanceCreator.CompileForExecution(
                script, 
                Guid.NewGuid().ToString(), 
                new BasicSchemaProvider<T>(sources),
                positionalEnvironmentVariables ?? CreateMockedEnvironmentVariables());
        }

        private IReadOnlyDictionary<uint,IReadOnlyDictionary<string,string>> CreateMockedEnvironmentVariables()
        {
            var environmentVariablesMock = new Mock<IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>>>();
            environmentVariablesMock.Setup(f => f[It.IsAny<uint>()]).Returns(new Dictionary<string, string>());

            return environmentVariablesMock.Object;
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

        static BasicEntityTestBase()
        {
            new Plugins.Environment().SetValue(Constants.NetStandardDllEnvironmentName, EnvironmentUtils.GetOrCreateEnvironmentVariable());

            Culture.ApplyWithDefaultCulture();
        }
    }
}