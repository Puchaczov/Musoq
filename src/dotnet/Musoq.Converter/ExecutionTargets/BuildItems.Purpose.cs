namespace Musoq.Converter.Build;

public partial class BuildItems
{
    internal CompilationPurpose CompilationPurpose
    {
        get => TryGetArtifact(BuildItemKeys.CompilationPurpose, out CompilationPurpose purpose)
            ? purpose : CompilationPurpose.Execution;
        set => SetRequired(BuildItemKeys.CompilationPurpose, value);
    }
}
