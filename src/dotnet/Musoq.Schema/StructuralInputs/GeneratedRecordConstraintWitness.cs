using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Musoq.Evaluator")]

namespace Musoq.Schema.StructuralInputs;

/// <summary>Compilation-only evidence for value-type constraints before a generated record's CLR type exists.</summary>
internal readonly struct GeneratedRecordConstraintWitness
{
    internal static Type Resolve(Type parameter, Type actual)
    {
        if (actual != typeof(StructuralValue))
            return actual;
        var constraints = parameter.GenericParameterAttributes;
        if ((constraints & GenericParameterAttributes.ReferenceTypeConstraint) != 0)
            return actual;
        return (constraints & (GenericParameterAttributes.NotNullableValueTypeConstraint |
            GenericParameterAttributes.DefaultConstructorConstraint)) != 0 ? typeof(GeneratedRecordConstraintWitness) : actual;
    }
}
