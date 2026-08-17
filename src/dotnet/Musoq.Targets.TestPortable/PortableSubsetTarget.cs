using System.Collections.Generic;
using System.Collections.Frozen;
using System.Linq;

namespace Musoq.Targets.TestPortable;

internal static class PortableSubsetTarget
{
    public static ExecutionTargetId TargetId { get; } = new("test-portable-subset");

    public static IReadOnlySet<ExecutionOperationId> SupportedOperations { get; } = new[]
    {
        "source.scan",
        "table.create",
        "table.values.create",
        "control.foreach",
        "control.scope",
        "variable.let",
        "variable.assign",
        "control.continue",
        "control.continue-if",
        "control.if",
        "object.create",
        "row.generated.create",
        "table.row.append",
        "table.sort",
        "table.top-offset",
        "table.skip",
        "table.take",
        "table.slice",
        "return.table",
        "expr.field-read",
        "expr.script-parameter",
        "expr.script-variable",
        "expr.literal",
        "expr.binary",
        "expr.unary",
        "expr.call",
        "expr.strict-cast",
        "expr.null-check",
        "expr.in",
        "expr.case",
        "expr.coalesce",
        "stream.rows",
        "expr.variable-read"
    }.Select(static value => new ExecutionOperationId(value)).ToFrozenSet();

    public static ExecutionTargetCapabilities Capabilities { get; } = ExecutionTargetCapabilities.Create(
        [
            ExecutionTargetRequirementKind.ClrTypeUsage,
            ExecutionTargetRequirementKind.MethodInfoCall,
            ExecutionTargetRequirementKind.SchemaProviderBinding,
            ExecutionTargetRequirementKind.GeneratedClrRow,
            ExecutionTargetRequirementKind.PluginInvocation,
            ExecutionTargetRequirementKind.HostSourceAccess,
            ExecutionTargetRequirementKind.NullTypeCoercion,
            ExecutionTargetRequirementKind.ProfilingDiagnostics,
            ExecutionTargetRequirementKind.Cancellation
        ],
        [
            ExecutionTargetRequirementKind.HostSourceAccess,
            ExecutionTargetRequirementKind.GeneratedClrRow,
            ExecutionTargetRequirementKind.PluginInvocation,
            ExecutionTargetRequirementKind.NullTypeCoercion,
            ExecutionTargetRequirementKind.Cancellation,
            ExecutionTargetRequirementKind.ProfilingDiagnostics
        ],
        [
            ExecutionPortableSymbolPortability.Portable,
            ExecutionPortableSymbolPortability.HostImport,
            ExecutionPortableSymbolPortability.ClrOnly
        ],
        [
            ExecutionPortableSymbolPortability.Portable,
            ExecutionPortableSymbolPortability.HostImport,
            ExecutionPortableSymbolPortability.ClrOnly
        ],
        SupportedOperations,
        [ExecutionSemanticsContract.Version1.Version],
        [
            ExecutionTargetFeatureKind.ConstantKind,
            ExecutionTargetFeatureKind.BinaryOperation,
            ExecutionTargetFeatureKind.UnaryOperation,
            ExecutionTargetFeatureKind.StrictCastTarget,
            ExecutionTargetFeatureKind.Callable,
            ExecutionTargetFeatureKind.CallableKind,
            ExecutionTargetFeatureKind.SourceKind,
            ExecutionTargetFeatureKind.ReadModifier,
            ExecutionTargetFeatureKind.TypePortability,
            ExecutionTargetFeatureKind.Container,
            ExecutionTargetFeatureKind.DynamicValue
        ]);
}
