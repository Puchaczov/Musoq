using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Build;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.CodeGeneration;
using Musoq.Evaluator.IR.Execution.Portability;
using Musoq.Evaluator.Utils;
using Musoq.Converter.Tests.Schema;
using Musoq.Schema.Optimization;
using Musoq.Targets.Execution.Analysis;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class TargetRequestAndExportImmutabilityTests
{
    [TestMethod]
    public void TargetRenderRequestSubcontracts_WhenInputsAreMutated_ShouldRemainStable()
    {
        var options = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["mode"] = "initial"
        };
        var parameters = new List<string> { "p0" };
        var variables = new List<string> { "v0" };
        var references = new List<string> { "System.Private.CoreLib" };

        var renderOptions = new TargetRenderOptions(options);
        var scriptBinding = new TargetScriptBindingContract(parameters, variables);
        var referenceInventory = new TargetReferenceInventory(references);

        options["mode"] = "mutated";
        parameters.Add("p1");
        variables.Clear();
        references.Clear();

        Assert.AreEqual("initial", renderOptions.Values["mode"]);
        CollectionAssert.AreEqual(new[] { "p0" }, scriptBinding.ParameterNames.ToArray());
        CollectionAssert.AreEqual(new[] { "v0" }, scriptBinding.VariableNames.ToArray());
        CollectionAssert.AreEqual(new[] { "System.Private.CoreLib" }, referenceInventory.ReferenceNames.ToArray());
        Assert.Throws<NotSupportedException>(() => ((IDictionary<string, string>)renderOptions.Values)["mode"] = "exposed");
        Assert.Throws<NotSupportedException>(() => ((IList<string>)scriptBinding.ParameterNames).Add("exposed"));
        Assert.Throws<NotSupportedException>(() => ((IList<string>)referenceInventory.ReferenceNames).Clear());
    }

    [TestMethod]
    public void CSharpClrRenderInputs_WhenInputListsAreMutated_ShouldRemainStable()
    {
        var additionalTypes = new List<Type> { typeof(string) };
        var parameters = new List<ScriptParameterDefinition>();
        var variables = new List<ScriptVariableDefinition>();
        var assemblies = new List<Assembly> { typeof(string).Assembly };

        var inputs = new CSharpClrRenderInputs
        {
            ExecutionBindings = new CSharpClrExecutionBindingContext(),
            CompilationOptions = new CompilationOptions(),
            RenderProfile = TargetRenderProfile.StableArtifact,
            AssemblyName = "TargetInputs",
            NamespaceName = "TargetInputs",
            QueryResultMode = QueryResultMode.Table,
            AdditionalReferenceTypes = additionalTypes,
            Scope = new Scope(null, 0),
            ScriptParameterDefinitions = parameters,
            ScriptVariableDefinitions = variables,
            ReferenceAssemblies = assemblies
        };

        additionalTypes.Clear();
        parameters.Add(null!);
        variables.Add(null!);
        assemblies.Clear();

        Assert.HasCount(1, inputs.AdditionalReferenceTypes);
        Assert.AreEqual(typeof(string), inputs.AdditionalReferenceTypes[0]);
        Assert.IsEmpty(inputs.ScriptParameterDefinitions);
        Assert.IsEmpty(inputs.ScriptVariableDefinitions);
        Assert.HasCount(1, inputs.ReferenceAssemblies);
        Assert.AreSame(typeof(string).Assembly, inputs.ReferenceAssemblies[0]);
        Assert.Throws<NotSupportedException>(() => ((IList<Type>)inputs.AdditionalReferenceTypes).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<Assembly>)inputs.ReferenceAssemblies).Clear());
    }

    [TestMethod]
    public void TargetRenderInputCompilerState_WhenInputListsAreMutated_ShouldRemainStable()
    {
        var additionalTypes = new List<Type> { typeof(string) };
        var parameters = new List<ScriptParameterDefinition>();
        var variables = new List<ScriptVariableDefinition>();
        var assemblies = new List<Assembly> { typeof(string).Assembly };
        var state = new TargetRenderInputCompilerState(
            "CompilerState",
            typeof(string),
            additionalTypes,
            interpreterSourceCode: null,
            new Scope(null, 0),
            parameters,
            variables,
            assemblies);

        additionalTypes.Clear();
        parameters.Add(null!);
        variables.Add(null!);
        assemblies.Clear();

        Assert.AreEqual(typeof(string), state.OutputType);
        Assert.HasCount(1, state.AdditionalReferenceTypes);
        Assert.IsEmpty(state.ScriptParameterDefinitions);
        Assert.IsEmpty(state.ScriptVariableDefinitions);
        Assert.HasCount(1, state.ReferenceAssemblies);
        Assert.Throws<NotSupportedException>(() => ((IList<Type>)state.AdditionalReferenceTypes).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<Assembly>)state.ReferenceAssemblies).Clear());
    }

    [TestMethod]
    public void TargetExportArtifact_WhenConstructedDirectly_ShouldFreezeCollectionsAndBlobContent()
    {
        var sourceFiles = new List<TargetExportSourceFile>
        {
            new("query.js", "javascript", "export function run() {}")
        };
        var blobBytes = new byte[] { 0, 97, 115, 109 };
        var binaryBlobs = new List<TargetExportBinaryBlob>
        {
            new("query.wasm", blobBytes, "application/wasm")
        };
        var hostAbiImports = new List<TargetHostAbiImport>
        {
            new(
                TargetHostAbiImportKind.SourceAccess,
                "schema-source:source:test.rows",
                "source-access-v1",
                1,
                new TargetSourceAccessAbiDetails(
                    "schema-source",
                    "source",
                    "test",
                    "rows",
                    "object",
                    ExecutionPortableSymbolPortability.Portable,
                    string.Empty,
                    null,
                    [],
                    [],
                    [],
                    []))
        };
        var entrypoints = new List<TargetRuntimeEntrypoint>
        {
            new("run", TargetRuntimeEntrypointKind.TableQuery, "run")
        };
        var services = new HashSet<TargetRuntimeServiceRequirementKind>
        {
            TargetRuntimeServiceRequirementKind.SourceAccess
        };
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["targetFamily"] = "portable"
        };

        var artifact = new TargetExportArtifact(
            TestExecutionTargetIds.TestOnlyNonClr,
            sourceFiles,
            binaryBlobs,
            entrypoints,
            new TargetRuntimeServiceRequirements(services),
            new TargetHostAbiInventory(hostAbiImports),
            metadata);

        sourceFiles.Clear();
        binaryBlobs.Clear();
        hostAbiImports.Clear();
        entrypoints.Clear();
        services.Clear();
        metadata["targetFamily"] = "mutated";
        blobBytes[0] = 255;
        var exposedContent = artifact.BinaryBlobs[0].Content;
        exposedContent[1] = 255;

        Assert.HasCount(1, artifact.SourceFiles);
        Assert.HasCount(1, artifact.BinaryBlobs);
        Assert.HasCount(1, artifact.HostAbiInventory.Imports);
        Assert.HasCount(1, artifact.Entrypoints);
        Assert.AreEqual(0, artifact.BinaryBlobs[0].Content[0]);
        Assert.AreEqual(97, artifact.BinaryBlobs[0].Content[1]);
        Assert.IsTrue(artifact.RuntimeServices.Requires(TargetRuntimeServiceRequirementKind.SourceAccess));
        Assert.AreEqual("portable", artifact.DiagnosticsMetadata["targetFamily"]);
        Assert.Throws<NotSupportedException>(() => ((IList<TargetExportSourceFile>)artifact.SourceFiles).Clear());
        Assert.Throws<NotSupportedException>(() => ((IDictionary<string, string>)artifact.DiagnosticsMetadata)["targetFamily"] = "exposed");
    }

    [TestMethod]
    public void TargetFinalizationAndInspectionResults_WhenInputsAreMutated_ShouldRemainStable()
    {
        var diagnostics = new List<TargetDiagnostic>
        {
            new("MT-TEST", TargetDiagnosticSeverity.Warning, "initial")
        };
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["language"] = "fake-js"
        };

        var result = new TestFinalizationResult(
            diagnostics,
            TargetExportArtifact.Create(TestExecutionTargetIds.TestOnlyNonClr));
        var inspection = new RenderedQueryInspection(
            TestExecutionTargetIds.TestOnlyNonClr,
            null,
            metadata);

        diagnostics.Clear();
        metadata["language"] = "mutated";

        Assert.HasCount(1, result.Diagnostics);
        Assert.AreEqual("initial", result.Diagnostics[0].Message);
        Assert.AreEqual("fake-js", inspection.SourceMetadata["language"]);
        Assert.Throws<NotSupportedException>(() => ((IList<TargetDiagnostic>)result.Diagnostics).Clear());
        Assert.Throws<NotSupportedException>(() => ((IDictionary<string, string>)inspection.SourceMetadata)["language"] = "exposed");
    }

    [TestMethod]
    public void QueryRuntimeBinding_WhenInputsAreMutated_ShouldRemainStable()
    {
        var runtimeSettings = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["format"] = "json"
        };
        var settingsBySource = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal)
        {
            ["source"] = runtimeSettings
        };
        var descriptions = new List<SourceRuntimeSettingDescription>
        {
            new(
                "format",
                Required: false,
                Secret: false,
                SourceRuntimeSettingPhase.Execution,
                SourceRuntimeSettingResolutionStatus.Provided,
                "format setting")
        };
        var descriptionsBySource = new Dictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>>(StringComparer.Ordinal)
        {
            ["source"] = descriptions
        };
        var plans = new Dictionary<string, SourceExecutionPlan>(StringComparer.Ordinal)
        {
            ["source"] = SourceExecutionPlan.Empty(SourceIdentity.Empty)
        };

        var binding = new QueryRuntimeBinding(
            new SystemSchemaProvider(),
            settingsBySource,
            descriptionsBySource,
            plans);

        runtimeSettings["format"] = "csv";
        settingsBySource["other"] = new Dictionary<string, string>();
        descriptions.Clear();
        descriptionsBySource.Clear();
        plans.Clear();

        Assert.AreEqual("json", binding.SourceRuntimeSettingsBySourceContextId["source"]["format"]);
        Assert.HasCount(1, binding.SourceRuntimeSettingDescriptionsBySourceContextId["source"]);
        Assert.IsTrue(binding.SourceExecutionPlans.ContainsKey("source"));
        Assert.Throws<NotSupportedException>(() =>
            ((IDictionary<string, IReadOnlyDictionary<string, string>>)binding.SourceRuntimeSettingsBySourceContextId).Clear());
        Assert.Throws<NotSupportedException>(() =>
            ((IDictionary<string, string>)binding.SourceRuntimeSettingsBySourceContextId["source"])["format"] = "exposed");
        Assert.Throws<NotSupportedException>(() =>
            ((IList<SourceRuntimeSettingDescription>)binding.SourceRuntimeSettingDescriptionsBySourceContextId["source"]).Clear());
    }

    [TestMethod]
    public void TargetRuntimeAndAnalysisContracts_WhenInputsAreMutated_ShouldRemainStable()
    {
        var symbol = ExecutionPortableSymbolFactory.FromType(typeof(string));
        var fields = new List<TargetFieldContract>
        {
            new("Name", "s.Name", symbol, symbol, "NotNullable")
        };
        var sourceAccess = new List<TargetSourceAccessContract>
        {
            new("schema-source", "source", "test", "rows", symbol, symbol, fields)
        };
        var rowShapes = new List<TargetRowShapeContract>
        {
            new("SourceEntityShape", "s", symbol, fields)
        };
        var requirements = new List<ExecutionTargetRequirement>
        {
            new(ExecutionTargetRequirementKind.HostSourceAccess, "source")
        };
        var report = new ExecutionTargetCompatibilityReport(requirements);
        var contract = new TargetRuntimeContract(
            "Q_Test",
            sourceAccess,
            [],
            rowShapes,
            new TargetNullBehaviorContract(false, false, false, "test"),
            new TargetCancellationContract(true, false),
            new TargetDiagnosticsContract(true, true, true),
            new TargetProfilingContract(true, true, 1, 1));

        fields.Clear();
        sourceAccess.Clear();
        rowShapes.Clear();
        requirements.Clear();

        Assert.HasCount(1, contract.SourceAccess);
        Assert.HasCount(1, contract.SourceAccess[0].Fields);
        Assert.HasCount(1, contract.RowShapes);
        Assert.HasCount(1, contract.RowShapes[0].Fields);
        Assert.Throws<NotSupportedException>(() => ((IList<TargetSourceAccessContract>)contract.SourceAccess).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<TargetFieldContract>)contract.SourceAccess[0].Fields).Clear());
        Assert.HasCount(1, report.Requirements);
        Assert.Throws<NotSupportedException>(() => ((IList<ExecutionTargetRequirement>)report.Requirements).Clear());
    }

    [TestMethod]
    public void PortableSymbols_WhenInputsAreMutated_ShouldRemainStable()
    {
        var argument = ExecutionPortableSymbolFactory.FromType(typeof(int));
        var arguments = new List<ExecutionPortableTypeDescriptor>
        {
            argument
        };
        var symbol = new ExecutionPortableTypeDescriptor(
            ExecutionPortableTypeKind.List,
            "list:test",
            "test")
        {
            Arguments = arguments
        };
        var parameterTypes = new List<ExecutionPortableTypeDescriptor>
        {
            argument
        };
        var callable = new ExecutionPortableCallableDescriptor(
            ExecutionPortableCallableKind.ClrMethod,
            "method:test",
            "test")
        {
            ParameterTypes = parameterTypes
        };

        arguments.Clear();
        parameterTypes.Clear();

        Assert.HasCount(1, symbol.Arguments);
        Assert.HasCount(1, callable.ParameterTypes);
        Assert.Throws<NotSupportedException>(() => ((IList<ExecutionPortableTypeDescriptor>)symbol.Arguments).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<ExecutionPortableTypeDescriptor>)callable.ParameterTypes).Clear());
    }

    private sealed record TestFinalizationResult(
        IReadOnlyList<TargetDiagnostic> Diagnostics,
        ExecutableQueryArtifact ExecutableArtifact)
        : TargetFinalizationResult(
            TestExecutionTargetIds.TestOnlyNonClr,
            true,
            Diagnostics,
            ExecutableArtifact);
}
