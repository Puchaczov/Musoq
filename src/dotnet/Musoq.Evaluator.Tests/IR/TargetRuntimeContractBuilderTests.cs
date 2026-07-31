using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Targets.Execution.Analysis;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class TargetRuntimeContractBuilderTests
{
    [TestMethod]
    public void Build_WhenPlanScansSource_ShouldDescribeSourceAccessDiagnosticsAndProfiling()
    {
        var shape = new SourceEntityShape(
            "s",
            typeof(SampleEntity),
            [CreateField("Name", typeof(string), FieldNullability.NotNullable)]);
        var binding = new ExecutionSourceBinding(
            "test",
            "rows",
            "s:1",
            0,
            [],
            shape.Fields,
            SourceType: ExecutionClrBindingFactory.FromClr(typeof(SampleEntity)));
        var plan = new ExecutionPlan(
            "Q_Source",
            [shape],
            new ExecutionBlock(
            [
                new ExecutionSourceScan(
                    new ExecutionVariable("s", typeof(SampleEntity)),
                    new ExecutionVariable("rows", typeof(object)),
                    binding)
            ]));

        var contract = Build(plan);

        Assert.AreEqual("Q_Source", contract.PlanIdentifier);
        Assert.AreEqual(1, contract.SourceAccess.Count);
        Assert.AreEqual("schema-source", contract.SourceAccess[0].Kind);
        Assert.AreEqual("s:1", contract.SourceAccess[0].SourceContextId);
        Assert.IsTrue(contract.Diagnostics.RequiresSourceDiagnostics);
        Assert.IsTrue(contract.Profiling.SupportsSourceBoundaryProfiling);
        Assert.AreEqual(1, contract.Profiling.SourceBoundaryCount);
    }

    [TestMethod]
    public void HostAbiInventoryBuilder_WhenPlanScansSource_ShouldDescribeRuntimeImports()
    {
        var shape = new SourceEntityShape(
            "s",
            typeof(SampleEntity),
            [CreateField("Name", typeof(string), FieldNullability.NotNullable)]);
        var binding = new ExecutionSourceBinding(
            "test",
            "rows",
            "s:1",
            0,
            [],
            shape.Fields,
            SourceType: ExecutionClrBindingFactory.FromClr(typeof(SampleEntity)));
        var plan = new ExecutionPlan(
            "Q_SourceAbi",
            [shape],
            new ExecutionBlock(
            [
                new ExecutionSourceScan(
                    new ExecutionVariable("s", typeof(SampleEntity)),
                    new ExecutionVariable("rows", typeof(object)),
                    binding)
            ]));

        var inventory = TargetHostAbiInventoryBuilder.Build(Build(plan));

        Assert.IsTrue(inventory.Requires(TargetHostAbiImportKind.SourceAccess));
        Assert.IsTrue(inventory.Requires(TargetHostAbiImportKind.RowShapeTransfer));
        Assert.IsTrue(inventory.Requires(TargetHostAbiImportKind.NullTypeCoercion));
        Assert.IsTrue(inventory.Requires(TargetHostAbiImportKind.Cancellation));
        Assert.IsTrue(inventory.Requires(TargetHostAbiImportKind.Diagnostics));
        Assert.IsTrue(inventory.Requires(TargetHostAbiImportKind.Profiling));
        var sourceImport = inventory.Imports.Single(import => import.Kind == TargetHostAbiImportKind.SourceAccess);
        Assert.AreEqual("schema-source:s:1:test.rows", sourceImport.Name);
        Assert.AreEqual("source-access-v1", sourceImport.Contract);
        Assert.AreEqual(1, sourceImport.ContractVersion);
        var sourceDetails = Assert.IsInstanceOfType<TargetSourceAccessAbiDetails>(sourceImport.Details);
        Assert.AreEqual("schema-source", sourceDetails.SourceKind);
        Assert.AreEqual("s:1", sourceDetails.SourceContextId);
        Assert.AreEqual("test", sourceDetails.SchemaName);
        Assert.AreEqual("rows", sourceDetails.MethodName);
        Assert.AreEqual(1, sourceDetails.FieldCount);
        Assert.AreEqual(ExecutionPortableSymbolPortability.HostImport, sourceDetails.RowsPortability);
        Assert.AreEqual(ExecutionPortableSymbolPortability.ClrOnly, sourceDetails.SourcePortability);
        Assert.AreEqual("schema-source", sourceImport.Attributes["kind"]);
        Assert.AreEqual("s:1", sourceImport.Attributes["sourceContextId"]);
        Assert.AreEqual("test", sourceImport.Attributes["schemaName"]);
        Assert.AreEqual("rows", sourceImport.Attributes["methodName"]);
        Assert.AreEqual("HostImport", sourceImport.Attributes["rowsPortability"]);
        Assert.AreEqual("ClrOnly", sourceImport.Attributes["sourcePortability"]);
        Assert.AreEqual("1", sourceImport.Attributes["fieldCount"]);
    }

    [TestMethod]
    public void HostAbiInventoryBuilder_WhenSourceHasPortableContract_ShouldPreserveOrderedAbiFacts()
    {
        var field = new FieldBinding(
            "Name",
            "s.Name",
            3,
            typeof(string),
            FieldNullability.Nullable,
            new GeneratedFieldAccess("Name"),
            readModifiers: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["encoding"] = "utf-8"
            });
        var shape = new SourceEntityShape("s", typeof(SampleEntity), [field]);
        var binding = new ExecutionSourceBinding(
            "test",
            "rows",
            "s:portable",
            0,
            [new ExecutionLiteral("argument", typeof(string))],
            shape.Fields,
            SourceType: ExecutionClrBindingFactory.FromClr(typeof(SampleEntity)));
        var plan = new ExecutionPlan(
            "Q_SourcePortableAbi",
            [shape],
            new ExecutionBlock(
            [
                new ExecutionSourceScan(
                    new ExecutionVariable("s", typeof(SampleEntity)),
                    new ExecutionVariable("rows", typeof(object)),
                    binding)
            ]));
        var report = ExecutionTargetCompatibilityAnalyzer.Analyze(plan);
        var contract = TargetRuntimeContractBuilder.Build(
            plan,
            report,
            [
                new TargetSourceRuntimeMetadata(
                    "s:portable",
                    [TargetSourcePlanOperation.Columns, TargetSourcePlanOperation.Predicate],
                    [
                        new TargetRuntimeSettingAbiContract(
                            "apiKey",
                            true,
                            "Execution",
                            "Provided",
                            string.Empty),
                        new TargetRuntimeSettingAbiContract(
                            "format",
                            false,
                            "Planning, Execution",
                            "Defaulted",
                            "Input format")
                    ])
            ]);

        var sourceDetails = Assert.IsInstanceOfType<TargetSourceAccessAbiDetails>(
            TargetHostAbiInventoryBuilder.Build(contract).Imports.Single(import =>
                import.Kind == TargetHostAbiImportKind.SourceAccess).Details);

        Assert.HasCount(1, sourceDetails.Arguments);
        Assert.AreEqual(0, sourceDetails.Arguments[0].Position);
        Assert.Contains("string", sourceDetails.Arguments[0].TypeSymbol.StableName);
        Assert.HasCount(1, sourceDetails.Fields);
        Assert.AreEqual(3, sourceDetails.Fields[0].Index);
        Assert.AreEqual("Nullable", sourceDetails.Fields[0].Nullability);
        Assert.AreEqual("utf-8", sourceDetails.Fields[0].ReadModifiers["encoding"]);
        CollectionAssert.AreEqual(
            new[] { TargetSourcePlanOperation.Columns, TargetSourcePlanOperation.Predicate },
            sourceDetails.AcceptedOperations.ToArray());
        Assert.AreEqual("apiKey", sourceDetails.RuntimeSettings[0].Key);
        Assert.AreEqual(string.Empty, sourceDetails.RuntimeSettings[0].NonSecretDescription);
        Assert.AreEqual("Input format", sourceDetails.RuntimeSettings[1].NonSecretDescription);
        Assert.IsFalse(sourceDetails.Attributes.Values.Any(static value => value.Contains("argument", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void Build_WhenPlanCallsPluginMethod_ShouldDescribePluginInvocation()
    {
        var toUpper = ResolveLibraryMethod(nameof(LibraryBase.ToUpper), typeof(string));
        var plan = new ExecutionPlan(
            "Q_Method",
            [],
            new ExecutionBlock(
            [
                new ExecutionLet(
                    new ExecutionVariable("upper", typeof(string)),
                    new ExecutionMethodCall(
                        toUpper,
                        [new ExecutionLiteral("alpha", typeof(string))],
                        null,
                        typeof(string)))
            ]));

        var contract = Build(plan);

        Assert.AreEqual(1, contract.PluginInvocations.Count);
        Assert.AreEqual(nameof(LibraryBase.ToUpper), contract.PluginInvocations[0].Callable.MethodName);
        Assert.Contains("Musoq.Plugins.LibraryBase", contract.PluginInvocations[0].Callable.StableName);
    }

    [TestMethod]
    public void Build_WhenTheSamePluginCallIsRepeated_ShouldProduceOneInvocationAndOneOverloadSafeImport()
    {
        var contains = ResolveLibraryMethod(nameof(LibraryBase.Contains), typeof(string), typeof(string));
        var plan = new ExecutionPlan(
            "Q_RepeatedPluginContract",
            [],
            new ExecutionBlock(
            [
                new ExecutionLet(
                    new ExecutionVariable("first", typeof(bool?)),
                    new ExecutionMethodCall(
                        contains,
                        [new ExecutionLiteral("folder/file", typeof(string)), new ExecutionLiteral("/", typeof(string))],
                        null,
                        typeof(bool?))),
                new ExecutionLet(
                    new ExecutionVariable("second", typeof(bool?)),
                    new ExecutionMethodCall(
                        contains,
                        [new ExecutionLiteral("folder/file", typeof(string)), new ExecutionLiteral("/", typeof(string))],
                        null,
                        typeof(bool?)))
            ]));

        var contract = Build(plan);
        var inventory = TargetHostAbiInventoryBuilder.Build(contract);
        var import = inventory.Imports.Single(item => item.Kind == TargetHostAbiImportKind.PluginInvocation);

        Assert.HasCount(1, contract.PluginInvocations);
        Assert.Contains(contract.PluginInvocations[0].Callable.StableName, import.Name);
    }

    [TestMethod]
    public void Build_WhenOverloadedPluginMethodsAreUsed_ShouldProduceDistinctInvocationsAndImports()
    {
        var startsWithTwoArguments = ResolveLibraryMethod(nameof(LibraryBase.StartsWith), typeof(string), typeof(string));
        var startsWithThreeArguments = ResolveLibraryMethod(
            nameof(LibraryBase.StartsWith),
            typeof(string),
            typeof(string),
            typeof(string));
        var plan = new ExecutionPlan(
            "Q_OverloadedPluginContract",
            [],
            new ExecutionBlock(
            [
                new ExecutionLet(
                    new ExecutionVariable("two", typeof(bool?)),
                    new ExecutionMethodCall(
                        startsWithTwoArguments,
                        [new ExecutionLiteral("abc", typeof(string)), new ExecutionLiteral("a", typeof(string))],
                        null,
                        typeof(bool?))),
                new ExecutionLet(
                    new ExecutionVariable("three", typeof(bool?)),
                    new ExecutionMethodCall(
                        startsWithThreeArguments,
                        [
                            new ExecutionLiteral("abc", typeof(string)),
                            new ExecutionLiteral("a", typeof(string)),
                            new ExecutionLiteral("Ordinal", typeof(string))
                        ],
                        null,
                        typeof(bool?)))
            ]));

        var contract = Build(plan);
        var inventory = TargetHostAbiInventoryBuilder.Build(contract);
        var imports = inventory.Imports
            .Where(item => item.Kind == TargetHostAbiImportKind.PluginInvocation)
            .ToArray();

        Assert.HasCount(2, contract.PluginInvocations);
        Assert.HasCount(2, imports);
        Assert.AreNotEqual(imports[0].Name, imports[1].Name);
    }

    [TestMethod]
    public void LibraryBaseBindableMethods_ShouldHaveCollisionFreePluginAbiIdentities()
    {
        var methods = typeof(LibraryBase)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(static method => method.IsDefined(typeof(BindableMethodAttribute), inherit: true))
            .ToArray();
        var identities = methods
            .Select(static method =>
                $"{method.Name} [{ExecutionPortableSymbolFactory.FromMethod(method).StableName}]")
            .ToArray();

        Assert.AreEqual(identities.Length, identities.Distinct(StringComparer.Ordinal).Count());
    }

    [TestMethod]
    public void HostAbiInventory_WhenEveryTypedImportIsRepeated_ShouldCollapseEachImportKind()
    {
        var stringType = ExecutionPortableSymbolFactory.FromType(typeof(string));
        var pluginMethod = ResolveLibraryMethod(nameof(LibraryBase.Contains), typeof(string), typeof(string));
        var contract = new TargetRuntimeContract(
            "Q_AllAbiKinds",
            [new TargetSourceAccessContract(
                "schema-source",
                "source:1",
                "schema",
                "rows",
                stringType,
                stringType,
                [],
                [new TargetFieldContract(0, "Value", "Value", stringType, stringType, "Unknown", null)],
                [TargetSourcePlanOperation.Columns],
                [])],
            [new TargetPluginInvocationContract(
                "Contains -> Musoq.Plugins.LibraryBase.Contains",
                ExecutionPortableSymbolFactory.FromMethod(pluginMethod))],
            [new TargetRowShapeContract(
                "GeneratedRowShape",
                "Result",
                stringType,
                [new TargetFieldContract(0, "Value", "Value", stringType, stringType, "Unknown", null)])],
            new TargetNullBehaviorContract(true, true, true, "test-null-semantics"),
            new TargetCancellationContract(true, true),
            new TargetDiagnosticsContract(true, true, true),
            new TargetProfilingContract(true, true, 1, 1));

        var inventory = TargetHostAbiInventoryBuilder.Build(contract);
        var repeated = new TargetHostAbiInventory(inventory.Imports.Concat(inventory.Imports));

        Assert.AreEqual(inventory.Imports.Count, repeated.Imports.Count);
        CollectionAssert.AreEqual(
            inventory.Imports.Select(static import => $"{import.Kind}:{import.Name}").ToArray(),
            repeated.Imports.Select(static import => $"{import.Kind}:{import.Name}").ToArray());
    }

    [TestMethod]
    public void HostAbiInventoryBuilder_WhenPlanCallsHostAggregate_ShouldUseSharedCallableClassification()
    {
        var average = ResolveLibraryMethod(nameof(LibraryBase.Avg), typeof(int?), typeof(int));
        var plan = new ExecutionPlan(
            "Q_HostAggregate",
            [],
            new ExecutionBlock(
            [
                new ExecutionLet(
                    new ExecutionVariable("average", typeof(int?)),
                    new ExecutionMethodCall(
                        average,
                        [
                            new ExecutionLiteral(1, typeof(int?)),
                            new ExecutionLiteral(0, typeof(int))
                        ],
                        null,
                        typeof(int?)))
            ]));

        var contract = Build(plan);
        var invocation = contract.PluginInvocations.Single();
        var import = TargetHostAbiInventoryBuilder.Build(contract).Imports.Single(hostImport =>
            hostImport.Kind == TargetHostAbiImportKind.PluginInvocation);
        var details = Assert.IsInstanceOfType<TargetPluginInvocationAbiDetails>(import.Details);

        Assert.AreEqual(ExecutionPortableCallableKind.HostAggregate, invocation.Callable.Kind);
        Assert.AreEqual(nameof(LibraryBase.Avg), details.MethodName);
    }

    [TestMethod]
    public void RuntimeSettingAbiContract_ShouldNotExposeValuesOrSecretDescriptions()
    {
        var propertyNames = typeof(TargetRuntimeSettingAbiContract)
            .GetProperties()
            .Select(static property => property.Name)
            .ToArray();

        CollectionAssert.DoesNotContain(propertyNames, "Value");
        CollectionAssert.DoesNotContain(propertyNames, "Secret");
        CollectionAssert.Contains(propertyNames, nameof(TargetRuntimeSettingAbiContract.NonSecretDescription));
    }

    [TestMethod]
    public void HostAbiInventoryBuilder_WhenPlanCallsPluginAndUsesGeneratedRows_ShouldDescribePluginAndRows()
    {
        var toUpper = ResolveLibraryMethod(nameof(LibraryBase.ToUpper), typeof(string));
        var generatedShape = new GeneratedRowShape(
            "ResultRow0",
            [CreateField("Name", typeof(string), FieldNullability.Unknown)]);
        var plan = new ExecutionPlan(
            "Q_PluginAbi",
            [generatedShape],
            new ExecutionBlock(
            [
                new ExecutionLet(
                    new ExecutionVariable("upper", typeof(string)),
                    new ExecutionMethodCall(
                        toUpper,
                        [new ExecutionLiteral("alpha", typeof(string))],
                        null,
                        typeof(string)))
            ]));

        var inventory = TargetHostAbiInventoryBuilder.Build(Build(plan));

        Assert.IsTrue(inventory.Requires(TargetHostAbiImportKind.PluginInvocation));
        Assert.IsTrue(inventory.Requires(TargetHostAbiImportKind.RowShapeTransfer));
        var pluginImport = inventory.Imports.Single(import => import.Kind == TargetHostAbiImportKind.PluginInvocation);
        Assert.IsTrue(pluginImport.Name.Contains("Musoq.Plugins.LibraryBase.ToUpper", StringComparison.Ordinal));
        Assert.AreEqual("plugin-invocation-v2", pluginImport.Contract);
        Assert.AreEqual(2, pluginImport.ContractVersion);
        var pluginDetails = Assert.IsInstanceOfType<TargetPluginInvocationAbiDetails>(pluginImport.Details);
        Assert.AreEqual(nameof(LibraryBase.ToUpper), pluginDetails.MethodName);
        Assert.AreEqual(1, pluginDetails.ParameterCount);
        Assert.AreEqual(nameof(LibraryBase.ToUpper), pluginImport.Attributes["methodName"]);
        Assert.AreEqual("1", pluginImport.Attributes["parameterCount"]);

        var rowImport = inventory.Imports.Single(import => import.Kind == TargetHostAbiImportKind.RowShapeTransfer);
        Assert.AreEqual("GeneratedRowShape:ResultRow0", rowImport.Name);
        var rowDetails = Assert.IsInstanceOfType<TargetRowShapeTransferAbiDetails>(rowImport.Details);
        Assert.AreEqual("ResultRow0", rowDetails.Name);
        Assert.AreEqual(1, rowDetails.FieldCount);
        Assert.AreEqual(ExecutionPortableSymbolPortability.Portable, rowDetails.TypePortability);
        Assert.AreEqual("ResultRow0", rowImport.Attributes["name"]);
        Assert.AreEqual("Portable", rowImport.Attributes["typePortability"]);
        Assert.AreEqual("1", rowImport.Attributes["fieldCount"]);
    }

    [TestMethod]
    public void HostAbiImport_CreateCustom_ShouldDefensivelyCopyAttributes()
    {
        var attributes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["service"] = "source"
        };

        var import = TargetHostAbiImport.CreateCustom(
            TargetHostAbiImportKind.SourceAccess,
            "source",
            "source-access-v1",
            1,
            attributes);
        attributes["service"] = "mutated";

        Assert.IsInstanceOfType<TargetCustomAbiImportDetails>(import.Details);
        Assert.AreEqual("source", import.Attributes["service"]);
        Assert.Throws<NotSupportedException>(() =>
            ((IDictionary<string, string>)import.Attributes)["service"] = "blocked");
    }

    [TestMethod]
    public void HostAbiImport_ShouldNotExposeRawAttributeConstructor()
    {
        var rawAttributeConstructors = typeof(TargetHostAbiImport)
            .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(static constructor =>
                constructor.GetParameters()
                    .Any(static parameter =>
                        parameter.ParameterType == typeof(IReadOnlyDictionary<string, string>)))
            .ToArray();

        Assert.IsEmpty(rawAttributeConstructors);
    }

    [TestMethod]
    public void HostAbiImport_WhenDetailsKindDoesNotMatch_ShouldReject()
    {
        var details = new TargetDiagnosticsAbiDetails(
            RequiresBuildDiagnostics: true,
            RequiresSourceDiagnostics: true,
            RequiresRuntimeExceptionDiagnostics: true);

        var exception = Assert.Throws<ArgumentException>(() =>
            new TargetHostAbiImport(
                TargetHostAbiImportKind.SourceAccess,
                "source",
                "source-access-v1",
                1,
                details));

        Assert.Contains("does not match import kind", exception.Message);
    }

    [TestMethod]
    public void HostAbiImportDetails_WhenRequiredFieldsAreInvalid_ShouldReject()
    {
        Assert.Throws<ArgumentException>(() =>
            new TargetSourceAccessAbiDetails(
                "",
                "source",
                "schema",
                "rows",
                "rows:type",
                ExecutionPortableSymbolPortability.Portable,
                "",
                null,
                [],
                [],
                [],
                []));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TargetRowShapeTransferAbiDetails(
                "GeneratedRowShape",
                "ResultRow",
                "generated-row:ResultRow",
                ExecutionPortableSymbolPortability.Portable,
                -1));
    }

    [TestMethod]
    public void HostAbiImportDetails_WhenPortabilityIsTyped_ShouldExposeReadableAttributes()
    {
        foreach (var portability in new[]
                 {
                     ExecutionPortableSymbolPortability.Portable,
                     ExecutionPortableSymbolPortability.HostImport,
                     ExecutionPortableSymbolPortability.ClrOnly
                 })
        {
            var sourceDetails = new TargetSourceAccessAbiDetails(
                "schema-source",
                "source",
                "schema",
                "rows",
                "rows:type",
                portability,
                "source:type",
                portability,
                [],
                [CreateAbiField(portability)],
                [],
                []);
            var rowDetails = new TargetRowShapeTransferAbiDetails(
                "GeneratedRowShape",
                "ResultRow",
                "generated-row:ResultRow",
                portability,
                1);

            Assert.AreEqual(portability, sourceDetails.RowsPortability);
            Assert.AreEqual(portability, sourceDetails.SourcePortability);
            Assert.AreEqual(portability.ToString(), sourceDetails.Attributes["rowsPortability"]);
            Assert.AreEqual(portability.ToString(), sourceDetails.Attributes["sourcePortability"]);
            Assert.AreEqual(portability, rowDetails.TypePortability);
            Assert.AreEqual(portability.ToString(), rowDetails.Attributes["typePortability"]);
        }

        var sourceWithoutSourceType = new TargetSourceAccessAbiDetails(
            "schema-source",
            "source",
            "schema",
            "rows",
            "rows:type",
            ExecutionPortableSymbolPortability.Portable,
            "",
            null,
            [],
            [CreateAbiField(ExecutionPortableSymbolPortability.Portable)],
            [],
            []);
        var rowWithoutType = new TargetRowShapeTransferAbiDetails(
            "GeneratedRowShape",
            "ResultRow",
            "",
            null,
            1);

        Assert.IsNull(sourceWithoutSourceType.SourcePortability);
        Assert.AreEqual("", sourceWithoutSourceType.Attributes["sourcePortability"]);
        Assert.IsNull(rowWithoutType.TypePortability);
        Assert.AreEqual("", rowWithoutType.Attributes["typePortability"]);
    }

    [TestMethod]
    public void Build_WhenPlanHasAggregateShape_ShouldDescribeAggregateRowShape()
    {
        var aggregateShape = new AggregateGroupShape(
            "ResultAggregateGroup",
            [new AggregateGroupKeyField("Category", "__key0", typeof(string))],
            [],
            []);
        var plan = new ExecutionPlan(
            "Q_Aggregate",
            [aggregateShape],
            new ExecutionBlock([]));

        var contract = Build(plan);
        var shape = contract.RowShapes.Single();

        Assert.AreEqual(nameof(AggregateGroupShape), shape.Kind);
        StringAssert.StartsWith(shape.TypeSymbol!.StableName, "generated-row:sha256:");
        Assert.AreEqual("__key0", shape.Fields.Single().Name);
    }

    [TestMethod]
    public void Build_WhenPlanComputesPluginWindow_ShouldDescribeWindowPluginInvocation()
    {
        var rowNumber = ResolveLibraryMethod(nameof(LibraryBase.WindowRowNumber));
        var plan = new ExecutionPlan(
            "Q_Window",
            [],
            new ExecutionBlock(
            [
                new ExecutionComputePluginWindow(
                    new ExecutionVariable("buffer", typeof(object)),
                    new ExecutionVariable("item", typeof(object)),
                    ExecutionRowAccessMode.Direct,
                    null,
                    [],
                    new ExecutionLiteral(null, typeof(object)),
                    [],
                    [],
                    null,
                    rowNumber,
                    "row_number",
                    new ExecutionVariable("results", typeof(long[])))
            ]));

        var contract = Build(plan);

        Assert.AreEqual(1, contract.PluginInvocations.Count);
        Assert.AreEqual("row_number -> Musoq.Plugins.LibraryBase.WindowRowNumber", contract.PluginInvocations[0].Detail);
        Assert.IsTrue(contract.Cancellation.RequiresCancellationToken);
        Assert.IsTrue(contract.Profiling.SupportsOperatorProfiling);
    }

    [TestMethod]
    public void Build_WhenPlanHasGeneratedRowsAndNullableFields_ShouldDescribeRowsAndNullBehavior()
    {
        var generatedShape = new GeneratedRowShape(
            "ResultRow0",
            [CreateField("Score", typeof(int?), FieldNullability.Nullable)]);
        var plan = new ExecutionPlan(
            "Q_Generated",
            [generatedShape],
            new ExecutionBlock([]));

        var contract = Build(plan);
        var shape = contract.RowShapes.Single();

        Assert.AreEqual(nameof(GeneratedRowShape), shape.Kind);
        StringAssert.StartsWith(shape.TypeSymbol!.StableName, "generated-row:sha256:");
        Assert.IsTrue(contract.NullBehavior.UsesNullableValueTypes);
        Assert.IsTrue(contract.NullBehavior.UsesFieldNullabilityMetadata);
    }

    [TestMethod]
    public void Build_WhenCalledRepeatedly_ShouldProduceDeterministicContractSignature()
    {
        var plan = new ExecutionPlan(
            "Q_Deterministic",
            [
                new GeneratedRowShape("BRow", [CreateField("B", typeof(int), FieldNullability.Unknown)]),
                new GeneratedRowShape("ARow", [CreateField("A", typeof(string), FieldNullability.Unknown)])
            ],
            new ExecutionBlock([]));

        var first = CreateSignature(Build(plan));
        var second = CreateSignature(Build(plan));

        Assert.AreEqual(first, second);
        Assert.IsTrue(first.IndexOf("ARow", StringComparison.Ordinal) < first.IndexOf("BRow", StringComparison.Ordinal));
    }

    [TestMethod]
    public void HostAbiInventoryBuilder_WhenCalledRepeatedly_ShouldProduceDeterministicSignature()
    {
        var plan = new ExecutionPlan(
            "Q_DeterministicAbi",
            [
                new GeneratedRowShape("BRow", [CreateField("B", typeof(int), FieldNullability.Unknown)]),
                new GeneratedRowShape("ARow", [CreateField("A", typeof(string), FieldNullability.Unknown)])
            ],
            new ExecutionBlock([]));

        var first = CreateAbiSignature(TargetHostAbiInventoryBuilder.Build(Build(plan)));
        var second = CreateAbiSignature(TargetHostAbiInventoryBuilder.Build(Build(plan)));

        Assert.AreEqual(first, second);
        Assert.IsTrue(first.IndexOf("GeneratedRowShape:ARow", StringComparison.Ordinal) <
                      first.IndexOf("GeneratedRowShape:BRow", StringComparison.Ordinal));
    }

    private static TargetRuntimeContract Build(ExecutionPlan plan)
    {
        var report = ExecutionTargetCompatibilityAnalyzer.Analyze(plan);
        return TargetRuntimeContractBuilder.Build(plan, report);
    }

    private static TargetSourceFieldAbiContract CreateAbiField(
        ExecutionPortableSymbolPortability portability)
    {
        var symbol = new ExecutionPortableTypeDescriptor(
            ExecutionPortableTypeKind.Primitive,
            "string",
            "string")
        {
            Portability = portability,
            PortabilityReason = portability == ExecutionPortableSymbolPortability.ClrOnly
                ? "test fallback"
                : "test portable"
        };
        return new TargetSourceFieldAbiContract(0, "Name", symbol, symbol, "Unknown", null);
    }

    private static string CreateSignature(TargetRuntimeContract contract)
    {
        return string.Join(
            "|",
            contract.RowShapes.Select(shape => $"{shape.Kind}:{shape.Name}:{string.Join(",", shape.Fields.Select(field => field.Name))}"));
    }

    private static string CreateAbiSignature(TargetHostAbiInventory inventory)
    {
        return string.Join(
            "|",
            inventory.Imports.Select(import =>
                $"{import.Kind}:{import.Name}:{import.Contract}:v{import.ContractVersion}:{FormatAttributes(import.Attributes)}"));
    }

    private static string FormatAttributes(IReadOnlyDictionary<string, string> attributes)
    {
        return string.Join(
            ",",
            attributes
                .OrderBy(static pair => pair.Key, StringComparer.Ordinal)
                .Select(static pair => $"{pair.Key}={pair.Value}"));
    }

    private static FieldBinding CreateField(
        string name,
        Type type,
        FieldNullability nullability)
    {
        return new FieldBinding(
            name,
            name,
            0,
            type,
            nullability,
            new GeneratedFieldAccess(name));
    }

    private static MethodInfo ResolveLibraryMethod(string name, params Type[] parameterTypes)
    {
        return typeof(LibraryBase)
            .GetMethod(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static, parameterTypes) ??
               throw new InvalidOperationException($"Could not resolve LibraryBase.{name}.");
    }

    private sealed class SampleEntity
    {
        public string? Name { get; init; }
    }
}
