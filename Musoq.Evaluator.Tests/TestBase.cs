using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
