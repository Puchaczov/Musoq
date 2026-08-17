using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;
using NameDto = Musoq.Converter.Tests.TwoModeTestFixtures.NameDto;
using Person = Musoq.Converter.Tests.TwoModeTestFixtures.Person;
using static Musoq.Converter.Tests.TwoModeTestFixtures;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class ParamsAliasConverterEquivalenceTests
{
    private static readonly CompilationOptions CompilationOptions = new(ParallelizationMode.None);
    private static readonly SystemSchemaProvider SchemaProvider = new();
    private static readonly TestsLoggerResolver LoggerResolver = new();

    [TestMethod]
    public async Task CompilationSurfaces_ShouldExposeEquivalentParameterContractsAndGeneratedBinding()
    {
        const string body = @"
(required: int, ids: string[], optional: string = 'fallback', nullable: int? = null)
select $required as Required, $optional as Optional, $nullable as Nullable
from #system.dual() d
where d.Dummy in $ids";

        var canonicalScript = "param" + body;
        var aliasScript = "params" + body;

        using var canonical = InstanceCreator.CompileForExecution(
            canonicalScript,
            "ParamsAliasExecution",
            SchemaProvider,
            LoggerResolver,
            CompilationOptions);
        using var alias = InstanceCreator.CompileForExecution(
            aliasScript,
            "ParamsAliasExecution",
            SchemaProvider,
            LoggerResolver,
            CompilationOptions);

        SetParameters(canonical);
        SetParameters(alias);
        using var canonicalTable = canonical.Run();
        using var aliasTable = alias.Run();

        AssertCompiledQueryEquivalent(canonical, alias);
        AssertTablesEquivalent(canonicalTable, aliasTable);

        var canonicalDiagnostics = InstanceCreator.CompileWithDiagnostics(
            canonicalScript,
            "ParamsAliasDiagnostics",
            SchemaProvider,
            LoggerResolver,
            CompilationOptions);
        var aliasDiagnostics = InstanceCreator.CompileWithDiagnostics(
            aliasScript,
            "ParamsAliasDiagnostics",
            SchemaProvider,
            LoggerResolver,
            CompilationOptions);

        Assert.IsTrue(canonicalDiagnostics.Succeeded, FormatDiagnostics(canonicalDiagnostics));
        Assert.IsTrue(aliasDiagnostics.Succeeded, FormatDiagnostics(aliasDiagnostics));
        using var canonicalDiagnosticQuery = canonicalDiagnostics.CompiledQuery ??
                                             throw new AssertFailedException(FormatDiagnostics(canonicalDiagnostics));
        using var aliasDiagnosticQuery = aliasDiagnostics.CompiledQuery ??
                                         throw new AssertFailedException(FormatDiagnostics(aliasDiagnostics));
        AssertCompiledQueryEquivalent(canonicalDiagnosticQuery, aliasDiagnosticQuery);
        AssertDiagnosticKindsEquivalent(canonicalDiagnostics.Diagnostics, aliasDiagnostics.Diagnostics);

        var canonicalAsync = await InstanceCreator.CompileWithDiagnosticsAsync(
            canonicalScript,
            "ParamsAliasAsync",
            SchemaProvider,
            LoggerResolver,
            CompilationOptions).ConfigureAwait(false);
        var aliasAsync = await InstanceCreator.CompileWithDiagnosticsAsync(
            aliasScript,
            "ParamsAliasAsync",
            SchemaProvider,
            LoggerResolver,
            CompilationOptions).ConfigureAwait(false);

        Assert.IsTrue(canonicalAsync.Succeeded, FormatDiagnostics(canonicalAsync));
        Assert.IsTrue(aliasAsync.Succeeded, FormatDiagnostics(aliasAsync));
        using var canonicalAsyncQuery = canonicalAsync.CompiledQuery ??
                                         throw new AssertFailedException(FormatDiagnostics(canonicalAsync));
        using var aliasAsyncQuery = aliasAsync.CompiledQuery ??
                                    throw new AssertFailedException(FormatDiagnostics(aliasAsync));
        AssertCompiledQueryEquivalent(canonicalAsyncQuery, aliasAsyncQuery);

        var canonicalInspection = InstanceCreator.CompileForInspection(
            canonicalScript,
            "ParamsAliasInspection",
            SchemaProvider,
            LoggerResolver,
            CompilationOptions);
        var aliasInspection = InstanceCreator.CompileForInspection(
            aliasScript,
            "ParamsAliasInspection",
            SchemaProvider,
            LoggerResolver,
            CompilationOptions);

        AssertInspectionEquivalent(canonicalInspection, aliasInspection);

        if (Debugger.IsAttached)
            return;

        var cacheSuffix = Guid.NewGuid().ToString("N");
        var cacheParameter = "cacheValue" + cacheSuffix;
        var cacheBody = "\n(" + cacheParameter + ": int) select $" + cacheParameter +
                           " as Value from #system.dual() d";
        var cacheOptions = new CompilationOptions();
        var cachedCanonical = InstanceCreator.CompileWithDiagnostics(
            "param" + cacheBody,
            $"ParamsAliasCacheCanonical_{cacheSuffix}",
            SchemaProvider,
            LoggerResolver,
            cacheOptions);
        var cachedAlias = InstanceCreator.CompileWithDiagnostics(
            "params" + cacheBody,
            $"ParamsAliasCacheAlias_{cacheSuffix}",
            SchemaProvider,
            LoggerResolver,
            cacheOptions);

        Assert.IsTrue(cachedCanonical.Succeeded, FormatDiagnostics(cachedCanonical));
        Assert.IsTrue(cachedAlias.Succeeded, FormatDiagnostics(cachedAlias));
        using var cachedCanonicalQuery = cachedCanonical.CompiledQuery ??
                                         throw new AssertFailedException(FormatDiagnostics(cachedCanonical));
        using var cachedAliasQuery = cachedAlias.CompiledQuery ??
                                    throw new AssertFailedException(FormatDiagnostics(cachedAlias));
        Assert.IsNotNull(cachedCanonical.BuildItems);
        Assert.IsNotNull(cachedAlias.BuildItems);
        var canonicalContract = InstanceCreator.CreateCanonicalExecutionContractForTests(
            cachedCanonical.BuildItems!,
            SchemaProvider);
        var aliasContract = InstanceCreator.CreateCanonicalExecutionContractForTests(
            cachedAlias.BuildItems!,
            SchemaProvider);

        Assert.AreEqual(canonicalContract.NormalizedGeneratedSyntax, aliasContract.NormalizedGeneratedSyntax);
        Assert.AreEqual(canonicalContract.RuntimeContractFingerprint, aliasContract.RuntimeContractFingerprint);
        Assert.AreEqual(canonicalContract.ExecutionSemanticsFingerprint, aliasContract.ExecutionSemanticsFingerprint);
        Assert.AreEqual(canonicalContract.SemanticContractFingerprint, aliasContract.SemanticContractFingerprint);
        Assert.AreEqual(
            InstanceCreator.GetCanonicalExecutionEntryIdentityForTests(cachedCanonical.BuildItems!, SchemaProvider),
            InstanceCreator.GetCanonicalExecutionEntryIdentityForTests(cachedAlias.BuildItems!, SchemaProvider));
    }

    [TestMethod]
    public void TypedAndProfiledPublicApis_ShouldTreatAliasAsCanonical()
    {
        const string canonicalScript =
            "param(expected: string) select p.Name as Name from #A.entities() p where p.Name = $expected order by p.Name";
        const string aliasScript =
            "params(expected: string) select p.Name as Name from #A.entities() p where p.Name = $expected order by p.Name";
        var people = People();
        var options = new TypedQueryRunOptions(
            CancellationToken.None,
            new Dictionary<string, object?> { ["expected"] = "Alice" });

        var canonical = CreateTypedBuilder(canonicalScript).Compile<NameDto>();
        var alias = CreateTypedBuilder(aliasScript).Compile<NameDto>();
        AssertTypedQueryEquivalent(canonical, alias);

        var canonicalRows = canonical.Run(options, SourceRows(people)).ToArray();
        var aliasRows = alias.Run(options, SourceRows(people)).ToArray();
        CollectionAssert.AreEqual(canonicalRows, aliasRows);

        var canonicalProfile = CreateTypedBuilder(canonicalScript).CompileForProfile<NameDto>();
        var aliasProfile = CreateTypedBuilder(aliasScript).CompileForProfile<NameDto>();
        AssertTypedProfileEquivalent(canonicalProfile, aliasProfile);

        var canonicalProfileResult = canonicalProfile.RunWithProfile(options, SourceRows(people));
        var aliasProfileResult = aliasProfile.RunWithProfile(options, SourceRows(people));
        var canonicalProfileRows = canonicalProfileResult.Rows.ToArray();
        var aliasProfileRows = aliasProfileResult.Rows.ToArray();

        CollectionAssert.AreEqual(canonicalProfileRows, aliasProfileRows);
        Assert.AreEqual(canonicalProfileResult.Profile.Sources.Count, aliasProfileResult.Profile.Sources.Count);
        Assert.AreEqual(canonicalProfileResult.Profile.Operators.Count, aliasProfileResult.Profile.Operators.Count);
    }

    [TestMethod]
    public void TypedInspectionAndArtifacts_ShouldKeepPublicShapeAndMetadataEquivalent()
    {
        const string body = @"
(expected: string = 'single')
select d.Dummy as Name from #system.dual() d where d.Dummy = $expected";
        var canonicalScript = "param" + body;
        var aliasScript = "params" + body;

        var canonicalInspection = InstanceCreator.CompileForTypedInspection<NameDto>(
            canonicalScript,
            "ParamsAliasTypedInspection",
            SchemaProvider,
            LoggerResolver,
            CompilationOptions);
        var aliasInspection = InstanceCreator.CompileForTypedInspection<NameDto>(
            aliasScript,
            "ParamsAliasTypedInspection",
            SchemaProvider,
            LoggerResolver,
            CompilationOptions);

        AssertTypedInspectionEquivalent(canonicalInspection, aliasInspection);

        var canonicalArtifact = InstanceCreator.CompileForTypedArtifact<NameDto>(
            canonicalScript,
            "ParamsAliasTypedArtifact",
            SchemaProvider,
            LoggerResolver,
            CompilationOptions);
        var aliasArtifact = InstanceCreator.CompileForTypedArtifact<NameDto>(
            aliasScript,
            "ParamsAliasTypedArtifact",
            SchemaProvider,
            LoggerResolver,
            CompilationOptions);

        AssertTypedArtifactEquivalent(canonicalArtifact, aliasArtifact);

        var canonicalLoaded = InstanceCreator.LoadTypedArtifact<NameDto>(
            canonicalArtifact,
            SchemaProvider,
            LoggerResolver);
        var aliasLoaded = InstanceCreator.LoadTypedArtifact<NameDto>(
            aliasArtifact,
            SchemaProvider,
            LoggerResolver);

        CollectionAssert.AreEqual(
            canonicalLoaded.Run().ToArray(),
            aliasLoaded.Run().ToArray());
    }

    [TestMethod]
    public void TargetContractsAndArtifactMetadata_ShouldTreatAliasAsCanonical()
    {
        const string body = @"
(expected: string = 'single')
select d.Dummy as Name from #system.dual() d where d.Dummy = $expected";
        var canonicalScript = "param" + body;
        var aliasScript = "params" + body;

        var canonicalPackage = InstanceCreator.CompileTargetPackageWithDiagnostics(
            canonicalScript,
            "ParamsAliasTarget",
            SchemaProvider,
            LoggerResolver,
            ExecutionTargetIds.CSharpClr,
            CompilationOptions);
        var aliasPackage = InstanceCreator.CompileTargetPackageWithDiagnostics(
            aliasScript,
            "ParamsAliasTarget",
            SchemaProvider,
            LoggerResolver,
            ExecutionTargetIds.CSharpClr,
            CompilationOptions);

        Assert.IsTrue(canonicalPackage.Succeeded, FormatDiagnostics(canonicalPackage.Diagnostics));
        Assert.IsTrue(aliasPackage.Succeeded, FormatDiagnostics(aliasPackage.Diagnostics));
        var canonicalItems = canonicalPackage.BuildItems ??
                             throw new AssertFailedException("Canonical target build did not expose build items.");
        var aliasItems = aliasPackage.BuildItems ??
                         throw new AssertFailedException("Alias target build did not expose build items.");

        AssertParameterSurface(canonicalItems.ScriptParameterDefinitions, aliasItems.ScriptParameterDefinitions);
        Assert.AreEqual(
            canonicalItems.RenderingArtifacts.SemanticsContract,
            aliasItems.RenderingArtifacts.SemanticsContract);
        var canonicalTargetContract = InstanceCreator.CreateCanonicalExecutionContractForTests(
            canonicalItems,
            SchemaProvider);
        var aliasTargetContract = InstanceCreator.CreateCanonicalExecutionContractForTests(
            aliasItems,
            SchemaProvider);
        Assert.AreEqual(
            canonicalTargetContract.RuntimeContractFingerprint,
            aliasTargetContract.RuntimeContractFingerprint);
        Assert.AreEqual(
            canonicalPackage.Package!.Metadata[CompiledQueryArtifactSupport.MetadataSemanticShapeSha256],
            aliasPackage.Package!.Metadata[CompiledQueryArtifactSupport.MetadataSemanticShapeSha256]);
        Assert.AreEqual(
            canonicalPackage.Package!.Metadata[CompiledQueryArtifactSupport.MetadataGeneratedCodeSha256],
            aliasPackage.Package!.Metadata[CompiledQueryArtifactSupport.MetadataGeneratedCodeSha256]);
        Assert.AreNotEqual(
            canonicalPackage.Package!.Metadata[CompiledQueryArtifactSupport.MetadataScriptSha256],
            aliasPackage.Package!.Metadata[CompiledQueryArtifactSupport.MetadataScriptSha256]);

        var canonicalArtifact = InstanceCreator.CompileArtifactWithDiagnostics(
            canonicalScript,
            "ParamsAliasPublicArtifact",
            SchemaProvider,
            LoggerResolver,
            CompilationOptions);
        var aliasArtifact = InstanceCreator.CompileArtifactWithDiagnostics(
            aliasScript,
            "ParamsAliasPublicArtifact",
            SchemaProvider,
            LoggerResolver,
            CompilationOptions);

        Assert.IsTrue(canonicalArtifact.Succeeded, FormatDiagnostics(canonicalArtifact.Diagnostics));
        Assert.IsTrue(aliasArtifact.Succeeded, FormatDiagnostics(aliasArtifact.Diagnostics));
        var canonicalPublicArtifact = canonicalArtifact.Artifact ??
                                      throw new AssertFailedException("Canonical artifact was not created.");
        var aliasPublicArtifact = aliasArtifact.Artifact ??
                                  throw new AssertFailedException("Alias artifact was not created.");
        Assert.AreEqual(
            canonicalPublicArtifact.Metadata[CompiledQueryArtifactSupport.MetadataSemanticShapeSha256],
            aliasPublicArtifact.Metadata[CompiledQueryArtifactSupport.MetadataSemanticShapeSha256]);
        Assert.AreEqual(
            canonicalPublicArtifact.Metadata[CompiledQueryArtifactSupport.MetadataGeneratedCodeSha256],
            aliasPublicArtifact.Metadata[CompiledQueryArtifactSupport.MetadataGeneratedCodeSha256]);
        Assert.AreEqual(
            canonicalPublicArtifact.Metadata[CompiledQueryArtifactSupport.MetadataRuntimeV2ContractSignature],
            aliasPublicArtifact.Metadata[CompiledQueryArtifactSupport.MetadataRuntimeV2ContractSignature]);
        Assert.AreEqual(
            canonicalPublicArtifact.Metadata[CompiledQueryArtifactSupport.MetadataExecutionSemanticsVersion],
            aliasPublicArtifact.Metadata[CompiledQueryArtifactSupport.MetadataExecutionSemanticsVersion]);
        Assert.AreNotEqual(
            canonicalPublicArtifact.Metadata[CompiledQueryArtifactSupport.MetadataScriptSha256],
            aliasPublicArtifact.Metadata[CompiledQueryArtifactSupport.MetadataScriptSha256]);
    }

    private static MusoqQueryBuilder CreateTypedBuilder(string script)
    {
        return Musoq.Query(script)
            .Source<Person>("#A", "entities")
            .WithCompilationOptions(CompilationOptions);
    }

    private static MusoqSourceRows SourceRows(IReadOnlyList<Person> people)
    {
        return Musoq.Source("#A", "entities", Chunks(people));
    }

    private static void SetParameters(CompiledQuery query)
    {
        query.Parameters["required"] = 7;
        query.Parameters["ids"] = new[] { "single" };
    }

    private static void AssertCompiledQueryEquivalent(CompiledQuery expected, CompiledQuery actual)
    {
        AssertParameterSurface(expected.ParameterDefinitions, actual.ParameterDefinitions);
        AssertParameterSurface(expected.RequiredParameters, actual.RequiredParameters);
        AssertContractSurface(expected.ParameterContracts, actual.ParameterContracts);
    }

    private static void AssertTypedQueryEquivalent<TOut>(
        ICompiledTypedQuery<TOut> expected,
        ICompiledTypedQuery<TOut> actual)
    {
        AssertParameterSurface(expected.ParameterDefinitions, actual.ParameterDefinitions);
        AssertParameterSurface(expected.RequiredParameters, actual.RequiredParameters);
        AssertContractSurface(expected.ParameterContracts, actual.ParameterContracts);
        Assert.AreEqual(expected.Diagnostics.ResultMode, actual.Diagnostics.ResultMode);
        Assert.AreEqual(expected.Diagnostics.SelectedResultSinkKind, actual.Diagnostics.SelectedResultSinkKind);
        Assert.AreEqual(expected.Diagnostics.RowPathKind, actual.Diagnostics.RowPathKind);
        Assert.AreEqual(
            expected.Diagnostics.RequiresComputeTableMethod,
            actual.Diagnostics.RequiresComputeTableMethod);
        Assert.AreEqual(
            expected.Diagnostics.FinalSinkRejectionKind,
            actual.Diagnostics.FinalSinkRejectionKind);
        Assert.AreEqual(expected.Diagnostics.FinalSinkRejectionReason, actual.Diagnostics.FinalSinkRejectionReason);
    }

    private static void AssertTypedProfileEquivalent<TOut>(
        ICompiledTypedProfileQuery<TOut> expected,
        ICompiledTypedProfileQuery<TOut> actual)
    {
        AssertParameterSurface(expected.ParameterDefinitions, actual.ParameterDefinitions);
        AssertParameterSurface(expected.RequiredParameters, actual.RequiredParameters);
        AssertContractSurface(expected.ParameterContracts, actual.ParameterContracts);
        Assert.AreEqual(expected.Diagnostics.ResultMode, actual.Diagnostics.ResultMode);
        Assert.AreEqual(expected.Diagnostics.ProfileMode, actual.Diagnostics.ProfileMode);
    }

    private static void AssertTypedInspectionEquivalent(
        TypedQueryInspectionResult expected,
        TypedQueryInspectionResult actual)
    {
        Assert.AreEqual(expected.ResultMode, actual.ResultMode);
        Assert.AreEqual(expected.SelectedResultSinkKind, actual.SelectedResultSinkKind);
        Assert.AreEqual(expected.OutputType, actual.OutputType);
        Assert.AreEqual(expected.RowsKind, actual.RowsKind);
        Assert.AreEqual(expected.RowPathKind, actual.RowPathKind);
        Assert.AreEqual(expected.RequiresComputeTableMethod, actual.RequiresComputeTableMethod);
        Assert.AreEqual(expected.FinalSinkRejectionKind, actual.FinalSinkRejectionKind);
        Assert.AreEqual(expected.FinalSinkRejectionReason, actual.FinalSinkRejectionReason);
        CollectionAssert.AreEqual(
            expected.OutputBindingDiagnostics.ToArray(),
            actual.OutputBindingDiagnostics.ToArray());
        Assert.IsNotNull(expected.Query);
        Assert.IsNotNull(actual.Query);
        Assert.AreEqual(expected.GeneratedCSharpCode, actual.GeneratedCSharpCode);
    }

    private static void AssertTypedArtifactEquivalent(
        ICompiledTypedQueryArtifact expected,
        ICompiledTypedQueryArtifact actual)
    {
        Assert.AreEqual(expected.ArtifactVersion, actual.ArtifactVersion);
        Assert.AreEqual(expected.RuntimeContractSignature, actual.RuntimeContractSignature);
        Assert.AreEqual(expected.ResultMode, actual.ResultMode);
        Assert.AreEqual(expected.OutputType, actual.OutputType);
        Assert.AreEqual(expected.OutputTypeName, actual.OutputTypeName);
        AssertParameterSurface(expected.ParameterDefinitions, actual.ParameterDefinitions);
        AssertContractSurface(expected.ParameterContracts, actual.ParameterContracts);
        CollectionAssert.AreEqual(
            expected.SourceSlotIdentities.ToArray(),
            actual.SourceSlotIdentities.ToArray());
        Assert.AreEqual(
            expected.SourceRuntimeSettingsBySourceContextId.Count,
            actual.SourceRuntimeSettingsBySourceContextId.Count);
        Assert.AreEqual(expected.SourceExecutionPlans.Count, actual.SourceExecutionPlans.Count);
    }

    private static void AssertInspectionEquivalent(
        QueryInspectionResult expected,
        QueryInspectionResult actual)
    {
        Assert.AreEqual(expected.LogicalPlanText, actual.LogicalPlanText);
        Assert.AreEqual(expected.PhysicalPlanText, actual.PhysicalPlanText);
        Assert.AreEqual(expected.PlanningText, actual.PlanningText);
        Assert.AreEqual(expected.ExecutionPlanText, actual.ExecutionPlanText);
        Assert.AreEqual(expected.InitialLogicalPlanText, actual.InitialLogicalPlanText);
        Assert.AreEqual(expected.OptimizedLogicalPlanText, actual.OptimizedLogicalPlanText);
        Assert.AreEqual(expected.InitialPhysicalPlanText, actual.InitialPhysicalPlanText);
        Assert.AreEqual(expected.OptimizedPhysicalPlanText, actual.OptimizedPhysicalPlanText);
        Assert.AreEqual(expected.InitialExecutionPlanText, actual.InitialExecutionPlanText);
        Assert.AreEqual(expected.OptimizedExecutionPlanText, actual.OptimizedExecutionPlanText);
        Assert.AreEqual(expected.OptimizerTraceText, actual.OptimizerTraceText);
        Assert.AreEqual(expected.GeneratedCSharpCode, actual.GeneratedCSharpCode);
        AssertDiagnosticKindsEquivalent(expected.Diagnostics, actual.Diagnostics);
    }

    private static void AssertParameterSurface(
        IReadOnlyList<ScriptParameterDefinition> expected,
        IReadOnlyList<ScriptParameterDefinition> actual)
    {
        Assert.HasCount(expected.Count, actual);
        for (var index = 0; index < expected.Count; index++)
        {
            var expectedParameter = expected[index];
            var actualParameter = actual[index];
            Assert.AreEqual(expectedParameter.Name, actualParameter.Name);
            Assert.AreEqual(expectedParameter.ParameterType, actualParameter.ParameterType);
            Assert.AreEqual(expectedParameter.HasDefaultValue, actualParameter.HasDefaultValue);
            Assert.AreEqual(expectedParameter.DefaultValue, actualParameter.DefaultValue);
            Assert.AreEqual(expectedParameter.IsRequired, actualParameter.IsRequired);
            AssertContractSurface([expectedParameter.Contract], [actualParameter.Contract]);
        }
    }

    private static void AssertContractSurface(
        IReadOnlyList<ScriptParameterContract> expected,
        IReadOnlyList<ScriptParameterContract> actual)
    {
        Assert.HasCount(expected.Count, actual);
        for (var index = 0; index < expected.Count; index++)
        {
            var expectedContract = expected[index];
            var actualContract = actual[index];
            Assert.AreEqual(expectedContract.Name, actualContract.Name);
            Assert.AreEqual(expectedContract.DeclaredTypeName, actualContract.DeclaredTypeName);
            Assert.AreEqual(expectedContract.CanonicalTypeName, actualContract.CanonicalTypeName);
            Assert.AreEqual(expectedContract.ClrType, actualContract.ClrType);
            Assert.AreEqual(expectedContract.IsNullable, actualContract.IsNullable);
            Assert.AreEqual(expectedContract.IsCollection, actualContract.IsCollection);
            Assert.AreEqual(expectedContract.ElementClrType, actualContract.ElementClrType);
            Assert.AreEqual(
                expectedContract.ElementCanonicalTypeName,
                actualContract.ElementCanonicalTypeName);
            Assert.AreEqual(expectedContract.HasDefaultValue, actualContract.HasDefaultValue);
            Assert.AreEqual(expectedContract.DefaultKind, actualContract.DefaultKind);
            Assert.AreEqual(expectedContract.DefaultValue, actualContract.DefaultValue);
        }
    }

    private static void AssertDiagnosticKindsEquivalent(
        IReadOnlyList<global::Musoq.Parser.Diagnostics.Diagnostic> expected,
        IReadOnlyList<global::Musoq.Parser.Diagnostics.Diagnostic> actual)
    {
        Assert.HasCount(expected.Count, actual);
        for (var index = 0; index < expected.Count; index++)
        {
            Assert.AreEqual(expected[index].Code, actual[index].Code);
            Assert.AreEqual(expected[index].Severity, actual[index].Severity);
            Assert.AreEqual(expected[index].Phase, actual[index].Phase);
            Assert.AreEqual(expected[index].Message, actual[index].Message);
        }
    }

    private static void AssertTablesEquivalent(Table expected, Table actual)
    {
        Assert.AreEqual(expected.Count, actual.Count);
        for (var rowIndex = 0; rowIndex < expected.Count; rowIndex++)
        {
            Assert.AreEqual(expected[rowIndex].Count, actual[rowIndex].Count);
            for (var columnIndex = 0; columnIndex < expected[rowIndex].Count; columnIndex++)
                Assert.AreEqual(expected[rowIndex][columnIndex], actual[rowIndex][columnIndex]);
        }
    }

    private static string FormatDiagnostics(BuildResult result)
    {
        return string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToDetailedString()));
    }

    private static string FormatDiagnostics(IReadOnlyList<global::Musoq.Parser.Diagnostics.Diagnostic> diagnostics)
    {
        return string.Join(Environment.NewLine, diagnostics.Select(static diagnostic => diagnostic.ToDetailedString()));
    }
}
