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
    }

    private byte[]? GetExecutableDllFile()
    {
        return CSharpClrArtifactCompatibility.GetDllFile(ExecutableArtifact);
    }

    private byte[]? GetExecutablePdbFile()
    {
        return CSharpClrArtifactCompatibility.GetPdbFile(ExecutableArtifact);
    }

    private byte[]? GetDllFileValue()
    {
        return GetOptional<byte[]>(BuildItemKeys.DllFile) is { } value
            ? (byte[])value.Clone()
            : GetExecutableDllFile();
    }

    private byte[]? GetPdbFileValue()
    {
        return GetOptional<byte[]>(BuildItemKeys.PdbFile) is { } value
            ? (byte[])value.Clone()
            : GetExecutablePdbFile();
    }
}
