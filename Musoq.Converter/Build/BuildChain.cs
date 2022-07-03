namespace Musoq.Converter.Build
{
    public abstract class BuildChain
    {
        protected readonly BuildChain Successor;

        protected BuildChain(BuildChain successor)
        {
            Successor = successor;
        }

        public abstract void Build(BuildItems items);
    }
}
