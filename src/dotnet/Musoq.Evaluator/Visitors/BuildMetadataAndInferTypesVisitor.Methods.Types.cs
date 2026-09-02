using System.Reflection;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    /// <summary>
    ///     Contains context information needed for method resolution.
    /// </summary>
    private record struct MethodResolutionContext(
        string Alias,
        TableSymbol TableSymbol,
        (ISchema Schema, ISchemaTable Table, string TableName) SchemaTablePair,
        Type EntityType);

    private readonly record struct AggregateResolutionSignature(MethodInfo Method, MethodInfo SetMethod)
    {
        public bool Equals(AggregateResolutionSignature other)
        {
            return AreSameMethod(Method, other.Method) &&
                   AreSameMethod(SetMethod, other.SetMethod);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Method.Module, Method.MetadataToken, SetMethod.Module,
                SetMethod.MetadataToken);
        }

        private static bool AreSameMethod(MethodInfo left, MethodInfo right)
        {
            return left.Module.Equals(right.Module) && left.MetadataToken == right.MetadataToken;
        }
    }
}
