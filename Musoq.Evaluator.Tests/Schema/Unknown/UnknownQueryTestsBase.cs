using System;
using System.Collections.Generic;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests.Schema.Unknown;

public class UnknownQueryTestsBase
{
    protected static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    static UnknownQueryTestsBase()
    {
        Culture.ApplyWithDefaultCulture();
    }

    protected ILoggerResolver LoggerResolver { get; } = new TestsLoggerResolver();

    protected CompiledQuery CreateAndRunVirtualMachine(
        string script,
        IReadOnlyCollection<dynamic> values)
    {
        return InstanceCreator.CompileForExecution(
            script,
            Guid.NewGuid().ToString(),
            new UnknownSchemaProvider(values),
            LoggerResolver,
            TestCompilationOptions);
    }
}