namespace Musoq.Converter.Build;

public partial class BuildItems
{
    private const string FinalizationPurposeKey = "FINALIZATION_PURPOSE";

    internal TargetFinalizationPurpose FinalizationPurpose
    {
        get => TryGetArtifact(FinalizationPurposeKey, out TargetFinalizationPurpose purpose)
            ? purpose
            : TargetFinalizationPurpose.Execution;
        set => SetRequired(FinalizationPurposeKey, value);
    }
}
