namespace Musoq.Plugins;

public partial class LibraryBase
{
    #pragma warning disable CS1591

    private static TResult AggregateDeclaration<TResult>()
    {
        return AggregateFunction.NotInvoked<TResult>();
    }
}
