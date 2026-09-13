using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Benchmarks.Components;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Execution;
using Musoq.Examples.DataSources.StructuredInputs;
using Musoq.Schema;

namespace Musoq.Benchmarks.Tests;

/// <summary>
/// Stable correctness and shape gates for the structural preparation
/// benchmark.  Timing remains BenchmarkDotNet evidence; these tests reject
/// semantic drift, generated object packing, and boxing in the hot methods.
/// </summary>
[TestClass]
public sealed class StructuredInputPerformanceQualificationTests
{
    private static readonly OpCode[] OneByteOpCodes = new OpCode[0x100];
    private static readonly OpCode[] TwoByteOpCodes = new OpCode[0x100];

    static StructuredInputPerformanceQualificationTests()
    {
        foreach (var field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is not OpCode opCode)
                continue;

            var value = unchecked((ushort)opCode.Value);
            if (value < 0x100)
                OneByteOpCodes[value] = opCode;
            else if ((value & 0xff00) == 0xfe00)
                TwoByteOpCodes[value & 0xff] = opCode;
        }
    }

    [TestMethod]
    public void BenchmarkFixture_WhenRunAtRepresentativeSize_ShouldKeepTypedAndStructuralResultsEquivalent()
    {
        var benchmark = new StructuredInputPreparationBenchmark { Size = 32 };
        benchmark.Setup();
        try
        {
            Assert.AreEqual(
                benchmark.TypedRecordConstruction(),
                benchmark.StructuralRecordConstruction());
            Assert.AreEqual(
                benchmark.TypedNumericStructConstruction(),
                benchmark.StructuralNumericStructConstruction());
            Assert.AreEqual(
                benchmark.TypedNestedConstruction(),
                benchmark.StructuralNestedConstruction());
            Assert.AreEqual(
                benchmark.TypedNullableConstruction(),
                benchmark.StructuralNullableConstruction());

            var expectedRows = benchmark.InlineSourceInvocation();
            Assert.AreEqual(expectedRows, benchmark.RetainedLetSourceInvocation());
            Assert.AreEqual(expectedRows, benchmark.HostNormalizationAndSourceInvocation());
            Assert.AreEqual(expectedRows, benchmark.CteProductionAndAdaptation());
            Assert.AreEqual(expectedRows, benchmark.ResultEnumerationAfterPreparation());
            Assert.AreEqual(0, benchmark.StructuralEmptyConstruction());
        }
        finally
        {
            benchmark.Cleanup();
        }
    }

    [TestMethod]
    public void EmptyStructuralCollection_ShouldReuseTypedRuntimeSingleton()
    {
        var value = Array.Empty<int>();

        Assert.AreSame(Array.Empty<int>(), value);
        Assert.IsEmpty(value);
    }

    [TestMethod]
    public void GeneratedPreparationShapes_ShouldUseTypedConstructionWithoutObjectPacks()
    {
        var queries = new (string Query, ISchemaProvider Provider)[]
        {
            (StructuredInputPreparationBenchmark.BuildInlineQuery(3), new StructuredInputsSchemaProvider()),
            (StructuredInputPreparationBenchmark.BuildLetQuery(3), new StructuredInputsSchemaProvider()),
            (StructuredInputPreparationBenchmark.BuildHostQuery(), new StructuredInputsSchemaProvider()),
            (StructuredInputPreparationBenchmark.BuildCteQuery(3), new StructuredInputsSchemaProvider()),
            (StructuredInputPreparationBenchmark.BuildWeightedQuery(3), new StructuredInputsSchemaProvider()),
            (StructuredInputPreparationBenchmark.BuildNestedQuery(3), new BenchmarkStructuredInputSchemaProvider()),
            (StructuredInputPreparationBenchmark.BuildNullableQuery(3), new BenchmarkStructuredInputSchemaProvider()),
            (StructuredInputPreparationBenchmark.BuildEmptyQuery(), new StructuredInputsSchemaProvider())
        };

        foreach (var (query, provider) in queries)
        {
            var inspection = InstanceCreator.CompileForInspection(
                query,
                $"structured-performance-shape-{Guid.NewGuid():N}",
                provider,
                new BenchmarkLoggerResolver(),
                new CompilationOptions(ParallelizationMode.None).WithTableResultMaterialization());
            var generated = inspection.GeneratedCSharpCode;

            Assert.DoesNotContain("GetRawConstructors", generated, query);
            Assert.DoesNotContain("new object[]", generated, query);
            Assert.DoesNotContain("DynamicInvoke", generated, query);
            Assert.DoesNotContain("MethodInfo.Invoke", generated, query);
            Assert.DoesNotContain("Activator.CreateInstance", generated, query);
            Assert.DoesNotContain("Array.GetValue", generated, query);
            Assert.DoesNotContain("Array.SetValue", generated, query);
            var runMarker = generated.IndexOf("public Table Run", StringComparison.Ordinal);
            Assert.IsGreaterThanOrEqualTo(0, runMarker, "The generated artifact did not expose an execution entry point.");
            var execution = generated[runMarker..];
            // StructuralValue is allowed in the public metadata contract and
            // in the one host-boundary representation.  It must not be used
            // by generated execution after the entry point.
            Assert.DoesNotContain("StructuralValue.FromRecord", execution, query);
            Assert.DoesNotContain("StructuralValue.FromCollection", execution, query);
            // Host preflight is intentionally the one boundary where an
            // owned object dictionary is assembled.  The generated execution
            // body must consume typed carrier slots after that boundary.
            var dictionaryText = "new Dictionary<string, object";
            var firstDictionary = generated.IndexOf(dictionaryText, StringComparison.Ordinal);
            if (generated.Contains("CaptureParameterSnapshot", StringComparison.Ordinal))
            {
                Assert.IsTrue(firstDictionary >= 0, "Structured host queries must expose an explicit capture boundary.");
                Assert.AreEqual(
                    firstDictionary,
                    generated.LastIndexOf(dictionaryText, StringComparison.Ordinal),
                    "Only one host-boundary parameter dictionary may be generated.");
            }
            else
            {
                Assert.IsLessThan(0, firstDictionary, "Non-parameter structural queries must not allocate a host dictionary.");
            }
        }
    }

    [TestMethod]
    public void GeneratedHotMethods_ShouldContainNoBoxInstructions()
    {
        using var letProbe = CompileProbe(
            StructuredInputPreparationBenchmark.BuildLetQuery(3),
            "structured-performance-let-il",
            new StructuredInputsSchemaProvider());
        using var cteProbe = CompileProbe(
            StructuredInputPreparationBenchmark.BuildCteQuery(3),
            "structured-performance-cte-il",
            new StructuredInputsSchemaProvider());

        var methods = GetReachableStructuralHotMethods(letProbe.Query, letProbe.Plan)
            .Concat(GetReachableStructuralHotMethods(cteProbe.Query, cteProbe.Plan))
            .Distinct()
            .ToArray();
        Assert.IsNotEmpty(methods, "The structural performance probe found no hot methods.");

        var boxed = methods
            .Where(ContainsBoxOpcode)
            .Select(static method => $"{method.DeclaringType?.FullName}.{method.Name}")
            .ToArray();

        Assert.IsEmpty(
            boxed,
            $"Structural preparation hot methods must not emit IL box instructions: {string.Join(", ", boxed)}");

        var forbiddenCalls = methods
            .SelectMany(FindForbiddenCalls)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        Assert.IsEmpty(
            forbiddenCalls,
            "Structural preparation hot methods and approved runtime helpers must not call boxing, reflection, delegate, or reflective-array paths: " +
            string.Join(", ", forbiddenCalls));
    }

    [TestMethod]
    public void BenchmarkStructuralCohorts_ShouldUseGeneratedEntryPoints()
    {
        var sourcePath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "Musoq.Benchmarks",
            "StructuredInputPreparationBenchmark.cs");
        Assert.IsTrue(File.Exists(sourcePath), $"Benchmark source was not found at '{sourcePath}'.");
        var source = File.ReadAllText(sourcePath);

        Assert.DoesNotContain("StructuralValue.From", source);
        Assert.DoesNotContain("ConvertPatternValues", source);
        Assert.DoesNotContain("ConvertWeightedValues", source);
        Assert.DoesNotContain("ConvertNestedValues", source);
        Assert.DoesNotContain("ConvertNullableValues", source);

        foreach (var methodName in new[]
                 {
                     nameof(StructuredInputPreparationBenchmark.StructuralRecordConstruction),
                     nameof(StructuredInputPreparationBenchmark.StructuralNumericStructConstruction),
                     nameof(StructuredInputPreparationBenchmark.StructuralNestedConstruction),
                     nameof(StructuredInputPreparationBenchmark.StructuralNullableConstruction),
                     nameof(StructuredInputPreparationBenchmark.StructuralEmptyConstruction)
                 })
        {
            StringAssert.Contains(source, methodName);
            Assert.DoesNotContain(source, $"StructuralValue {methodName}");
        }
    }

    [TestMethod]
    public void AllocationQualification_ShouldMatchIndependentTypedBaselines()
    {
        foreach (var size in new[] { 1, 3, 32, 1_024 })
        {
            var benchmark = new StructuredInputPreparationBenchmark { Size = size };
            benchmark.Setup();
            try
            {
                AssertGeneratedIntCohort(
                    benchmark.TypedRecordConstruction,
                    benchmark.StructuralRecordConstruction,
                    size,
                    "record");
                AssertGeneratedDecimalCohort(
                    benchmark.TypedNumericStructConstruction,
                    benchmark.StructuralNumericStructConstruction,
                    size,
                    "numeric struct");
                AssertGeneratedIntCohort(
                    benchmark.TypedNestedConstruction,
                    benchmark.StructuralNestedConstruction,
                    size,
                    "nested");
                AssertGeneratedIntCohort(
                    benchmark.TypedNullableConstruction,
                    benchmark.StructuralNullableConstruction,
                    size,
                    "nullable");

                var typedResult = MeasureAllocations(benchmark.ResultEnumerationAfterPreparation);
                var generatedResult = MeasureAllocations(benchmark.InlineSourceInvocation);
                Assert.AreEqual(
                    typedResult.Checksum,
                    generatedResult.Checksum,
                    "The generated result cohort must agree with the independently typed result cohort.");
                Assert.IsGreaterThan(
                    0L,
                    generatedResult.BytesPerOperation,
                    "The result allocation cohort must be measured separately from conversion.");
            }
            finally
            {
                benchmark.Cleanup();
            }
        }
    }

    [TestMethod]
    public void BenchmarkSizes_ShouldIncludeTheOneNodeBelowBudgetShape()
    {
        var nodeCount = StructuredInputPreparationBenchmark.PatternNodeCount(
            StructuredInputPreparationBenchmark.NearLimitSize);
        Assert.IsLessThanOrEqualTo(
            StructuredInputPreparationBenchmark.MaximumStructuralNodes - 1,
            nodeCount);
        Assert.IsLessThanOrEqualTo(
            StructuredInputPreparationBenchmark.MaximumStructuralNodes - nodeCount,
            StructuredInputPreparationBenchmark.PatternRecordNodeCount,
            "The largest representable record cohort must sit immediately below the limit stride.");
        CollectionAssert.AreEquivalent(
            new[] { 1, 3, 32, 1_024, StructuredInputPreparationBenchmark.NearLimitSize },
            StructuredInputPreparationBenchmark.QualificationSizes.ToArray());
    }

    [TestMethod]
    public void BenchmarkInventory_ShouldExposeEveryRequiredPreparationPhase()
    {
        var names = typeof(StructuredInputPreparationBenchmark)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(static method => method.GetCustomAttribute<BenchmarkDotNet.Attributes.BenchmarkAttribute>() != null)
            .Select(static method => method.Name)
            .ToHashSet(StringComparer.Ordinal);

        var required = new[]
        {
            nameof(StructuredInputPreparationBenchmark.ParseBindAndMetadata),
            nameof(StructuredInputPreparationBenchmark.TypedRecordConstruction),
            nameof(StructuredInputPreparationBenchmark.StructuralRecordConstruction),
            nameof(StructuredInputPreparationBenchmark.TypedNumericStructConstruction),
            nameof(StructuredInputPreparationBenchmark.StructuralNumericStructConstruction),
            nameof(StructuredInputPreparationBenchmark.TypedNestedConstruction),
            nameof(StructuredInputPreparationBenchmark.StructuralNestedConstruction),
            nameof(StructuredInputPreparationBenchmark.TypedNullableConstruction),
            nameof(StructuredInputPreparationBenchmark.StructuralNullableConstruction),
            nameof(StructuredInputPreparationBenchmark.StructuralEmptyConstruction),
            nameof(StructuredInputPreparationBenchmark.InlineSourceInvocation),
            nameof(StructuredInputPreparationBenchmark.RetainedLetSourceInvocation),
            nameof(StructuredInputPreparationBenchmark.HostNormalizationAndSourceInvocation),
            nameof(StructuredInputPreparationBenchmark.CteProductionAndAdaptation),
            nameof(StructuredInputPreparationBenchmark.ResultEnumerationAfterPreparation),
            nameof(StructuredInputPreparationBenchmark.ScalarSourceInvocation),
            nameof(StructuredInputPreparationBenchmark.ValuesSourceInvocation)
        };

        CollectionAssert.IsSubsetOf(required, names.ToArray());
    }

    private static void AssertGeneratedIntCohort(
        Func<int> typed,
        Func<int> generated,
        int size,
        string cohort)
    {
        var typedMeasurement = MeasureAllocations(typed);
        var generatedMeasurement = MeasureAllocations(generated);

        Assert.AreEqual(
            typedMeasurement.Checksum,
            generatedMeasurement.Checksum,
            $"Typed and generated {cohort} cohorts diverged at size {size}.");
        Assert.IsGreaterThan(
            0L,
            generatedMeasurement.TotalBytes,
            $"Generated {cohort} cohort did not execute a real compiled query at size {size}.");
    }

    private static void AssertGeneratedDecimalCohort(
        Func<decimal> typed,
        Func<decimal> generated,
        int size,
        string cohort)
    {
        var typedMeasurement = MeasureDecimalAllocations(typed);
        var generatedMeasurement = MeasureDecimalAllocations(generated);
        Assert.AreEqual(
            typedMeasurement.Checksum,
            generatedMeasurement.Checksum,
            $"Typed and generated {cohort} cohorts diverged at size {size}.");
        Assert.IsGreaterThan(
            0L,
            generatedMeasurement.TotalBytes,
            $"Generated {cohort} cohort did not execute a real compiled query at size {size}.");
    }

    private static AllocationMeasurement MeasureAllocations(Func<int> operation)
    {
        const int warmupIterations = 8;
        const int measuredIterations = 32;
        var checksum = 17;
        for (var index = 0; index < warmupIterations; index++)
            checksum = unchecked((checksum * 31) + operation());

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < measuredIterations; index++)
            checksum = unchecked((checksum * 31) + operation());
        var after = GC.GetAllocatedBytesForCurrentThread();
        GC.KeepAlive(checksum);

        return new AllocationMeasurement(
            after - before,
            (after - before) / measuredIterations,
            checksum);
    }

    private static DecimalAllocationMeasurement MeasureDecimalAllocations(Func<decimal> operation)
    {
        const int warmupIterations = 8;
        const int measuredIterations = 32;
        var checksum = 0m;
        for (var index = 0; index < warmupIterations; index++)
            checksum += operation();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < measuredIterations; index++)
            checksum += operation();
        var after = GC.GetAllocatedBytesForCurrentThread();
        GC.KeepAlive(checksum);

        return new DecimalAllocationMeasurement(
            after - before,
            (after - before) / measuredIterations,
            checksum);
    }

    private readonly record struct AllocationMeasurement(
        long TotalBytes,
        long BytesPerOperation,
        int Checksum);

    private readonly record struct DecimalAllocationMeasurement(
        long TotalBytes,
        long BytesPerOperation,
        decimal Checksum);

    private static CompiledQuery Compile(
        string query,
        string name,
        ISchemaProvider? schemaProvider = null)
    {
        var result = InstanceCreator.CompileWithDiagnostics(
            query,
            name,
            schemaProvider ?? new StructuredInputsSchemaProvider(),
            new BenchmarkLoggerResolver(),
            new CompilationOptions(ParallelizationMode.None).WithTableResultMaterialization());
        Assert.IsTrue(
            result.Succeeded,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToDetailedString())));
        return result.CompiledQuery!;
    }

    private static StructuralProbe CompileProbe(
        string query,
        string name,
        ISchemaProvider schemaProvider)
    {
        var inspection = InstanceCreator.CompileForInspection(
            query,
            $"{name}-inspection",
            schemaProvider,
            new BenchmarkLoggerResolver(),
            new CompilationOptions(ParallelizationMode.None).WithTableResultMaterialization());
        Assert.IsNotNull(inspection.ExecutionPlan, "The structural probe did not produce an Execution IR plan.");
        return new StructuralProbe(Compile(query, name, schemaProvider), inspection.ExecutionPlan!);
    }

    private sealed record StructuralProbe(CompiledQuery Query, ExecutionPlan Plan) : IDisposable
    {
        public void Dispose() => Query.Dispose();
    }

    private static IEnumerable<MethodInfo> GetGeneratedMethods(CompiledQuery query)
    {
        var runnableField = typeof(CompiledQuery).GetField("_runnable", BindingFlags.Instance | BindingFlags.NonPublic) ??
                            throw new AssertFailedException("Compiled query runnable field was not found.");
        var current = runnableField.GetValue(query) ??
                      throw new AssertFailedException("Compiled query runnable was not initialized.");

        while (FindProperty(current.GetType(), "Inner")?.GetValue(current) is { } inner)
            current = inner;

        return GetMethods(current.GetType());
    }

    private static IReadOnlyList<MethodInfo> GetReachableStructuralHotMethods(
        CompiledQuery query,
        ExecutionPlan plan)
    {
        var methods = GetGeneratedMethods(query).ToArray();
        var generatedAssembly = methods.FirstOrDefault()?.DeclaringType?.Assembly;
        Assert.IsNotNull(generatedAssembly, "The compiled structural query did not expose a generated assembly.");

        var expectedConstructors = CollectConstructorStableIds(plan);
        var roots = methods
            .Where(method => GetCalledMethods(method)
                .OfType<ConstructorInfo>()
                .Select(CreateConstructorStableId)
                .Any(expectedConstructors.Contains))
            .ToArray();
        if (roots.Length == 0)
            throw new AssertFailedException(
                "The Execution IR exposed no generated method that invokes one of its structural constructors. " +
                "The IL qualification would otherwise be disconnected from the planned callables.");

        var visited = new HashSet<MethodBase>();
        var pending = new Queue<MethodInfo>(roots);
        while (pending.Count > 0)
        {
            var method = pending.Dequeue();
            if (!visited.Add(method))
                continue;

            foreach (var target in GetCalledMethods(method))
            {
                if (target is MethodInfo targetMethod &&
                    (targetMethod.DeclaringType?.Assembly == generatedAssembly ||
                     IsApprovedRuntimeHelper(targetMethod)) &&
                    !visited.Contains(targetMethod))
                {
                    pending.Enqueue(targetMethod);
                }
            }
        }

        return visited.OfType<MethodInfo>().ToArray();
    }

    private static IReadOnlySet<string> CollectConstructorStableIds(ExecutionPlan plan)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        VisitPlanObject(plan, result, visited);
        return result;
    }

    private static void VisitPlanObject(
        object? value,
        ISet<string> constructorIds,
        ISet<object> visited)
    {
        if (value == null || value is string || value is Type || value is Delegate)
            return;

        if (value is ExecutionCallableRef callable)
        {
            if (string.Equals(callable.MethodName, ".ctor", StringComparison.Ordinal))
                constructorIds.Add(callable.StableId);
            return;
        }

        var valueType = value.GetType();
        if (valueType.IsPrimitive || valueType.IsEnum)
            return;
        if (!valueType.IsValueType && !visited.Add(value))
            return;

        if (value is IEnumerable sequence)
        {
            foreach (var item in sequence)
                VisitPlanObject(item, constructorIds, visited);
            return;
        }

        foreach (var property in valueType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.GetIndexParameters().Length != 0 || property.GetMethod == null)
                continue;

            object? child;
            try
            {
                child = property.GetValue(value);
            }
            catch (Exception)
            {
                continue;
            }

            VisitPlanObject(child, constructorIds, visited);
        }
    }

    private static bool IsApprovedRuntimeHelper(MethodInfo method)
    {
        var declaringType = method.DeclaringType;
        var assembly = declaringType?.Assembly;
        var @namespace = declaringType?.Namespace;
        return assembly == typeof(StructuralInputLimitRuntime).Assembly &&
               (@namespace?.StartsWith("Musoq.Evaluator.Helpers", StringComparison.Ordinal) == true ||
                @namespace?.StartsWith("Musoq.Evaluator.Runtime", StringComparison.Ordinal) == true);
    }

    private static string CreateConstructorStableId(ConstructorInfo constructor)
    {
        var declaringType = constructor.DeclaringType ??
                            throw new InvalidOperationException("A constructor must have a declaring type.");
        return $"constructor:{CreateTypeStableId(declaringType)}(" +
               string.Join(",", constructor.GetParameters().Select(static parameter => CreateTypeStableId(parameter.ParameterType))) +
               ")";
    }

    private static string CreateTypeStableId(Type type)
    {
        if (type == typeof(bool)) return "primitive:bool";
        if (type == typeof(byte)) return "primitive:uint8";
        if (type == typeof(sbyte)) return "primitive:int8";
        if (type == typeof(short)) return "primitive:int16";
        if (type == typeof(ushort)) return "primitive:uint16";
        if (type == typeof(int)) return "primitive:int32";
        if (type == typeof(uint)) return "primitive:uint32";
        if (type == typeof(long)) return "primitive:int64";
        if (type == typeof(ulong)) return "primitive:uint64";
        if (type == typeof(float)) return "primitive:float32";
        if (type == typeof(double)) return "primitive:float64";
        if (type == typeof(decimal)) return "primitive:decimal";
        if (type == typeof(char)) return "primitive:char";
        if (type == typeof(string)) return "primitive:string";
        if (type == typeof(DateTime)) return "primitive:datetime";
        if (type == typeof(DateTimeOffset)) return "primitive:datetimeoffset";
        if (type == typeof(Guid)) return "primitive:guid";
        if (type == typeof(TimeSpan)) return "primitive:timespan";
        if (type == typeof(object)) return "host-opaque:dynamic-object";

        var nullable = Nullable.GetUnderlyingType(type);
        if (nullable != null)
            return $"nullable<{CreateTypeStableId(nullable)}>";

        if (type.IsArray)
            return $"array:{type.GetArrayRank()}<{CreateTypeStableId(type.GetElementType()!)}>";

        if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();
            var prefix = definition == typeof(IEnumerable<>) ||
                         definition == typeof(IReadOnlyCollection<>) ||
                         definition == typeof(IReadOnlyList<>)
                ? "sequence"
                : definition == typeof(ICollection<>) ||
                  definition == typeof(IList<>) ||
                  definition == typeof(List<>)
                    ? "list"
                    : definition == typeof(IReadOnlyDictionary<,>) ||
                      definition == typeof(IDictionary<,>) ||
                      definition == typeof(Dictionary<,>)
                        ? "map"
                        : $"clr:{CreateVersionFreeTypeIdentity(definition)}";
            return $"{prefix}<{string.Join(",", type.GetGenericArguments().Select(CreateTypeStableId))}>";
        }

        return $"clr:{CreateVersionFreeTypeIdentity(type)}";
    }

    private static string CreateVersionFreeTypeIdentity(Type type) =>
        $"{type.FullName ?? type.Name}@{type.Assembly.GetName().Name ?? "<unknown>"}";

    private static IEnumerable<string> FindForbiddenCalls(MethodInfo method)
    {
        var structuralHelper = method.Name.Contains("Prepare", StringComparison.Ordinal) ||
                               method.Name.Contains("Structural", StringComparison.Ordinal);
        foreach (var target in GetCalledMethods(method))
        {
            var declaringType = target.DeclaringType;
            if (declaringType == typeof(Array) &&
                (target.Name.Equals("GetValue", StringComparison.Ordinal) ||
                 target.Name.Equals("SetValue", StringComparison.Ordinal)))
            {
                yield return $"{method.Name} -> {declaringType.FullName}.{target.Name}";
            }

            if (structuralHelper &&
                target.Name.Equals("Invoke", StringComparison.Ordinal) &&
                declaringType != null &&
                typeof(Delegate).IsAssignableFrom(declaringType))
            {
                yield return $"{method.Name} -> {declaringType.FullName}.{target.Name}";
            }

            if (structuralHelper &&
                target is ConstructorInfo &&
                declaringType != null &&
                typeof(Delegate).IsAssignableFrom(declaringType))
            {
                yield return $"{method.Name} -> {declaringType.FullName} delegate construction";
            }

            if (structuralHelper && target.Name.Equals("GetEnumerator", StringComparison.Ordinal))
                yield return $"{method.Name} -> {declaringType?.FullName}.{target.Name}";

            if (declaringType == typeof(Activator) ||
                (declaringType?.Namespace?.StartsWith("System.Reflection", StringComparison.Ordinal) ?? false))
            {
                yield return $"{method.Name} -> {declaringType?.FullName}.{target.Name}";
            }

            if (structuralHelper && IsDictionaryMember(target))
                yield return $"{method.Name} -> {declaringType?.FullName}.{target.Name}";
        }
    }

    private static bool IsDictionaryMember(MethodBase method)
    {
        var declaringType = method.DeclaringType;
        return declaringType != null &&
               (typeof(System.Collections.IDictionary).IsAssignableFrom(declaringType) ||
                (declaringType.IsGenericType &&
                 declaringType.GetGenericTypeDefinition() == typeof(Dictionary<,>)));
    }

    private static IEnumerable<MethodBase> GetCalledMethods(MethodInfo method)
    {
        var il = method.GetMethodBody()?.GetILAsByteArray();
        if (il == null || il.Length == 0)
            yield break;

        var genericTypeArguments = method.DeclaringType?.IsGenericType == true
            ? method.DeclaringType.GetGenericArguments()
            : null;
        var genericMethodArguments = method.IsGenericMethod
            ? method.GetGenericArguments()
            : null;

        for (var index = 0; index < il.Length;)
        {
            var opCode = ReadOpCode(il, ref index);
            var operandIndex = index;
            int operandSize;
            try
            {
                operandSize = GetOperandSize(opCode.OperandType, il, operandIndex);
            }
            catch (ArgumentOutOfRangeException)
            {
                yield break;
            }

            if (opCode.OperandType == OperandType.InlineMethod && operandSize == 4)
            {
                var token = BitConverter.ToInt32(il, operandIndex);
                MethodBase? target = null;
                try
                {
                    target = method.Module.ResolveMethod(token, genericTypeArguments, genericMethodArguments);
                }
                catch (ArgumentException)
                {
                    // Some runtime-generated methods expose an unresolved
                    // token. The direct IL checks still cover their body.
                }

                if (target != null)
                    yield return target;
            }

            index = operandIndex + operandSize;
        }
    }

    private static IEnumerable<MethodInfo> GetMethods(Type type)
    {
        var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (var method in type.GetMethods(flags))
            if (method.GetMethodBody() != null)
                yield return method;

        foreach (var nested in type.GetNestedTypes(flags))
        foreach (var method in GetMethods(nested))
            yield return method;
    }

    private static PropertyInfo? FindProperty(Type type, string name)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            var property = current.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null)
                return property;
        }

        return null;
    }

    private static bool ContainsBoxOpcode(MethodInfo method)
    {
        var il = method.GetMethodBody()?.GetILAsByteArray();
        if (il == null || il.Length == 0)
            return false;

        for (var index = 0; index < il.Length;)
        {
            var opCode = ReadOpCode(il, ref index);
            if (opCode == OpCodes.Box)
                return true;
            index += GetOperandSize(opCode.OperandType, il, index);
        }

        return false;
    }

    private static OpCode ReadOpCode(byte[] il, ref int index)
    {
        var value = il[index++];
        return value != 0xfe
            ? OneByteOpCodes[value]
            : TwoByteOpCodes[il[index++]];
    }

    private static int GetOperandSize(OperandType operandType, byte[] il, int operandIndex)
    {
        return operandType switch
        {
            OperandType.InlineNone => 0,
            OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
            OperandType.InlineVar => 2,
            OperandType.InlineBrTarget or OperandType.InlineField or OperandType.InlineI or OperandType.InlineMethod
                or OperandType.InlineSig or OperandType.InlineString or OperandType.InlineTok or OperandType.InlineType
                => 4,
            OperandType.InlineI8 or OperandType.InlineR => 8,
            OperandType.ShortInlineR => 4,
            OperandType.InlineSwitch => 4 + BitConverter.ToInt32(il, operandIndex) * 4,
            _ => throw new ArgumentOutOfRangeException(nameof(operandType), operandType, null)
        };
    }
}
