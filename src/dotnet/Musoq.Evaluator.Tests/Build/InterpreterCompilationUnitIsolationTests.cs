using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Build;
using Musoq.Evaluator.Runtime;

namespace Musoq.Evaluator.Tests.Build;

[TestClass]
public sealed class InterpreterCompilationUnitIsolationTests
{
    private const string ValidSource = """
                                       namespace Musoq.Generated.Interpreters
                                       {
                                           public sealed class TestSchema
                                           {
                                           }

                                           public sealed class GenericSchema<T>
                                           {
                                           }
                                       }
                                       """;

    [TestMethod]
    public void Compile_WhenSourceHasErrors_ShouldExposeDiagnosticsAndSkipAssemblyLoad()
    {
        var references = new EmptyInterpreterReferenceProvider();
        var loader = new TrackingAssemblyLoader();
        var unit = CreateUnit("broken", "namespace Musoq.Generated.Interpreters { public sealed class Broken {", references, loader);

        var succeeded = unit.Compile();

        Assert.IsFalse(succeeded);
        Assert.IsFalse(unit.IsSuccess);
        Assert.IsNull(unit.CompiledAssembly);
        Assert.IsNull(unit.GetAssemblyBytes());
        Assert.AreEqual(1, references.CallCount);
        Assert.AreEqual(0, loader.LoadCallCount);
        Assert.IsTrue(unit.GetErrorMessages().Any(static message => message.Contains("error", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void Compile_WhenSourceIsValid_ShouldLoadAssemblyAndExposeBytes()
    {
        var references = new EmptyInterpreterReferenceProvider();
        var loader = new TrackingAssemblyLoader();
        var unit = CreateUnit("valid", ValidSource, references, loader);

        var succeeded = unit.Compile();

        Assert.IsTrue(succeeded);
        Assert.IsTrue(unit.IsSuccess);
        Assert.IsNotNull(unit.CompiledAssembly);
        Assert.IsNotNull(unit.GetAssemblyBytes());
        Assert.IsTrue(unit.GetAssemblyBytes()!.Length > 0);
        Assert.AreEqual(1, references.CallCount);
        Assert.AreEqual(1, loader.LoadCallCount);
    }

    [TestMethod]
    public void GetInterpreterType_WhenGenericTypeExists_ShouldReturnGenericDefinition()
    {
        var unit = CreateUnit("generic", ValidSource, new EmptyInterpreterReferenceProvider(), new TrackingAssemblyLoader());

        Assert.IsTrue(unit.Compile());

        var type = unit.GetInterpreterType("GenericSchema");

        Assert.IsNotNull(type);
        Assert.AreEqual("GenericSchema`1", type.Name);
        Assert.IsTrue(type.IsGenericTypeDefinition);
    }

    [TestMethod]
    public void GetInterpreterType_WhenTypeDoesNotExist_ShouldReturnNull()
    {
        var unit = CreateUnit("missing", ValidSource, new EmptyInterpreterReferenceProvider(), new TrackingAssemblyLoader());

        Assert.IsTrue(unit.Compile());

        Assert.IsNull(unit.GetInterpreterType("MissingSchema"));
    }

    [TestMethod]
    public void Compile_WhenAssemblyLoaderFails_ShouldPropagateLoadFailure()
    {
        var loader = new ThrowingAssemblyLoader();
        var unit = CreateUnit("loadfailure", ValidSource, new EmptyInterpreterReferenceProvider(), loader);

        var exception = Assert.Throws<InvalidOperationException>(() => unit.Compile());

        Assert.AreEqual("load failed", exception.Message);
        Assert.IsNotNull(unit.GetAssemblyBytes());
        Assert.IsNull(unit.CompiledAssembly);
        Assert.AreEqual(1, loader.LoadCallCount);
    }

    private static InterpreterCompilationUnit CreateUnit(
        string assemblyName,
        string sourceCode,
        IInterpreterReferenceProvider references,
        IAssemblyLoader loader)
    {
        return new InterpreterCompilationUnit(
            assemblyName,
            sourceCode,
            CreateCompilationFactory(),
            references,
            loader);
    }

    private static ICSharpCompilationFactory CreateCompilationFactory()
    {
        var cache = new DefaultMetadataReferenceCache();

        return new RoslynCompilationFactory(new RuntimeReferenceProvider(cache), cache);
    }

    private sealed class EmptyInterpreterReferenceProvider : IInterpreterReferenceProvider
    {
        public int CallCount { get; private set; }

        public IReadOnlyList<MetadataReference> GetReferences()
        {
            CallCount++;

            return [];
        }
    }

    private sealed class TrackingAssemblyLoader : IAssemblyLoader
    {
        public int LoadCallCount { get; private set; }

        public Assembly Load(byte[] assemblyBytes)
        {
            LoadCallCount++;

            return Assembly.Load(assemblyBytes);
        }
    }

    private sealed class ThrowingAssemblyLoader : IAssemblyLoader
    {
        public int LoadCallCount { get; private set; }

        public Assembly Load(byte[] assemblyBytes)
        {
            LoadCallCount++;

            throw new InvalidOperationException("load failed");
        }
    }

}
