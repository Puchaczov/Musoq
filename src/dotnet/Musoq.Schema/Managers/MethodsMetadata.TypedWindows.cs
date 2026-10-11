using System.Linq;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;
using Musoq.Plugins;
using Musoq.Schema.Helpers;

namespace Musoq.Schema.Managers;

public partial class MethodsMetadata
{
    /// <summary>Resolves a window SQL declaration using its concrete argument signature.</summary>
    public bool TryGetWindowFunction(string name, Type[] argumentTypes, [NotNullWhen(true)] out MethodInfo? result)
    {
        var normalized = MethodNameNormalizer.Normalize(name);
        var candidates = GetAllFunctions().Where(function => function.Kind == SqlFunctionKind.Window &&
            function.Visibility == FunctionVisibility.Public && MethodNameNormalizer.Normalize(function.Name) == normalized &&
            function.KernelType is not null && function.Parameters.Count(static parameter => !parameter.Optional) <= argumentTypes.Length &&
            function.Parameters.Length >= argumentTypes.Length)
            .Select(function => new { function.Declaration, Cost = MeasureMethodCloseness(function.Declaration, argumentTypes) })
            .Where(static candidate => candidate.Cost != int.MaxValue).OrderBy(static candidate => candidate.Cost).ToArray();
        if (candidates.Length == 0)
        {
            result = null;
            return false;
        }
        if (candidates.Length > 1 && candidates[0].Cost == candidates[1].Cost)
            throw Musoq.Schema.Exceptions.MethodResolutionException.ForAmbiguousMethod(name,
                argumentTypes.Select(static type => type.Name).ToArray(),
                candidates.Where(candidate => candidate.Cost == candidates[0].Cost)
                    .Select(static candidate => candidate.Declaration.ToString()!).ToArray());
        result = candidates[0].Declaration;
        return true;
    }
}
