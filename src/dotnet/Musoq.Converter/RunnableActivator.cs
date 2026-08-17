using System.Linq.Expressions;

namespace Musoq.Converter;

internal static class RunnableActivator
{
    public static Func<TContract> Create<TContract>(Type runnableType)
        where TContract : class
    {
        ArgumentNullException.ThrowIfNull(runnableType);

        if (!typeof(TContract).IsAssignableFrom(runnableType))
            throw new InvalidOperationException($"Type {runnableType.FullName} does not implement {typeof(TContract).FullName}.");

        var constructor = runnableType.GetConstructor(Type.EmptyTypes);
        if (constructor == null)
            throw new InvalidOperationException($"Type {runnableType.FullName} does not expose a public parameterless constructor.");

        var create = Expression.New(constructor);
        var convert = Expression.Convert(create, typeof(TContract));
        return Expression.Lambda<Func<TContract>>(convert).Compile();
    }
}
