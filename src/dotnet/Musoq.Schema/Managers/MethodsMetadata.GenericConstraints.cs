using System.Linq;

namespace Musoq.Schema.Managers;

public partial class MethodsMetadata
{
    private static bool ConformsToConstructedConstraint(Type constraint, Type parameter, Type argument)
    {
        var specialized = SubstituteConstraintParameter(constraint, parameter, argument);
        if (specialized is null)
            return false;

        return specialized.ContainsGenericParameters || specialized.IsAssignableFrom(argument);
    }

    private static Type? SubstituteConstraintParameter(Type constraint, Type parameter, Type argument)
    {
        if (constraint == parameter)
            return argument;
        if (!constraint.IsGenericType)
            return constraint;

        var arguments = constraint.GetGenericArguments()
            .Select(item => SubstituteConstraintParameter(item, parameter, argument)).ToArray();
        if (arguments.Any(static item => item is null))
            return null;

        try { return constraint.GetGenericTypeDefinition().MakeGenericType(arguments!); }
        catch (ArgumentException) { return null; }
    }
}
