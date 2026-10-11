namespace Musoq.Plugins;

public partial class LibraryBase
{
    private const WindowEvaluationModes NumericWindowModes = WindowEvaluationModes.WholePartition |
        WindowEvaluationModes.Running | WindowEvaluationModes.RecomputeFrames;
}
