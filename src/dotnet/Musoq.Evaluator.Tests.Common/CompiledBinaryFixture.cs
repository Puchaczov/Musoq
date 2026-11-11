using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Musoq.Evaluator.Build;
using Musoq.Evaluator.Runtime;

namespace Musoq.Evaluator.Tests;

/// <summary>Typed entry points implemented beside the generated schema under test.</summary>
public interface ICompiledBinaryFixture
{
    int BytesConsumed { get; }
    object Interpret(byte[] data);
    (bool Success, object? Result) TryInterpret(byte[] data);
    object PartialInterpret(byte[] data);
}

internal sealed class CompiledBinaryFixture : ICompiledBinaryFixture, IDisposable
{
    private readonly EvaluatorRuntimeEnvironment _environment = new();
    private readonly InterpreterCompilationUnit _unit;
    private readonly ICompiledBinaryFixture _adapter;

    internal CompiledBinaryFixture(string code, string schemaName, string[] typeArguments)
    {
        try
        {
            var references = new FixtureReferences(_environment);
            var compilation = _environment.CreateCompilation("FixtureProjection")
                .AddReferences(references.GetReferences())
                .AddSyntaxTrees(CSharpSyntaxTree.ParseText(code));
            var adapter = SchemaFixtureProjection.Create(compilation, schemaName, typeArguments);
            _unit = new InterpreterCompilationUnit($"TestAssembly_{Guid.NewGuid():N}", code + adapter,
                _environment.CompilationFactory, references, DefaultAssemblyLoader.Instance);
            if (!_unit.Compile())
                throw new InvalidOperationException(string.Join(Environment.NewLine, _unit.GetErrorMessages()));
            _adapter = _unit.CreateInterpreterInstance<ICompiledBinaryFixture>("BinaryFixtureAdapter")
                ?? throw new InvalidOperationException("Compiled fixture adapter was not activated.");
        }
        catch
        {
            _unit?.Dispose();
            _environment.Dispose();
            throw;
        }
    }

    public int BytesConsumed => _adapter.BytesConsumed;
    public object Interpret(byte[] data) => _adapter.Interpret(data);
    public (bool Success, object? Result) TryInterpret(byte[] data) => _adapter.TryInterpret(data);
    public object PartialInterpret(byte[] data) => _adapter.PartialInterpret(data);

    public void Dispose()
    {
        _unit.Dispose();
        _environment.Dispose();
    }

    private sealed class FixtureReferences(EvaluatorRuntimeEnvironment environment) : IInterpreterReferenceProvider
    {
        public IReadOnlyList<MetadataReference> GetReferences() =>
        [
            .. new DefaultInterpreterReferenceProvider(environment.MetadataReferenceCache).GetReferences(),
            environment.GetOrCreateMetadataReference(typeof(ICompiledBinaryFixture).Assembly.Location)
        ];
    }
}
