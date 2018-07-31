using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Core.Schema;
using Musoq.Plugins;
using Musoq.Schema;
using Environment = System.Environment;

namespace Musoq.Evaluator.Tests.Core
{
    public class TestBase
    {
        protected CancellationTokenSource TokenSource { get; } = new CancellationTokenSource();

        protected CompiledQuery CreateAndRunVirtualMachine<T>(string script,
            IDictionary<string, IEnumerable<T>> sources)
            where T : BasicEntity
        {
            return InstanceCreator.CompileForExecution(script, new SchemaProvider<T>(sources));
        }

        static TestBase()
        {
            new Plugins.Environment().SetValue(Constants.NetStandardDllEnvironmentName, EnvironmentUtils.GetOrCreateEnvironmentVariable());
        }
    }
}