using Musoq.Converter.Build;

namespace Musoq.Converter
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
