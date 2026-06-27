using System;
using System.Collections.Generic;
using Musoq.Converter;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Components;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Schema.PathValue;

public class PathValueQueryTestBase
{
    protected static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    protected ILoggerResolver LoggerResolver { get; } = new TestsLoggerResolver();

    protected CompiledQuery CreateAndRunVirtualMachine(string script, IEnumerable<PathValueEntity> entities)
    {
        return InstanceCreator.CompileForExecution(
            script,
            Guid.NewGuid().ToString(),
            new PathValueSchemaProvider(entities),
            LoggerResolver,
            TestCompilationOptions);
    }

    protected Table RunQuery(string script, IEnumerable<PathValueEntity> entities)
    {
        var vm = CreateAndRunVirtualMachine(script, entities);
        return TableMaterializationTestHelper.Materialize(vm.Run());
    }

    protected class PathValueSchemaProvider(IEnumerable<PathValueEntity> entities) : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            return new PathValueSchema(entities);
        }
    }
}
