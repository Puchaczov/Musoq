using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Plugins;
using Musoq.Targets.Execution.Analysis;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ExecutionTargetCompatibilityAnalyzerTests
{
    [TestMethod]
    public void Capabilities_WhenCSharpClr_ShouldAcceptAllCurrentRequirementKinds()
    {
        var requirements = Enum.GetValues<ExecutionTargetRequirementKind>()
            .Select(kind => new ExecutionTargetRequirement(kind, kind.ToString()))
            .ToArray();
        var report = new ExecutionTargetCompatibilityReport(requirements);

        var validation = ExecutionTargetCapabilities.CSharpClr.Validate(report);

        Assert.IsTrue(validation.IsSupported);
        Assert.IsEmpty(validation.UnsupportedRequirements);
    }

    [TestMethod]
    public void Capabilities_WhenCSharpClr_ShouldAcceptCurrentRuntimeRequirements()
    {
        var runtimeContract = CreateRuntimeContractWithAllCurrentRuntimeRequirements();

        var validation = ExecutionTargetCapabilities.CSharpClr.Validate(runtimeContract);

        Assert.IsTrue(validation.IsSupported);
        Assert.IsEmpty(validation.UnsupportedRequirements);
    }

    [TestMethod]
    public void Capabilities_WhenRequirementKindIsUnsupported_ShouldReportDeterministicDiagnostic()
    {
        var capabilities = ExecutionTargetCapabilities.Create(ExecutionTargetRequirementKind.ClrTypeUsage);
        var report = new ExecutionTargetCompatibilityReport(
        [
            new ExecutionTargetRequirement(ExecutionTargetRequirementKind.MethodInfoCall, "Musoq.Plugins.LibraryBase.ToUpper")
        ]);

        var validation = capabilities.Validate(report);

        Assert.IsFalse(validation.IsSupported);
        Assert.AreEqual(
            "Execution target 'TestTarget' does not support: MethodInfoCall: Musoq.Plugins.LibraryBase.ToUpper",
            validation.FormatUnsupportedRequirements("TestTarget"));
    }

    [TestMethod]
    public void Capabilities_WhenRuntimeRequirementKindIsUnsupported_ShouldReportDeterministicDiagnostic()
    {
        var capabilities = ExecutionTargetCapabilities.Create(
            [ExecutionTargetRequirementKind.ClrTypeUsage],
            [ExecutionTargetRequirementKind.Cancellation]);
        var runtimeContract = CreateRuntimeContractWithAllCurrentRuntimeRequirements();

        var validation = capabilities.Validate(runtimeContract);

        Assert.IsFalse(validation.IsSupported);
        Assert.AreEqual(
            "Execution target 'TestTarget' does not support: GeneratedClrRow: ResultRow0; PluginInvocation: Sample.Plugin; HostSourceAccess: schema-source:s:1:sample.rows; NullTypeCoercion: clr-null-and-sql-null-compatible:nullable-value-types+object-nulls+field-nullability-metadata; ProfilingDiagnostics: build-diagnostics+source-diagnostics+runtime-exception-diagnostics+source-boundary-profiling:1+operator-profiling:2",
            validation.FormatUnsupportedRequirements("TestTarget"));
    }

    [TestMethod]
    public void Capabilities_WhenKindIsSupportedButTypePortabilityIsNot_ShouldRejectSymbol()
    {
        var capabilities = ExecutionTargetCapabilities.Create(
            [ExecutionTargetRequirementKind.HostSourceAccess],
            [],
            [ExecutionPortableSymbolPortability.Portable, ExecutionPortableSymbolPortability.HostImport],
            [ExecutionPortableSymbolPortability.Portable, ExecutionPortableSymbolPortability.HostImport]);
        var symbol = ExecutionPortableSymbolFactory.FromType(typeof(Uri));
        var report = new ExecutionTargetCompatibilityReport(
        [
            new ExecutionTargetRequirement(
                ExecutionTargetRequirementKind.HostSourceAccess,
                "uri-source",
                symbol)
        ]);

        var validation = capabilities.Validate(report);

        Assert.IsFalse(validation.IsSupported);
        var diagnostic = validation.FormatUnsupportedRequirements("BrowserTarget");
        StringAssert.Contains(diagnostic, "uri-source");
        StringAssert.Contains(diagnostic, symbol.StableName);
        StringAssert.Contains(diagnostic, "[ClrOnly]");
        StringAssert.Contains(diagnostic, "No portable catalog entry");
    }

    [TestMethod]
    public void Capabilities_WhenKindIsSupportedButCallablePortabilityIsNot_ShouldRejectSymbol()
    {
        var capabilities = ExecutionTargetCapabilities.Create(
            [ExecutionTargetRequirementKind.MethodInfoCall],
            [],
            [ExecutionPortableSymbolPortability.Portable, ExecutionPortableSymbolPortability.HostImport],
            [ExecutionPortableSymbolPortability.Portable]);
        var symbol = ExecutionPortableSymbolFactory.FromMethod(
            ResolveLibraryMethod(nameof(LibraryBase.ToUpper), typeof(string)));
        var report = new ExecutionTargetCompatibilityReport(
        [
            new ExecutionTargetRequirement(
                ExecutionTargetRequirementKind.MethodInfoCall,
                "LibraryBase.ToUpper",
                CallableSymbol: symbol)
        ]);

        var validation = capabilities.Validate(report);

        Assert.IsFalse(validation.IsSupported);
        var diagnostic = validation.FormatUnsupportedRequirements("PortableOnlyTarget");
        StringAssert.Contains(diagnostic, nameof(LibraryBase.ToUpper));
        StringAssert.Contains(diagnostic, symbol.StableName);
        StringAssert.Contains(diagnostic, "[HostImport]");
    }

    [TestMethod]
    public void ReadinessAnalyzer_WhenCSharpClrProfileIsUsed_ShouldAcceptAllCurrentRequirementKinds()
    {
        var report = new ExecutionTargetCompatibilityReport(
            Enum.GetValues<ExecutionTargetRequirementKind>()
                .Select(kind => new ExecutionTargetRequirement(kind, kind.ToString()))
                .ToArray());

        var readiness = ExecutionTargetReadinessAnalyzer.Analyze(
            report,
            ExecutionTargetReadinessProfile.CSharpClr);

        Assert.IsTrue(readiness.IsReady(ExecutionTargetRuntimeFamily.CSharpClr));
        Assert.IsEmpty(readiness.Issues);
    }

    [TestMethod]
    public void ReadinessAnalyzer_WhenFutureProfilesAreUsed_ShouldReportDeterministicBlockers()
    {
        var report = new ExecutionTargetCompatibilityReport(
        [
            new ExecutionTargetRequirement(ExecutionTargetRequirementKind.ClrTypeUsage, "System.String"),
            new ExecutionTargetRequirement(ExecutionTargetRequirementKind.MethodInfoCall, "Plugin.Method"),
            new ExecutionTargetRequirement(ExecutionTargetRequirementKind.GeneratedClrRow, "ResultRow0")
        ]);

        var readiness = ExecutionTargetReadinessAnalyzer.AnalyzeFutureTargets(report);

        Assert.IsFalse(readiness.IsReady(ExecutionTargetRuntimeFamily.BrowserSource));
        AssertIssue(readiness, ExecutionTargetRuntimeFamily.BrowserSource, ExecutionTargetReadinessCategory.ClrOnlyTypeUsage, "System.String");
        AssertIssue(readiness, ExecutionTargetRuntimeFamily.BrowserSource, ExecutionTargetReadinessCategory.GeneratedRowShape, "ResultRow0");
        AssertIssue(readiness, ExecutionTargetRuntimeFamily.BytecodeVm, ExecutionTargetReadinessCategory.ReflectionMethodInfo, "Plugin.Method");
        AssertIssue(readiness, ExecutionTargetRuntimeFamily.Interpreter, ExecutionTargetReadinessCategory.GeneratedRowShape, "ResultRow0");
    }

    [TestMethod]
    public void ReadinessAnalyzer_WhenRuntimeContractIsProvided_ShouldReportRuntimeServiceBlockers()
    {
        var sourceShape = new SourceEntityShape(
            "s",
            typeof(SampleEntity),
            [new FieldBinding("Name", "s.Name", 0, typeof(string), FieldNullability.Nullable, new ClrPropertyAccess("Name"))]);
        var resultShape = new GeneratedRowShape(
            "ResultRow0",
            [new FieldBinding("Name", "Name", 0, typeof(string), FieldNullability.Nullable, new GeneratedFieldAccess("Name"))]);
        var binding = new ExecutionSourceBinding(
            "sample",
            "rows",
            "s:1",
            0,
            [],
            sourceShape.Fields,
            SourceType: ExecutionTypeRef.FromClr(typeof(SampleEntity)));
        var plan = new ExecutionPlan(
            "Q_ReadinessRuntime",
            [sourceShape, resultShape],
            new ExecutionBlock(
            [
                new ExecutionSourceScan(
                    new ExecutionVariable("s", typeof(SampleEntity)),
                    new ExecutionVariable("sRows", typeof(object)),
                    binding)
            ]));
        var compatibilityReport = ExecutionTargetCompatibilityAnalyzer.Analyze(plan);
        var runtimeContract = TargetRuntimeContractBuilder.Build(plan, compatibilityReport);

        var readiness = ExecutionTargetReadinessAnalyzer.AnalyzeFutureTargets(compatibilityReport, runtimeContract);

        AssertIssue(
            readiness,
            ExecutionTargetRuntimeFamily.BytecodeVm,
            ExecutionTargetReadinessCategory.HostSourceAccess,
            "schema-source:s:1:sample.rows");
        AssertIssue(
            readiness,
            ExecutionTargetRuntimeFamily.BrowserSource,
            ExecutionTargetReadinessCategory.Cancellation,
            "cancellation-token");
        AssertIssue(
            readiness,
            ExecutionTargetRuntimeFamily.Interpreter,
            ExecutionTargetReadinessCategory.GeneratedRowShape,
            "ResultRow0");
        Assert.IsFalse(
            readiness.IssuesFor(ExecutionTargetRuntimeFamily.BrowserSource)
                .Any(issue => issue.Category == ExecutionTargetReadinessCategory.HostSourceAccess),
            "Browser-like source target profile accepts host source access in the placeholder readiness matrix.");
    }

    [TestMethod]
    public void ReadinessAnalyzer_WhenSupportedRequirementCarriesClrOnlySymbol_ShouldReportSymbolBlocker()
    {
        var report = new ExecutionTargetCompatibilityReport(
        [
            new ExecutionTargetRequirement(
                ExecutionTargetRequirementKind.HostSourceAccess,
                "host-source-with-clr-shape",
                ExecutionPortableSymbolFactory.FromType(typeof(Uri)))
        ]);

        var readiness = ExecutionTargetReadinessAnalyzer.Analyze(
            report,
            ExecutionTargetReadinessProfile.BrowserLikeSource);

        var issue = readiness.Issues.Single(issue =>
            issue.RuntimeFamily == ExecutionTargetRuntimeFamily.BrowserSource &&
            issue.Category == ExecutionTargetReadinessCategory.ClrOnlyTypeUsage);
        StringAssert.Contains(issue.Requirement.Detail, "host-source-with-clr-shape");
        StringAssert.Contains(issue.Requirement.Detail, "[ClrOnly]");
        StringAssert.Contains(issue.Requirement.Detail, "No portable catalog entry");
        StringAssert.Contains(issue.Diagnostic, "No portable catalog entry");
    }

    [TestMethod]
    public void ReadinessAnalyzer_WhenCategoryIsSupportedButTypeSymbolPortabilityIsNot_ShouldReportSymbolBlocker()
    {
        var profile = ExecutionTargetReadinessProfile.Create(
            ExecutionTargetRuntimeFamily.BrowserSource,
            [
                ExecutionTargetReadinessCategory.ClrOnlyTypeUsage,
                ExecutionTargetReadinessCategory.HostSourceAccess
            ],
            [
                ExecutionPortableSymbolPortability.Portable,
                ExecutionPortableSymbolPortability.HostImport
            ],
            [
                ExecutionPortableSymbolPortability.Portable,
                ExecutionPortableSymbolPortability.HostImport
            ]);
        var report = new ExecutionTargetCompatibilityReport(
        [
            new ExecutionTargetRequirement(
                ExecutionTargetRequirementKind.ClrTypeUsage,
                "System.Uri",
                ExecutionPortableSymbolFactory.FromType(typeof(Uri)))
        ]);

        var readiness = ExecutionTargetReadinessAnalyzer.Analyze(report, profile);

        var issue = readiness.Issues.Single();
        Assert.AreEqual(ExecutionTargetReadinessCategory.ClrOnlyTypeUsage, issue.Category);
        StringAssert.Contains(issue.Requirement.Detail, "clr:System.Uri");
        StringAssert.Contains(issue.Requirement.Detail, "[ClrOnly]");
        StringAssert.Contains(issue.Requirement.Detail, "No portable catalog entry");
    }

    [TestMethod]
    public void ReadinessAnalyzer_WhenCategoryIsSupportedButCallablePortabilityIsNot_ShouldReportSymbolBlocker()
    {
        var profile = ExecutionTargetReadinessProfile.Create(
            ExecutionTargetRuntimeFamily.BrowserSource,
            [ExecutionTargetReadinessCategory.ReflectionMethodInfo],
            [
                ExecutionPortableSymbolPortability.Portable,
                ExecutionPortableSymbolPortability.HostImport
            ],
            [
                ExecutionPortableSymbolPortability.Portable,
                ExecutionPortableSymbolPortability.HostImport
            ]);
        var callable = new ExecutionPortableCallableDescriptor(
            ExecutionPortableCallableKind.ClrMethod,
            "method:clr:Unknown.Method",
            "Unknown.Method")
        {
            Portability = ExecutionPortableSymbolPortability.ClrOnly,
            PortabilityReason = "No portable callable catalog entry.",
            MethodName = "Method"
        };
        var report = new ExecutionTargetCompatibilityReport(
        [
            new ExecutionTargetRequirement(
                ExecutionTargetRequirementKind.MethodInfoCall,
                "Unknown.Method",
                CallableSymbol: callable)
        ]);

        var readiness = ExecutionTargetReadinessAnalyzer.Analyze(report, profile);

        var issue = readiness.Issues.Single();
        Assert.AreEqual(ExecutionTargetReadinessCategory.ReflectionMethodInfo, issue.Category);
        StringAssert.Contains(issue.Requirement.Detail, "method:clr:Unknown.Method");
        StringAssert.Contains(issue.Requirement.Detail, "[ClrOnly]");
        StringAssert.Contains(issue.Requirement.Detail, "No portable callable catalog entry");
    }

    [TestMethod]
    public void ReadinessAnalyzer_WhenRestrictedProfileIsUsed_ShouldReportNullDiagnosticsAndProfiling()
    {
        var resultShape = new GeneratedRowShape(
            "ResultRow0",
            [new FieldBinding("Score", "Score", 0, typeof(int?), FieldNullability.Nullable, new GeneratedFieldAccess("Score"))]);
        var plan = new ExecutionPlan(
            "Q_ReadinessRestricted",
            [resultShape],
            new ExecutionBlock(
            [
                new ExecutionLet(
                    new ExecutionVariable("score", typeof(int?)),
                    new ExecutionLiteral(1, typeof(int?)))
            ]));
        var compatibilityReport = ExecutionTargetCompatibilityAnalyzer.Analyze(plan);
        var runtimeContract = TargetRuntimeContractBuilder.Build(plan, compatibilityReport);
        var restrictedProfile = ExecutionTargetReadinessProfile.Create(
            ExecutionTargetRuntimeFamily.BrowserSource,
            ExecutionTargetReadinessCategory.GeneratedRowShape);

        var readiness = ExecutionTargetReadinessAnalyzer.Analyze(
            compatibilityReport,
            runtimeContract,
            restrictedProfile);

        AssertIssue(
            readiness,
            ExecutionTargetRuntimeFamily.BrowserSource,
            ExecutionTargetReadinessCategory.NullTypeCoercion,
            "clr-null-and-sql-null-compatible:nullable-value-types+field-nullability-metadata");
        Assert.IsTrue(
            readiness.IssuesFor(ExecutionTargetRuntimeFamily.BrowserSource)
                .Any(issue =>
                    issue.Category == ExecutionTargetReadinessCategory.ProfilingDiagnostics &&
                    issue.Requirement.Detail.Contains("operator-profiling:1", StringComparison.Ordinal)),
            "Expected restricted profile to report diagnostics/profiling runtime contract requirements.");
    }

    [TestMethod]
    public void Analyze_WhenPlanUsesSourceAndGeneratedRows_ShouldReportSchemaClrAndGeneratedRowRequirements()
    {
        var sourceShape = new SourceEntityShape(
            "s",
            typeof(SampleEntity),
            [
                new FieldBinding("Name", "s.Name", 0, typeof(string), FieldNullability.Unknown, new ClrPropertyAccess("Name")),
                new FieldBinding("Score", "s.Score", 1, typeof(int), FieldNullability.Unknown, new ClrPropertyAccess("Score"))
            ]);
        var resultShape = new GeneratedRowShape(
            "ResultRow0",
            [
                new FieldBinding("Name", "Name", 0, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("Name"))
            ]);
        var binding = new ExecutionSourceBinding(
            "test",
            "rows",
            "s:1",
            0,
            [],
            sourceShape.Fields,
            SourceType: ExecutionTypeRef.FromClr(typeof(SampleEntity)));
        var plan = new ExecutionPlan(
            "Q_AuditShapes",
            [sourceShape, resultShape],
            new ExecutionBlock(
            [
                new ExecutionSourceScan(
                    new ExecutionVariable("s", typeof(SampleEntity)),
                    new ExecutionVariable("sRows", typeof(object)),
                    binding)
            ]));

        var report = ExecutionTargetCompatibilityAnalyzer.Analyze(plan);

        AssertContainsKind(report, ExecutionTargetRequirementKind.SchemaProviderBinding);
        AssertContainsRequirement(report, ExecutionTargetRequirementKind.GeneratedClrRow, "ResultRow0");
        AssertContainsRequirement(report, ExecutionTargetRequirementKind.ClrTypeUsage, typeof(SampleEntity).FullName!);
        AssertContainsRequirement(report, ExecutionTargetRequirementKind.ClrTypeUsage, typeof(string).FullName!);
        var generatedRowRequirement = FindRequirement(report, ExecutionTargetRequirementKind.GeneratedClrRow, "ResultRow0");
        Assert.AreEqual(ExecutionPortableTypeKind.GeneratedRow, generatedRowRequirement.TypeSymbol!.Kind);
        StringAssert.StartsWith(generatedRowRequirement.TypeSymbol.StableName, "generated-row:sha256:");
        Assert.AreEqual(ExecutionPortableSymbolPortability.Portable, generatedRowRequirement.TypeSymbol.Portability);
        var stringRequirement = FindRequirement(report, ExecutionTargetRequirementKind.ClrTypeUsage, typeof(string).FullName!);
        Assert.AreEqual("primitive:string", stringRequirement.TypeSymbol!.StableName);
        Assert.AreEqual(ExecutionPortableSymbolPortability.Portable, stringRequirement.TypeSymbol.Portability);
        Assert.IsTrue(report.HasRequirements);
    }

    [TestMethod]
    public void Analyze_WhenPlanUsesMethodsAndPluginWindows_ShouldReportRuntimeRequirements()
    {
        var toUpper = ResolveLibraryMethod(nameof(LibraryBase.ToUpper), typeof(string));
        var rowNumber = ResolveLibraryMethod(nameof(LibraryBase.WindowRowNumber));
        var methodCall = new ExecutionMethodCall(
            toUpper,
            [new ExecutionLiteral("alpha", typeof(string))],
            null,
            typeof(string));
        var pluginWindow = new ExecutionComputePluginWindow(
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
            new ExecutionVariable("results", typeof(long[])));
        var plan = new ExecutionPlan(
            "Q_AuditRuntime",
            [],
            new ExecutionBlock(
            [
                new ExecutionLet(new ExecutionVariable("upper", typeof(string)), methodCall),
                pluginWindow
            ]));

        var report = ExecutionTargetCompatibilityAnalyzer.Analyze(plan);

        AssertContainsRequirement(report, ExecutionTargetRequirementKind.MethodInfoCall, "Musoq.Plugins.LibraryBase.ToUpper");
        AssertContainsRequirement(report, ExecutionTargetRequirementKind.MethodInfoCall, "Musoq.Plugins.LibraryBase.WindowRowNumber");
        AssertContainsRequirement(report, ExecutionTargetRequirementKind.PluginInvocation, "row_number -> Musoq.Plugins.LibraryBase.WindowRowNumber");
        var methodRequirement = FindRequirement(report, ExecutionTargetRequirementKind.MethodInfoCall, "Musoq.Plugins.LibraryBase.ToUpper");
        Assert.AreEqual(ExecutionPortableCallableKind.HostPlugin, methodRequirement.CallableSymbol!.Kind);
        Assert.AreEqual(nameof(LibraryBase.ToUpper), methodRequirement.CallableSymbol.MethodName);
        Assert.Contains("Musoq.Plugins.LibraryBase", methodRequirement.CallableSymbol.StableName);
        Assert.AreEqual(ExecutionPortableSymbolPortability.HostImport, methodRequirement.CallableSymbol.Portability);
        var readiness = ExecutionTargetReadinessAnalyzer.AnalyzeFutureTargets(report);
        AssertIssue(readiness, ExecutionTargetRuntimeFamily.BrowserSource, ExecutionTargetReadinessCategory.ReflectionMethodInfo, "Musoq.Plugins.LibraryBase.ToUpper");
        AssertIssue(readiness, ExecutionTargetRuntimeFamily.BytecodeVm, ExecutionTargetReadinessCategory.PluginInvocation, "row_number -> Musoq.Plugins.LibraryBase.WindowRowNumber");
    }

    [TestMethod]
    public void Analyze_WhenLiteralRequiresClrSidecar_ShouldReportExplicitConstantBlocker()
    {
        var literal = new ExecutionLiteral(new UnsupportedConstant("value"), typeof(UnsupportedConstant));
        var plan = new ExecutionPlan(
            "Q_ClrConstant",
            [],
            new ExecutionBlock([
                new ExecutionLet(new ExecutionVariable("value", typeof(UnsupportedConstant)), literal)
            ]));

        var report = ExecutionTargetCompatibilityAnalyzer.Analyze(plan);
        var requirement = report.Requirements.Single(static requirement =>
            requirement.Kind == ExecutionTargetRequirementKind.ClrOnlyConstant);

        Assert.AreEqual(ExecutionPortableSymbolPortability.ClrOnly, requirement.TypeSymbol?.Portability);
        Assert.IsTrue(ExecutionTargetCapabilities.CSharpClr.Validate(report).IsSupported);
        Assert.IsFalse(ExecutionTargetCapabilities.Create().Validate(report).IsSupported);
    }

    [TestMethod]
    public void Analyze_WhenCustomLibraryMethodIsOutsidePluginsNamespace_ShouldReportHostPluginInvocation()
    {
        var method = typeof(CustomPortableLibrary).GetMethod(nameof(CustomPortableLibrary.Echo))!;
        var plan = new ExecutionPlan(
            "Q_CustomPlugin",
            [],
            new ExecutionBlock(
            [
                new ExecutionLet(
                    new ExecutionVariable("value", typeof(string)),
                    new ExecutionMethodCall(
                        method,
                        [new ExecutionLiteral("sample", typeof(string))],
                        null,
                        typeof(string)))
            ]));

        var report = ExecutionTargetCompatibilityAnalyzer.Analyze(plan);

        var requirement = report.Requirements.Single(item =>
            item.Kind == ExecutionTargetRequirementKind.PluginInvocation);
        Assert.AreEqual(ExecutionPortableCallableKind.HostPlugin, requirement.CallableSymbol!.Kind);
        Assert.AreEqual(ExecutionPortableSymbolPortability.HostImport, requirement.CallableSymbol.Portability);
        StringAssert.Contains(requirement.Detail, nameof(CustomPortableLibrary.Echo));
    }

    private static MethodInfo ResolveLibraryMethod(string name, params Type[] parameterTypes)
    {
        return typeof(LibraryBase)
            .GetMethod(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static, parameterTypes) ??
               throw new InvalidOperationException($"Could not resolve LibraryBase.{name}.");
    }

    private static void AssertContainsKind(
        ExecutionTargetCompatibilityReport report,
        ExecutionTargetRequirementKind kind)
    {
        Assert.IsTrue(
            report.Requirements.Any(requirement => requirement.Kind == kind),
            $"Expected compatibility report to contain requirement kind {kind}.");
    }

    private static void AssertContainsRequirement(
        ExecutionTargetCompatibilityReport report,
        ExecutionTargetRequirementKind kind,
        string detail)
    {
        Assert.IsTrue(
            report.Requirements.Any(requirement =>
                requirement.Kind == kind &&
                requirement.Detail == detail),
            $"Expected compatibility report to contain {kind}: {detail}.");
    }

    private static ExecutionTargetRequirement FindRequirement(
        ExecutionTargetCompatibilityReport report,
        ExecutionTargetRequirementKind kind,
        string detail)
    {
        return report.Requirements.Single(requirement =>
            requirement.Kind == kind &&
            requirement.Detail == detail);
    }

    private static TargetRuntimeContract CreateRuntimeContractWithAllCurrentRuntimeRequirements()
    {
        return new TargetRuntimeContract(
            "Q_CapabilityRuntime",
            [
                new TargetSourceAccessContract(
                    "schema-source",
                    "s:1",
                    "sample",
                    "rows",
                    ExecutionPortableSymbolFactory.FromType(typeof(object)),
                    ExecutionPortableSymbolFactory.FromType(typeof(SampleEntity)),
                    [])
            ],
            [
                new TargetPluginInvocationContract(
                    "Sample.Plugin",
                    ExecutionPortableSymbolFactory.FromMethod(
                        typeof(string).GetMethod(nameof(string.ToUpper), Type.EmptyTypes)!))
            ],
            [
                new TargetRowShapeContract(
                    nameof(GeneratedRowShape),
                    "ResultRow0",
                    ExecutionPortableSymbolFactory.GeneratedRow("ResultRow0", []),
                    [])
            ],
            new TargetNullBehaviorContract(
                UsesNullableValueTypes: true,
                UsesObjectNulls: true,
                UsesFieldNullabilityMetadata: true,
                Semantics: "clr-null-and-sql-null-compatible"),
            new TargetCancellationContract(
                RequiresCancellationToken: true,
                RequiresParallelCancellation: false),
            new TargetDiagnosticsContract(
                RequiresBuildDiagnostics: true,
                RequiresSourceDiagnostics: true,
                RequiresRuntimeExceptionDiagnostics: true),
            new TargetProfilingContract(
                SupportsSourceBoundaryProfiling: true,
                SupportsOperatorProfiling: true,
                SourceBoundaryCount: 1,
                OperatorCount: 2));
    }

    private static void AssertIssue(
        ExecutionTargetReadinessReport report,
        ExecutionTargetRuntimeFamily runtimeFamily,
        ExecutionTargetReadinessCategory category,
        string detail)
    {
        Assert.IsTrue(
            report.Issues.Any(issue =>
                issue.RuntimeFamily == runtimeFamily &&
                issue.Category == category &&
                issue.Requirement.Detail == detail),
            $"Expected readiness issue for {runtimeFamily}, {category}, {detail}.");
    }

    private sealed class SampleEntity
    {
        public string? Name { get; init; }

        public int Score { get; init; }
    }

    private sealed class CustomPortableLibrary : LibraryBase
    {
        public string Echo(string value)
        {
            return value;
        }
    }

    private sealed record UnsupportedConstant(string Value);
}
