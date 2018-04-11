using System.Collections.Generic;
using Musoq.Converter;
using Musoq.Evaluator.Instructions;
using Musoq.Evaluator.Tests.Schema;

namespace Musoq.Evaluator.Tests
{
    public class TestBase
    {
        protected IVirtualMachine CreateAndRunVirtualMachine<T>(string script,
            IDictionary<string, IEnumerable<T>> sources)
            where T : BasicEntity
        {
            return InstanceCreator.Create(script, new SchemaProvider<T>(sources));
        }
    }
}
