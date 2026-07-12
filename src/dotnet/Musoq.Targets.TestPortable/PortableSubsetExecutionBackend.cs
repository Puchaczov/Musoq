using System;

namespace Musoq.Targets.TestPortable;

internal sealed class PortableSubsetExecutionBackend : IQueryExecutionBackend
{
    public ExecutionTargetId TargetId => PortableSubsetTarget.TargetId;

    public ExecutionTargetCapabilities Capabilities { get; } = PortableSubsetTarget.Capabilities;

    public TargetRenderResult Render(TargetRenderRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.TargetId != TargetId)
        {
            throw new InvalidOperationException(
                $"Portable subset backend expected target '{TargetId}', but received '{request.TargetId}'.");
        }

        PortableSubsetProgram program;
        try
        {
            program = PortableSubsetLowerer.Lower(request.ExecutionPlan);
        }
        catch (PortableSubsetLoweringException exception)
        {
            return TargetRenderResult.Failed(
                TargetId,
                [TargetDiagnostic.Error(TargetDiagnosticCodes.UnsupportedLowering, exception.Message)]);
        }
        var hostAbiInventory = TargetHostAbiInventoryBuilder.Build(request.RuntimeContract);
        return TargetRenderResult.Succeeded(new PortableSubsetRenderedArtifact(program, hostAbiInventory));
    }
}
