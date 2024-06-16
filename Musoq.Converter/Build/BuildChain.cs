namespace Musoq.Converter.Build
{
    public abstract class BuildChain(BuildChain successor)
    {
        protected readonly BuildChain Successor = successor;

        public abstract void Build(BuildItems items);
    }
}
