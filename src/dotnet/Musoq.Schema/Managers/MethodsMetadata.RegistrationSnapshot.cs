using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Musoq.Schema.Managers;

public partial class MethodsMetadata
{
    private bool _sharedRegistrations;

    protected sealed class RegistrationSnapshot
    {
        private readonly Dictionary<string, List<MethodInfo>> _methods;
        private readonly Dictionary<(string Name, MethodInfo Method), int> _indexes;
        private readonly Dictionary<string, string> _normalizedNames;

        internal RegistrationSnapshot(IEnumerable<(string Name, MethodInfo Method)> registrations)
        {
            var metadata = new MethodsMetadata();
            foreach (var registration in registrations)
                metadata.RegisterMethodCore(registration.Name, registration.Method);
            _methods = metadata._methods;
            _indexes = metadata._methodIndexCache;
            _normalizedNames = metadata._normalizedToOriginalMethodNames;
        }

        internal void Apply(MethodsMetadata metadata)
        {
            metadata.ClearRegistrations();
            metadata._methods = _methods;
            metadata._methodIndexCache = _indexes;
            metadata._normalizedToOriginalMethodNames = _normalizedNames;
            metadata._sharedRegistrations = true;
        }
    }

    private void EnsureWritableRegistrations()
    {
        if (!_sharedRegistrations)
            return;
        _methods = _methods.ToDictionary(static pair => pair.Key,
            static pair => new List<MethodInfo>(pair.Value), _methods.Comparer);
        _methodIndexCache = new(_methodIndexCache);
        _normalizedToOriginalMethodNames = new(_normalizedToOriginalMethodNames,
            _normalizedToOriginalMethodNames.Comparer);
        _sharedRegistrations = false;
    }
}
