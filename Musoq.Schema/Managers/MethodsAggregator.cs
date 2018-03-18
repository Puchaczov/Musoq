using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Plugins;

namespace Musoq.Schema.Managers
{
    public class MethodsAggregator
    {
        private readonly Dictionary<Type, LibraryBase> _instantiatedObjects;
        private readonly MethodsManager _methsManager;
        private readonly PropertiesManager _propsManager;

        public MethodsAggregator(MethodsManager methsManager, PropertiesManager propsManager)
        {
            _methsManager = methsManager;
            _propsManager = propsManager;
            _instantiatedObjects = new Dictionary<Type, LibraryBase>();
        }

        public MethodInfo ResolveMethod(string name, Type[] args)
        {
            return _methsManager.GetMethod(name, args);
        }

        public bool TryResolveMethod(string name, Type[] types, out MethodInfo method)
        {
            return _methsManager.TryGetMethod(name, types, out method);
        }

        public MethodInfo ResolveProperty(string name)
        {
            return _propsManager.Properties.SingleOrDefault(f => f.Name == name);
        }

        public object GetObjectForTheMethod(MethodInfo method)
        {
            return InstantiateFromType(method.DeclaringType);
        }

        public object GetObjectForTheProperty(PropertyInfo property)
        {
            return InstantiateFromType(property.DeclaringType);
        }

        private object InstantiateFromType(Type type)
        {
            if (_instantiatedObjects.ContainsKey(type)) return _instantiatedObjects[type];

            var instance = (LibraryBase) Activator.CreateInstance(type);
            _instantiatedObjects.Add(type, instance);

            return instance;
        }
    }
}