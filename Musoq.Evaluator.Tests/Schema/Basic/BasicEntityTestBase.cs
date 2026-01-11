using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Musoq.Converter;
using Musoq.Converter.Build;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Components;
using Musoq.Schema;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests.Schema.Basic;

public class BasicEntityTestBase
{
    protected static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);
    
    protected static readonly CompilationOptions ValidationEnabledCompilationOptions = new(usePrimitiveTypeValidation: true);
    
    static BasicEntityTestBase()
    {
        Culture.ApplyWithDefaultCulture();
    }
        
    protected CancellationTokenSource TokenSource { get; } = new();
    
    protected ILoggerResolver LoggerResolver { get; } = new TestsLoggerResolver();
        
    protected BuildItems CreateBuildItems<T>(string script)
    {
        return InstanceCreator.CreateForAnalyze(
            script, 
            Guid.NewGuid().ToString(), 
            typeof(T) == typeof(UsedColumnsOrUsedWhereEntity) ? 
                new UsedColumnsOrUsedWhereSchemaProvider<UsedColumnsOrUsedWhereEntity>(CreateMockObjectFor<UsedColumnsOrUsedWhereEntity>()) :
                new MockBasedSchemaProvider(CreateMockObjectFor<BasicEntity>()),
            LoggerResolver);
    }

    protected CompiledQuery CreateAndRunVirtualMachine<T>(
        string script,
        IDictionary<string, IEnumerable<T>> sources)
        where T : BasicEntity
    {
        return InstanceCreator.CompileForExecution(
            script, 
            Guid.NewGuid().ToString(), 
            new BasicSchemaProvider<T>(sources),
            LoggerResolver,
            TestCompilationOptions);
    }

    protected CompiledQuery CreateAndRunVirtualMachine<T>(
        string script,
        IDictionary<string, IEnumerable<T>> sources,
        CompilationOptions compilationOptions)
        where T : BasicEntity
    {
        return InstanceCreator.CompileForExecution(
            script, 
            Guid.NewGuid().ToString(), 
            new BasicSchemaProvider<T>(sources),
            LoggerResolver,
            compilationOptions);
    }

    protected CompiledQuery CreateAndRunVirtualMachine(
        string script,
        IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>> positionalEnvironmentVariables = null,
        ISchemaProvider schemaProvider = null)
    {
        return InstanceCreator.CompileForExecution(
            script, 
            Guid.NewGuid().ToString(), 
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);
    }

    private IReadOnlyDictionary<uint,IReadOnlyDictionary<string,string>> CreateMockedEnvironmentVariables()
    {
        var environmentVariablesMock = new Mock<IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>>>();
        environmentVariablesMock.Setup(f => f[It.IsAny<uint>()]).Returns(new Dictionary<string, string>());

        return environmentVariablesMock.Object;
    }

    protected void TestMethodTemplate<TResult>(string operation, TResult score)
    {
        var table = TestResultMethodTemplate(operation);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(typeof(TResult), table.Columns.ElementAt(0).ColumnType);

        Assert.AreEqual(score, table[0][0]);
    }

    protected Table TestResultMethodTemplate(string operation)
    {
        var query = $"select {operation} from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("ABCAACBA")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        return vm.Run();
    }

    private static IDictionary<string, IEnumerable<T>> CreateMockObjectFor<T>()
    {
        var mock = new Mock<IDictionary<string, IEnumerable<T>>>();
        mock.Setup(f => f[It.IsAny<string>()]).Returns([]);
            
        return mock.Object;
    }

    private class MockBasedSchemaProvider(IDictionary<string, IEnumerable<BasicEntity>> schemas)
        : BasicSchemaProvider<BasicEntity>(schemas)
    {
        public override ISchema GetSchema(string schema)
        {
            return new GenericSchema<BasicEntity, BasicEntityTable>(Values[schema], BasicEntity.TestNameToIndexMap, BasicEntity.TestIndexToObjectAccessMap);
        }
    }
}
