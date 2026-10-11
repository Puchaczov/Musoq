using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using Musoq.Plugins;

namespace Musoq.Schema.Managers;

public class MethodsManager : MethodsMetadata
{
    private static readonly ConcurrentDictionary<LibraryFunctionCatalog, RegistrationSnapshot> RegistrationSnapshots = new();
    private Dictionary<string, SqlFunctionDescriptor> _registrations = new(StringComparer.Ordinal);
    private LibraryFunctionCatalog? _initialCatalog;
    private ImmutableArray<SqlFunctionDescriptor> _selectedFunctions = [];

    /// <summary>Gets the selected declarations after inheritance and conflict resolution.</summary>
    public override ImmutableArray<SqlFunctionDescriptor> GetAllFunctions() => _selectedFunctions;

    public void RegisterLibraries(LibraryBase library)
    {
        ArgumentNullException.ThrowIfNull(library);
        var catalog = LibraryFunctionCatalog.For(library.GetType());
        if (_initialCatalog is null && _registrations.Count == 0)
        {
            RegistrationSnapshots.GetOrAdd(catalog, static value => CreateRegistrationSnapshot(value)).Apply(this);
            _initialCatalog = catalog;
            _selectedFunctions = catalog.Functions;
            return;
        }
        var selected = _initialCatalog is not null
            ? new Dictionary<string, SqlFunctionDescriptor>(_initialCatalog.Registrations, StringComparer.Ordinal)
            : new Dictionary<string, SqlFunctionDescriptor>(_registrations, StringComparer.Ordinal);
        foreach (var registration in catalog.Registrations)
            SelectRegistration(selected, registration);

        _initialCatalog = null;
        _registrations = selected;
        ClearRegistrations();
        foreach (var method in _registrations.Values.Where(static function => function.Kind != SqlFunctionKind.Window)
                     .Select(static function => function.Declaration).Distinct())
            RegisterMethodCore(method.Name, method);
        foreach (var function in _registrations.Values.Where(static function => function.Kind != SqlFunctionKind.Window &&
                     function.Name != function.Declaration.Name))
            RegisterMethodCore(function.Name, function.Declaration);
        _selectedFunctions = _registrations.Values.ToImmutableArray();
    }

    private static RegistrationSnapshot CreateRegistrationSnapshot(LibraryFunctionCatalog catalog)
    {
        var methods = catalog.Functions.Where(static function => function.Kind != SqlFunctionKind.Window)
            .Select(static function => function.Declaration).Distinct()
            .Select(static method => (method.Name, method));
        var aliases = catalog.Functions.Where(static function => function.Kind != SqlFunctionKind.Window &&
                function.Name != function.Declaration.Name)
            .Select(static function => (function.Name, function.Declaration));
        return new RegistrationSnapshot(methods.Concat(aliases));
    }

    private static void SelectRegistration(Dictionary<string, SqlFunctionDescriptor> selected,
        KeyValuePair<string, SqlFunctionDescriptor> registration)
    {
        if (!selected.TryGetValue(registration.Key, out var existing) || PreferDescendant(existing, registration.Value))
            selected[registration.Key] = registration.Value;
    }

    private static bool PreferDescendant(SqlFunctionDescriptor existing, SqlFunctionDescriptor candidate)
    {
        if (existing.Declaration == candidate.Declaration)
            return false;
        if (existing.Declaration.DeclaringType!.IsAssignableFrom(candidate.Declaration.DeclaringType))
            return true;
        if (candidate.Declaration.DeclaringType!.IsAssignableFrom(existing.Declaration.DeclaringType))
            return false;

        throw new InvalidOperationException($"Competing {candidate.Kind} declarations for SQL function '{candidate.Name}'.");
    }
}
