using System;
using System.Collections.Generic;
using Musoq.Converter;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests.Schema.Unknown;

public class UnknownQueryTestsBase
{
    protected CompiledQuery CreateAndRunVirtualMachine(
        string script,
        IReadOnlyCollection<dynamic> values)
    {
        return InstanceCreator.CompileForExecution(
            script, 
            Guid.NewGuid().ToString(),
            new UnknownSchemaProvider(values));
    }

    static UnknownQueryTestsBase()
    {
        Culture.ApplyWithDefaultCulture();
    }
}