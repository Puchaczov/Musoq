using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    private const WindowEvaluationModes ValueWindowModes = WindowEvaluationModes.WholePartition |
        WindowEvaluationModes.Running | WindowEvaluationModes.RecomputeFrames | WindowEvaluationModes.Batch;

    #pragma warning disable CS1591

    [WindowFunction(KernelType = typeof(FirstValueKernel<>), ContextType = typeof(WindowFrameContext<>), Modes = ValueWindowModes)]
    public T? FirstValue<T>(T? value) where T : struct => AggregateFunction.NotInvoked<T?>();

    [WindowFunction(Name = "FirstValue", KernelType = typeof(FirstReferenceValueKernel<>), ContextType = typeof(WindowFrameContext<>), Modes = ValueWindowModes)]
    public T? FirstReferenceValue<T>(T value) where T : class => AggregateFunction.NotInvoked<T?>();

    [WindowFunction(KernelType = typeof(LastValueKernel<>), ContextType = typeof(WindowFrameContext<>), Modes = ValueWindowModes)]
    public T? LastValue<T>(T? value) where T : struct => AggregateFunction.NotInvoked<T?>();

    [WindowFunction(Name = "LastValue", KernelType = typeof(LastReferenceValueKernel<>), ContextType = typeof(WindowFrameContext<>), Modes = ValueWindowModes)]
    public T? LastReferenceValue<T>(T value) where T : class => AggregateFunction.NotInvoked<T?>();

    [WindowFunction(KernelType = typeof(NthValueKernel<>), ContextType = typeof(WindowFrameContext<>), Modes = ValueWindowModes)]
    public T? NthValue<T>(T? value, int position) where T : struct => AggregateFunction.NotInvoked<T?>();

    [WindowFunction(Name = "NthValue", KernelType = typeof(NthReferenceValueKernel<>), ContextType = typeof(WindowFrameContext<>), Modes = ValueWindowModes)]
    public T? NthReferenceValue<T>(T value, int position) where T : class => AggregateFunction.NotInvoked<T?>();

    [WindowFunction(KernelType = typeof(LagKernel<>), ContextType = typeof(WindowFrameContext<>), RequiresOrderBy = true, Modes = ValueWindowModes)]
    public T? Lag<T>(T? value, int offset = 1, T? defaultValue = default) where T : struct => AggregateFunction.NotInvoked<T?>();

    [WindowFunction(Name = "Lag", KernelType = typeof(LagReferenceKernel<>), ContextType = typeof(WindowFrameContext<>), RequiresOrderBy = true, Modes = ValueWindowModes)]
    public T? LagReference<T>(T value, int offset = 1, T? defaultValue = default) where T : class => AggregateFunction.NotInvoked<T?>();

    [WindowFunction(KernelType = typeof(LeadKernel<>), ContextType = typeof(WindowFrameContext<>), RequiresOrderBy = true, Modes = ValueWindowModes)]
    public T? Lead<T>(T? value, int offset = 1, T? defaultValue = default) where T : struct => AggregateFunction.NotInvoked<T?>();

    [WindowFunction(Name = "Lead", KernelType = typeof(LeadReferenceKernel<>), ContextType = typeof(WindowFrameContext<>), RequiresOrderBy = true, Modes = ValueWindowModes)]
    public T? LeadReference<T>(T value, int offset = 1, T? defaultValue = default) where T : class => AggregateFunction.NotInvoked<T?>();

    [WindowFunction(KernelType = typeof(NtileKernel), IgnoresFrame = true, ContextType = typeof(WindowFrameContext<>), RequiresOrderBy = true, Modes = ValueWindowModes)]
    public long Ntile(int buckets) => AggregateFunction.NotInvoked<long>();
}
