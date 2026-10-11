namespace Musoq.Plugins;

internal static class WindowFrameValueOperations<T>
{
    public static T First(in WindowFrameContext<T> context)
        => context.IsEmpty ? default! : context.Read(context.FrameStart);

    public static T Last(in WindowFrameContext<T> context)
        => context.IsEmpty ? default! : context.Read(context.FrameEnd);

    public static T Nth(in WindowFrameContext<T> context, int position)
    {
        if (position <= 0 || context.IsEmpty || position > context.FrameEnd - context.FrameStart + 1)
            return default!;
        return context.Read(context.FrameStart + position - 1);
    }

    public static T Offset(in WindowFrameContext<T> context, long offset, T defaultValue)
    {
        var position = context.Position + offset;
        if (position < 0 || position >= context.Count)
            return defaultValue;
        return context.Read((int)position);
    }
}
