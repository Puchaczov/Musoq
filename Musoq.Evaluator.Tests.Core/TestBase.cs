using System.Collections.Generic;
using System.Threading;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Core.Schema;

namespace Musoq.Evaluator.Tests.Core
{
    public class TestBase
    {
        protected CancellationTokenSource TokenSource { get; } = new CancellationTokenSource();

        protected CompiledQuery CreateAndRunVirtualMachine<T>(string script,
            IDictionary<string, IEnumerable<T>> sources)
            where T : BasicEntity
        {
            return InstanceCreator.Create(script, new SchemaProvider<T>(sources));
        }
    }
}