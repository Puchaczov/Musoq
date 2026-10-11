using System;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Dynamic;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;

namespace Musoq.Evaluator.Tests;

public partial class DynamicSourceQueryTests
{
    private CompiledQuery CompileNative<T>(string query, T row) =>
        InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString("N"),
            LibraryExecutionFixture.CreateProvider<T, DynamicLibrary>([row]), LoggerResolver,
            ExecutionIrCompilationOptions, TestContext.CancellationToken);

    public sealed record NativeRoot<T>(T Complex);
    public sealed record NativeArray<T>(T[] Array);
    public sealed record NativePerson(int Id, string? Name);
}
