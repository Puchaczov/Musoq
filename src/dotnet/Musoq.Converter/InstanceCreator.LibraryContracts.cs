using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Musoq.Plugins;
using Musoq.Schema;

namespace Musoq.Converter;

public static partial class InstanceCreator
{
    private static readonly ConditionalWeakTable<SqlFunctionDescriptor[], LibraryContractSignature> LibraryContracts = new();

    private static string CreateLibraryContractSignature(ISchema schema)
    {
        var declarations = ImmutableCollectionsMarshal.AsArray(schema.GetAllLibraryFunctions());
        return declarations is null ? "<empty>" : LibraryContracts.GetValue(declarations,
            static functions => new LibraryContractSignature(ComputeLibraryContractSignature(functions))).Value;
    }

    private static string ComputeLibraryContractSignature(SqlFunctionDescriptor[] functions)
    {
        var builder = new StringBuilder();
        foreach (var function in functions.OrderBy(static function => function.Kind)
                     .ThenBy(static function => function.Name, StringComparer.Ordinal)
                     .ThenBy(static function => function.Declaration.DeclaringType?.AssemblyQualifiedName, StringComparer.Ordinal)
                     .ThenBy(static function => function.Declaration.MetadataToken))
        {
            builder.Append(function.Name.Length).Append(':').Append(function.Name).Append('|')
                .Append(function.Kind).Append('|').Append(function.Visibility).Append('|')
                .Append(function.Declaration.DeclaringType?.AssemblyQualifiedName).Append('|')
                .Append(function.Declaration.Module.ModuleVersionId).Append('|')
                .Append(function.Declaration.MetadataToken).Append('|')
                .Append(function.ResultType.AssemblyQualifiedName).Append('|')
                .Append(function.ExecutionPermissions).Append('|').Append(function.EmptyResultBehavior).Append('|')
                .Append(function.KernelType?.AssemblyQualifiedName).Append('|').Append(function.WindowModes).Append('|')
                .Append(function.RankingKind).Append('|').Append(function.WindowContextType?.AssemblyQualifiedName).Append('|')
                .Append(function.WindowBatchKernelType?.AssemblyQualifiedName).Append('|')
                .Append(function.RequiresWindowOrdering).Append('|').Append(function.RequiresWindowPeerMetadata);
            foreach (var parameter in function.Parameters)
                builder.Append('|').Append(parameter.Type.AssemblyQualifiedName);
            builder.AppendLine();
        }
        return CompiledQueryArtifactSupport.ComputeHash(builder.ToString());
    }

    private sealed record LibraryContractSignature(string Value);
}
