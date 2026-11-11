using System.Text;
using System.Threading;
using Musoq.Converter.Build;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Schema;
using Musoq.Targets.Abstractions;
using Musoq.Targets.Execution;

namespace Musoq.Converter;

internal sealed record InterpretationExecutableIdentity(
    CSharpArtifactCompilationIdentity Compilation,
    string RuntimeFingerprint
)
{
    public static InterpretationExecutableIdentity? TryCreate(
        RenderingBuildArtifacts rendering,
        TargetArtifactSemanticFacts facts,
        CancellationToken cancellationToken
    )
    {
        if (rendering.RuntimeContract is not { } runtime)
            return null;

        var compilation = CSharpClrGeneratedCodeCompatibility.TryGetCompilationIdentity(
            rendering.Artifact,
            cancellationToken
        );
        if (compilation is null)
            return null;

        TargetHostAbiInventory abi;
        try
        {
            abi = TargetHostAbiInventoryBuilder.Build(runtime);
        }
        catch (ArgumentException exception) when (exception.ParamName == "imports")
        {
            // A CLR query may render even if its imports cannot be represented
            // as a canonical set. Keep its ordinary compilation path.
            return null;
        }

        var identity = new StringBuilder();
        AppendComponent(identity, RuntimeV2Contract.ContractSignature);
        AppendComponent(identity, abi.ContractVersion.ToString());
        AppendComponent(identity, TargetContractVersions.ExecutionIr.ToString());
        AppendComponent(identity, ByteBufferCallableSemantics.Contract);
        AppendComponent(identity, runtime.ToString());
        AppendComponent(
            identity,
            CompiledQueryArtifactSupport.ComputeSemanticShapeHash(facts, "<interpretation-cache>", cancellationToken)
        );
        foreach (var import in abi.Imports)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AppendComponent(identity, import.Kind.ToString());
            AppendComponent(identity, import.Name);
            AppendComponent(identity, import.Contract);
            AppendComponent(identity, import.ContractVersion.ToString());
            AppendComponent(identity, import.CanonicalDefinition);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return new InterpretationExecutableIdentity(
            compilation,
            CompiledQueryArtifactSupport.ComputeHash(identity.ToString())
        );
    }

    private static void AppendComponent(StringBuilder builder, string value)
    {
        builder.Append(value.Length).Append(':').Append(value);
    }
}
