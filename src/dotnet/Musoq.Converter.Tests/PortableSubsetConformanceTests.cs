using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Build;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator;
using Musoq.Plugins;
using Musoq.Targets.TestPortable;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class PortableSubsetConformanceTests
{
    private static readonly IReadOnlyDictionary<string, PortableValue> EmptyValues =
        new Dictionary<string, PortableValue>(StringComparer.Ordinal);

    private static readonly CompilationOptions PortableOptions = new(
        parallelizationMode: ParallelizationMode.None,
        useConstantFolding: false,
        forceTableResultMaterialization: true);

    [TestMethod]
    [DataRow("select 1 as Value from #system.dual() d", DisplayName = "literal projection")]
    [DataRow("select d.Dummy as Value from #system.dual() d where d.Dummy = 'single'", DisplayName = "source filter projection")]
    [DataRow("select null is null as IsNull, coalesce(null, 'fallback') as Fallback, case when null is null then 'yes' else 'no' end as Choice from #system.dual() d", DisplayName = "null coalesce case")]
    [DataRow("from values { { Name: 'b', Score: 2 }, { Name: 'a', Score: 1 } } rows select rows.Name, rows.Score order by rows.Score skip 0 take 1", DisplayName = "values ordering skip take")]
    public void PortableSubset_WhenQueryIsSupported_ShouldMatchCSharpClr(string query)
    {
        AssertConforms(query, EmptyValues, EmptyValues);
    }

    [TestMethod]
    [DataRow("param", DisplayName = "canonical parameter spelling")]
    [DataRow("params", DisplayName = "plural parameter spelling")]
    public void PortableSubset_WhenQueryUsesParameters_ShouldMatchCSharpClr(string keyword)
    {
        AssertConforms(
            $"{keyword}(p: int) select $p as Value from #system.dual() d",
            new Dictionary<string, PortableValue>(StringComparer.Ordinal)
            {
                ["p"] = PortableValue.FromSigned(7, 32)
            },
            EmptyValues);
    }

    [TestMethod]
    public void PortableSubset_WhenQueryUsesScriptVariables_ShouldMatchCSharpClr()
    {
        AssertConforms(
            "let x: int = 2; select $x as Value from #system.dual() d",
            EmptyValues,
            new Dictionary<string, PortableValue>(StringComparer.Ordinal)
            {
                ["x"] = PortableValue.FromSigned(2, 32)
            });
    }

    [TestMethod]
    public void PortableSubset_WhenQueryUsesRuntimeArithmeticAndComparison_ShouldMatchCSharpClr()
    {
        AssertConforms(
            "param(a: int, b: int) select $a + $b as Sum, $a > $b as Greater from #system.dual() d",
            new Dictionary<string, PortableValue>(StringComparer.Ordinal)
            {
                ["a"] = PortableValue.FromSigned(4, 32),
                ["b"] = PortableValue.FromSigned(2, 32)
            },
            EmptyValues);
    }

    [TestMethod]
    [DataRow(
        "param(divisor: int) select 10 / $divisor as Value from #system.dual() d",
        "divisor",
        0,
        typeof(DivideByZeroException),
        DisplayName = "division by zero")]
    public void PortableSubset_WhenRuntimeExpressionFails_ShouldMatchCSharpClrException(
        string query,
        string parameterName,
        int parameterValue,
        Type expectedExceptionType)
    {
        using var registration = RegisterPortableTarget();
        var portable = CompilePortable(query);
        var context = CreateContext(
            portable,
            new Dictionary<string, PortableValue>(StringComparer.Ordinal)
            {
                [parameterName] = PortableValue.FromSigned(parameterValue, 32)
            },
            EmptyValues);
        var csharp = CompileCSharp(query);
        csharp.Parameters[parameterName] = parameterValue;

        var portableException = CaptureException(() => PortableSubsetInterpreter.Execute(portable, context));
        var csharpException = CaptureException(() => csharp.Run());

        Assert.AreEqual(expectedExceptionType, portableException.GetBaseException().GetType());
        Assert.AreEqual(expectedExceptionType, csharpException.GetBaseException().GetType());
    }

    [TestMethod]
    public void PortableSubset_WhenRuntimeIntegerAdditionOverflows_ShouldMatchCSharpClrWrap()
    {
        AssertConforms(
            "param(value: int) select $value + 1 as Value from #system.dual() d",
            new Dictionary<string, PortableValue>(StringComparer.Ordinal)
            {
                ["value"] = PortableValue.FromSigned(int.MaxValue, 32)
            },
            EmptyValues);
    }

    [TestMethod]
    public void PortableSubset_WhenRequiredHostSourceBindingIsMissing_ShouldRejectBeforeExecution()
    {
        using var registration = RegisterPortableTarget();
        var portable = CompilePortable("select d.Dummy as Value from #system.dual() d");

        var exception = Assert.ThrowsExactly<InvalidOperationException>(
            () => PortableSubsetInterpreter.Execute(portable, PortableExecutionContext.Empty));

        StringAssert.Contains(exception.Message, "missing required source context");
    }

    [TestMethod]
    [DataRow(
        "select Count(1) as Count from #system.dual() d",
        "aggregate.",
        DisplayName = "aggregate")]
    [DataRow(
        "select a.Dummy from #system.dual() a inner join #system.dual() b on a.Dummy = b.Dummy",
        "hash.",
        DisplayName = "join")]
    [DataRow(
        "with sourceRows as (select d.Dummy from #system.dual() d) select s.Dummy, t.Dummy from sourceRows s inner join sourceRows t on s.Dummy = t.Dummy",
        "cte.",
        DisplayName = "cte")]
    [DataRow(
        "select RowNumber() over (order by d.Dummy) as Number from #system.dual() d",
        "window.",
        DisplayName = "window")]
    public void PortableSubset_WhenOperationFamilyIsUnsupported_ShouldRejectBeforeBackend(
        string query,
        string expectedOperationPrefix)
    {
        var backend = new RecordingPortableBackend();
        using var registration = RegisterPortableTarget(backend);

        var result = InstanceCreator.CompileTargetPackageWithDiagnostics(
            query,
            $"PortableUnsupported{Guid.NewGuid():N}",
            new SystemSchemaProvider(),
            new TestsLoggerResolver(),
            PortableSubsetTarget.TargetId,
            PortableOptions);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(0, backend.RenderCount, "Unsupported operations must be rejected before backend rendering.");
        var diagnostics = string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToDetailedString()));
        StringAssert.Contains(diagnostics, TargetDiagnosticCodes.UnsupportedOperation);
        StringAssert.Contains(diagnostics, expectedOperationPrefix);
    }

    [TestMethod]
    public void PortableSubset_WhenCallableIsOutsideSubset_ShouldReturnStructuredLoweringDiagnostic()
    {
        var backend = new RecordingPortableBackend();
        using var registration = RegisterPortableTarget(backend);

        var result = InstanceCreator.CompileTargetPackageWithDiagnostics(
            "select ToUpper(d.Dummy) as Value from #system.dual() d",
            $"PortableUnsupportedCallable{Guid.NewGuid():N}",
            new SystemSchemaProvider(),
            new TestsLoggerResolver(),
            PortableSubsetTarget.TargetId,
            PortableOptions);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(1, backend.RenderCount);
        var diagnostics = string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToDetailedString()));
        StringAssert.Contains(diagnostics, TargetDiagnosticCodes.UnsupportedLowering);
        StringAssert.Contains(diagnostics, nameof(LibraryBase.ToUpper));
    }

    [TestMethod]
    public void PortableSubset_WhenMethodCallIsWrappedInPostfixCast_ShouldKeepCallableUnsupported()
    {
        var backend = new RecordingPortableBackend();
        using var registration = RegisterPortableTarget(backend);

        var result = InstanceCreator.CompileTargetPackageWithDiagnostics(
            "select ToUpper(d.Dummy)::string as Value from #system.dual() d",
            $"PortableUnsupportedCastCallable{Guid.NewGuid():N}",
            new SystemSchemaProvider(),
            new TestsLoggerResolver(),
            PortableSubsetTarget.TargetId,
            PortableOptions);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(1, backend.RenderCount);
        var diagnostics = string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToDetailedString()));
        StringAssert.Contains(diagnostics, TargetDiagnosticCodes.UnsupportedLowering);
        StringAssert.Contains(diagnostics, nameof(LibraryBase.ToUpper));
    }

    private static void AssertConforms(
        string query,
        IReadOnlyDictionary<string, PortableValue> parameters,
        IReadOnlyDictionary<string, PortableValue> scriptVariables)
    {
        using var registration = RegisterPortableTarget();
        var portable = CompilePortable(query);
        var portableResult = PortableSubsetInterpreter.Execute(
            portable,
            CreateContext(portable, parameters, scriptVariables));
        using var csharp = CompileCSharp(query);
        foreach (var parameter in parameters)
            csharp.Parameters[parameter.Key] = ToClrParameter(parameter.Value);
        var csharpResult = csharp.Run();

        Assert.AreEqual(csharpResult.Rows.Count, portableResult.Rows.Count);
        for (var rowIndex = 0; rowIndex < csharpResult.Rows.Count; rowIndex++)
        {
            var csharpRow = csharpResult.Rows[rowIndex];
            var portableRow = portableResult.Rows[rowIndex];
            Assert.AreEqual(csharpRow.Count, portableRow.Fields.Count);
            for (var columnIndex = 0; columnIndex < csharpRow.Count; columnIndex++)
            {
                Assert.AreEqual(
                    NormalizeClr(csharpRow[columnIndex]),
                    NormalizePortable(portableRow.Fields[columnIndex].Value),
                    $"Row {rowIndex}, column {columnIndex} differs.");
            }
        }
    }

    private static PortableSubsetRenderedArtifact CompilePortable(string query)
    {
        var result = InstanceCreator.CompileTargetPackageWithDiagnostics(
            query,
            $"PortableSubset{Guid.NewGuid():N}",
            new SystemSchemaProvider(),
            new TestsLoggerResolver(),
            PortableSubsetTarget.TargetId,
            PortableOptions);
        Assert.IsTrue(
            result.Succeeded,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToDetailedString())));
        Assert.IsNotNull(result.BuildItems);
        Assert.IsNotNull(result.Package);
        Assert.IsNotInstanceOfType<ClrAssemblyExecutableArtifact>(result.BuildItems.ExecutableArtifact);
        var rendered = Assert.IsInstanceOfType<PortableSubsetRenderedArtifact>(result.BuildItems.RenderingArtifact);
        Assert.IsEmpty(result.Package.BinaryBlobs);
        Assert.HasCount(1, result.Package.SourceFiles);
        Assert.AreEqual(rendered.Program.CreateManifest(), result.Package.SourceFiles[0].Content);
        return rendered;
    }

    private static CompiledQuery CompileCSharp(string query) => InstanceCreator.CompileForExecution(
        query,
        $"CSharpConformance{Guid.NewGuid():N}",
        new SystemSchemaProvider(),
        new TestsLoggerResolver(),
        PortableOptions);

    private static PortableExecutionContext CreateContext(
        PortableSubsetRenderedArtifact artifact,
        IReadOnlyDictionary<string, PortableValue> parameters,
        IReadOnlyDictionary<string, PortableValue> scriptVariables)
    {
        var sources = artifact.HostAbiInventory.Imports
            .Where(static import => import.Kind == TargetHostAbiImportKind.SourceAccess)
            .Select(static import => Assert.IsInstanceOfType<TargetSourceAccessAbiDetails>(import.Details))
            .ToDictionary(
                static details => details.SourceContextId,
                static _ => (IReadOnlyList<PortableRow>)
                [
                    PortableRow.Create(("Dummy", PortableValue.FromString("single")))
                ],
                StringComparer.Ordinal);
        return new PortableExecutionContext(parameters, scriptVariables, sources);
    }

    private static IDisposable RegisterPortableTarget(IQueryExecutionBackend? backend = null)
    {
        var descriptor = ExecutionTargetDescriptor.Create(
            PortableSubsetTarget.TargetId,
            renderPhase: backend ?? new PortableSubsetExecutionBackend(),
            finalizationPhase: new PortableSubsetRenderedQueryFinalizer(),
            inspectionPhase: new PortableSubsetRenderedQueryInspector(),
            createRenderInputs: static _ => new EmptyTargetBackendRenderInputs(PortableSubsetTarget.TargetId),
            createArtifactPackage: static context => TargetArtifactPackage.CreatePortableExportPackage(
                context.TargetId,
                "PortableSubsetProgram",
                Assert.IsInstanceOfType<TargetExportArtifact>(context.ExecutableArtifact),
                context.SemanticsContract,
                executionIrVersion: context.ExecutionIrVersion));
        return ExecutionTargetCatalog.UseTemporaryDescriptor(descriptor);
    }

    private static object ToClrParameter(PortableValue value) => value.Kind switch
    {
        PortableValueKind.SignedInteger when value.BitWidth == 32 => checked((int)value.SignedInteger),
        PortableValueKind.String => value.Text,
        PortableValueKind.Boolean => value.Boolean,
        _ => throw new NotSupportedException($"Test parameter kind '{value.Kind}' is not supported.")
    };

    private static string NormalizePortable(PortableValue value) => value.Kind switch
    {
        PortableValueKind.Null => "null",
        PortableValueKind.Boolean => $"bool:{value.Boolean}",
        PortableValueKind.SignedInteger => $"i{value.BitWidth}:{value.SignedInteger}",
        PortableValueKind.UnsignedInteger => $"u{value.BitWidth}:{value.UnsignedInteger}",
        PortableValueKind.FloatingPoint => $"f64:{value.FloatingPointBits:X16}",
        PortableValueKind.Decimal => $"decimal:{string.Join(",", value.DecimalBits)}",
        PortableValueKind.String => $"string:{value.Text}",
        _ => throw new ArgumentOutOfRangeException()
    };

    private static string NormalizeClr(object? value) => value switch
    {
        null => "null",
        bool boolean => $"bool:{boolean}",
        sbyte number => $"i8:{number}",
        short number => $"i16:{number}",
        int number => $"i32:{number}",
        long number => $"i64:{number}",
        byte number => $"u8:{number}",
        ushort number => $"u16:{number}",
        uint number => $"u32:{number}",
        ulong number => $"u64:{number}",
        float number => $"f64:{unchecked((ulong)BitConverter.DoubleToInt64Bits(number)):X16}",
        double number => $"f64:{unchecked((ulong)BitConverter.DoubleToInt64Bits(number)):X16}",
        decimal number => $"decimal:{string.Join(",", decimal.GetBits(number))}",
        string text => $"string:{text}",
        _ => throw new NotSupportedException($"CSharp comparison value '{value.GetType().Name}' is not supported.")
    };

    private static Exception CaptureException(Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            return exception;
        }

        Assert.Fail("Expected execution to throw.");
        throw new InvalidOperationException();
    }

    private sealed class RecordingPortableBackend : IQueryExecutionBackend
    {
        private readonly PortableSubsetExecutionBackend _inner = new();

        public ExecutionTargetId TargetId => PortableSubsetTarget.TargetId;

        public ExecutionTargetCapabilities Capabilities => _inner.Capabilities;

        public int RenderCount { get; private set; }

        public TargetRenderResult Render(TargetRenderRequest request)
        {
            RenderCount++;
            return _inner.Render(request);
        }
    }
}
