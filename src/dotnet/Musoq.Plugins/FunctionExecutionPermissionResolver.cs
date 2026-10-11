using System.Reflection;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

/// <summary>Resolves explicit declarations without inferring permission from purity or merge support.</summary>
public static class FunctionExecutionPermissionResolver
{
    /// <summary>Reads method permissions and exact declaring-library defaults.</summary>
    public static FunctionExecutionPermissions Resolve(MethodInfo method, SqlFunctionKind kind,
        BindableMethodAttribute declaration)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(declaration);
        if (declaration.HasExplicitExecutionPermissions)
            return declaration.ExecutionPermissions;
        var defaults = method.DeclaringType?.GetCustomAttribute<LibraryExecutionDefaultsAttribute>(inherit: false);
        if (defaults is null)
            return FunctionExecutionPermissions.None;
        return kind switch
        {
            SqlFunctionKind.Scalar => defaults.ScalarPermissions,
            SqlFunctionKind.Aggregate => defaults.AggregatePermissions,
            SqlFunctionKind.Window => defaults.WindowPermissions,
            _ => FunctionExecutionPermissions.None
        };
    }
}
