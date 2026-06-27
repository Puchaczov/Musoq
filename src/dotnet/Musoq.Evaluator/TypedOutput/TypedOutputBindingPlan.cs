using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Musoq.Evaluator.TypedOutput;

internal sealed class TypedOutputBindingPlan
{
    public TypedOutputBindingPlan(
        Type outputType,
        IReadOnlyList<TypedOutputColumn> columns,
        ConstructorInfo? constructor,
        IReadOnlyList<TypedOutputConstructorBinding> constructorBindings,
        IReadOnlyList<TypedOutputMemberBinding> memberBindings)
    {
        OutputType = outputType;
        Columns = columns.ToArray();
        Constructor = constructor;
        ConstructorBindings = constructorBindings.ToArray();
        MemberBindings = memberBindings.ToArray();
    }

    public Type OutputType { get; }

    public IReadOnlyList<TypedOutputColumn> Columns { get; }

    public ConstructorInfo? Constructor { get; }

    public IReadOnlyList<TypedOutputConstructorBinding> ConstructorBindings { get; }

    public IReadOnlyList<TypedOutputMemberBinding> MemberBindings { get; }
}
