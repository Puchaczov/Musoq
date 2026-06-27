using System.Runtime.CompilerServices;

namespace Musoq.Schema.Managers;

internal readonly struct MethodResolutionKey(string name, Type[] argTypes, Type? entityType)
    : IEquatable<MethodResolutionKey>
{
    private readonly string _name = name;
    private readonly Type[] _argTypes = argTypes;
    private readonly Type? _entityType = entityType;
    private readonly int _hashCode = ComputeHash(name, argTypes, entityType);

    public bool Equals(MethodResolutionKey other)
    {
        if (_hashCode != other._hashCode)
            return false;

        if (!string.Equals(_name, other._name, StringComparison.Ordinal))
            return false;

        if (_entityType != other._entityType)
            return false;

        if (_argTypes.Length != other._argTypes.Length)
            return false;

        for (var i = 0; i < _argTypes.Length; i++)
        {
            if (_argTypes[i] != other._argTypes[i])
                return false;
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return obj is MethodResolutionKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _hashCode;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ComputeHash(string name, Type[] argTypes, Type? entityType)
    {
        var hash = new HashCode();
        hash.Add(name, StringComparer.Ordinal);
        hash.Add(entityType);
        hash.Add(argTypes.Length);

        foreach (var argType in argTypes)
            hash.Add(argType);

        return hash.ToHashCode();
    }
}
