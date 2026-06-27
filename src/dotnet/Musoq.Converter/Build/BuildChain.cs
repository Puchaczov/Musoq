namespace Musoq.Converter.Build;

public abstract class BuildChain(BuildChain? successor)
{
    protected BuildChain? Successor { get; } = successor;

    public abstract void Build(BuildItems items);
}
