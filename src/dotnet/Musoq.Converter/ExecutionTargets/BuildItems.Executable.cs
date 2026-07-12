using Musoq.Targets.CSharpClr;

namespace Musoq.Converter.Build;

public partial class BuildItems
{
    internal ExecutableQueryArtifact? ExecutableArtifact
    {
        get => GetOptional<ExecutableQueryArtifact>(BuildItemKeys.ExecutableArtifact);
        set => SetExecutableArtifact(value);
    }

    private void SetExecutableArtifact(ExecutableQueryArtifact? artifact)
    {
        SetOptional(BuildItemKeys.ExecutableArtifact, artifact);

        if (CSharpClrArtifactCompatibility.TryGetAssemblyExecutable(artifact, out var clrArtifact))
        {
            DllFile = clrArtifact.DllFile;
            PdbFile = clrArtifact.PdbFile;
        }
    }
}
