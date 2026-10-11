using System.Collections.Generic;
using Musoq.Evaluator.Exceptions;

namespace Musoq.Evaluator.Helpers;

public static class ScriptParameterBinder
{
    public static T GetRequired<T>(ExecutionState state, string name) =>
        state.TypedParameters.GetRequired<T>(name);

    public static T GetOptional<T>(ExecutionState state, string name, T defaultValue) =>
        state.TypedParameters.Contains(name) ? state.TypedParameters.GetRequired<T>(name) : defaultValue;

    public static T? GetRequiredNullable<T>(ExecutionState state, string name) where T : struct =>
        state.TypedParameters.GetRequiredNullable<T>(name);

    public static T? GetOptionalNullable<T>(ExecutionState state, string name, T? defaultValue) where T : struct =>
        state.TypedParameters.Contains(name) ? state.TypedParameters.GetRequiredNullable<T>(name) : defaultValue;

    public static IReadOnlyList<T> GetRequiredCollection<T>(ExecutionState state, string name) =>
        state.TypedParameters.GetRequiredCollection<T>(name);

    public static void ValidateMembershipCollection<T>(IReadOnlyList<T>? values, string name)
    {
        if (values is null)
            throw ScriptParameterBindingException.NullNotAllowed(name, typeof(IReadOnlyList<T>));
    }

    public static void ValidateNoUnknownParameters(ExecutionState state, IReadOnlyCollection<string> declaredNames)
    {
        if (state.TypedParameters.Count == 0)
            return;
        var declared = new HashSet<string>(declaredNames, StringComparer.Ordinal);
        foreach (var name in state.TypedParameters.Names)
        {
            if (!declared.Contains(name))
                throw ScriptParameterBindingException.Unknown(name);
        }
    }
}
