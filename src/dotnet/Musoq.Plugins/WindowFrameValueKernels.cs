namespace Musoq.Plugins;

#pragma warning disable CS1591

public static class FirstValueKernel<T> where T : struct
{
    public static T? Get(in WindowFrameContext<T?> context) => WindowFrameValueOperations<T?>.First(in context);
}
public static class FirstReferenceValueKernel<T> where T : class
{
    public static T? Get(in WindowFrameContext<T> context) => WindowFrameValueOperations<T>.First(in context);
}
public static class LastValueKernel<T> where T : struct
{
    public static T? Get(in WindowFrameContext<T?> context) => WindowFrameValueOperations<T?>.Last(in context);
}
public static class LastReferenceValueKernel<T> where T : class
{
    public static T? Get(in WindowFrameContext<T> context) => WindowFrameValueOperations<T>.Last(in context);
}
public static class NthValueKernel<T> where T : struct
{
    public static T? Get(in WindowFrameContext<T?> context, int position) => WindowFrameValueOperations<T?>.Nth(in context, position);
}
public static class NthReferenceValueKernel<T> where T : class
{
    public static T? Get(in WindowFrameContext<T> context, int position) => WindowFrameValueOperations<T>.Nth(in context, position);
}
public static class LagKernel<T> where T : struct
{
    public static T? Get(in WindowFrameContext<T?> context, int offset, T? defaultValue)
        => WindowFrameValueOperations<T?>.Offset(in context, -(long)offset, defaultValue);
}
public static class LagReferenceKernel<T> where T : class
{
    public static T? Get(in WindowFrameContext<T> context, int offset, T? defaultValue)
        => WindowFrameValueOperations<T>.Offset(in context, -(long)offset, defaultValue!);
}
public static class LeadKernel<T> where T : struct
{
    public static T? Get(in WindowFrameContext<T?> context, int offset, T? defaultValue)
        => WindowFrameValueOperations<T?>.Offset(in context, offset, defaultValue);
}
public static class LeadReferenceKernel<T> where T : class
{
    public static T? Get(in WindowFrameContext<T> context, int offset, T? defaultValue)
        => WindowFrameValueOperations<T>.Offset(in context, offset, defaultValue!);
}

public static class NtileKernel
{
    public static long Get(in WindowFrameContext<int> context)
    {
        var buckets = context.Read(0);
        if (buckets <= 0)
            return 1L;
        var rowsPerBucket = context.Count / buckets;
        var extra = context.Count % buckets;
        var largeBoundary = extra * (rowsPerBucket + 1);
        if (context.Position < largeBoundary)
            return context.Position / (rowsPerBucket + 1) + 1L;
        return (context.Position - largeBoundary) / rowsPerBucket + extra + 1L;
    }
}
