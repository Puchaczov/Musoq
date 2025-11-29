using System;
using System.Collections.Generic;
using Musoq.Converter;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Components;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Schema.PathValue;

public class PathValueQueryTestBase
{
    protected ILoggerResolver LoggerResolver { get; } = new TestsLoggerResolver();

    protected CompiledQuery CreateAndRunVirtualMachine(string script, IEnumerable<PathValueEntity> entities)
    {
        return InstanceCreator.CompileForExecution(
            script,
            Guid.NewGuid().ToString(),
            new PathValueSchemaProvider(entities),
            LoggerResolver);
    }

    protected Table RunQuery(string script, IEnumerable<PathValueEntity> entities)
    {
        var vm = CreateAndRunVirtualMachine(script, entities);
        return vm.Run();
    }

    protected class PathValueSchemaProvider : ISchemaProvider
    {
        private readonly IEnumerable<PathValueEntity> _entities;

        public PathValueSchemaProvider(IEnumerable<PathValueEntity> entities)
        {
            _entities = entities;
        }

        public ISchema GetSchema(string schema)
        {
            return new PathValueSchema(_entities);
        }
    }
}
