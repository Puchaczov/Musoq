using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Musoq.Evaluator.Build;
using Musoq.Evaluator.Runtime;

namespace Musoq.Evaluator.Tests;

public interface ICompiledTextFixture
{
    object Parse(string text);
}

internal sealed class CompiledTextFixture : ICompiledTextFixture, IDisposable
{
    private readonly EvaluatorRuntimeEnvironment _environment = new();
    private readonly InterpreterCompilationUnit _unit;
    private readonly ICompiledTextFixture _adapter;

    internal CompiledTextFixture(string code, string schemaName)
    {
        try
        {
            var references = new FixtureReferences(_environment);
            var compilation = _environment.CreateCompilation("TextFixtureProjection")
                .AddReferences(references.GetReferences()).AddSyntaxTrees(CSharpSyntaxTree.ParseText(code));
            var adapter = SchemaFixtureProjection.CreateText(compilation, schemaName);
            _unit = new InterpreterCompilationUnit($"TextFixture_{Guid.NewGuid():N}", code + adapter,
                _environment.CompilationFactory, references, DefaultAssemblyLoader.Instance);
            if (!_unit.Compile())
                throw new InvalidOperationException(string.Join(Environment.NewLine, _unit.GetErrorMessages()));
            _adapter = _unit.CreateInterpreterInstance<ICompiledTextFixture>("TextFixtureAdapter")
                ?? throw new InvalidOperationException("Text fixture adapter was not activated.");
        }
        catch
        {
            _unit?.Dispose();
            _environment.Dispose();
            throw;
        }
    }

    public object Parse(string text) => _adapter.Parse(text);
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
            environment.GetOrCreateMetadataReference(typeof(ICompiledTextFixture).Assembly.Location)
        ];
    }
}
