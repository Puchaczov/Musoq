namespace Musoq.Evaluator.Utils
{
    public class ScopeWalker
    {
        private readonly Scope _scope;
        private int _childIndex;
        private readonly ScopeWalker _parent = null;

        public Scope Scope => _scope;


        public ScopeWalker(Scope scope)
        {
            _scope = scope;
        }

        private ScopeWalker(Scope scope, ScopeWalker parent)
            : this(scope)
        {
            _parent = parent;
        }

        public ScopeWalker NextChild()
        {
            return new ScopeWalker(_scope.Child[_childIndex++], this);
        }

        public ScopeWalker Child()
        {
            return new ScopeWalker(_scope.Child[_childIndex], this);
        }

        public ScopeWalker PrevChild()
        {
            return new ScopeWalker(_scope.Child[_childIndex--], this);
        }

        public ScopeWalker Parent()
        {
            return _parent;
        }
    }
}