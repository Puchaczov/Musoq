using System;
using System.Reflection;

namespace Musoq.Schema.Managers
{
    public class MethodsAggregator
    {
        private readonly MethodsManager _methsManager;

        public MethodsAggregator(MethodsManager methsManager)
        {
            _methsManager = methsManager;
        }

        public bool TryResolveMethod(string name, Type[] types, Type entityType, out MethodInfo method)
        {
            return _methsManager.TryGetMethod(name, types, entityType, out method);
        }

        public bool TryResolveRawMethod(string name, Type[] types, out MethodInfo method)
        {
            return _methsManager.TryGetRawMethod(name, types, out method);
        }
    }
}