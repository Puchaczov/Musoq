using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Diagnostics;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Visitors;

internal static class WindowDeclarationBinding
{
    private static readonly Lazy<MethodsManager> StandardLibrary = new(CreateStandardLibrary);

    public static MethodInfo? ResolveStandard(string name, Type[] arguments)
        => StandardLibrary.Value.TryGetWindowFunction(name, arguments, out var declaration) ? declaration : null;

    public static void ValidateArguments(AccessMethodNode call, Node[] arguments, IEnumerable<ISchema> schemas)
    {
        var declarations = FindDeclarations(call.Name, schemas);
        // Legacy typed factories validate their separate argument interface during lowering.
        if (declarations.Any(static declaration => declaration.KernelType is null))
            return;
        if (declarations.All(static declaration => declaration.Declaration.DeclaringType == typeof(LibraryBase)) &&
            declarations.Any(static declaration => declaration.WindowContextType is not null ||
                declaration.RankingKind != WindowRankingKind.None))
            WindowFunctionArgumentValidation.Validate(call, arguments, static (code, message, context) =>
                throw new CannotResolveMethodException(message, code, context.SpanOrEmpty()));

        var counts = declarations.SelectMany(static declaration =>
                Enumerable.Range(declaration.Parameters.Count(static parameter => !parameter.Optional),
                    declaration.Parameters.Count(static parameter => parameter.Optional) + 1))
            .Distinct().Order().ToArray();
        if (counts.Length == 0 || counts.Contains(arguments.Length))
            return;
        var expected = counts.Length == 1 ? counts[0].ToString(System.Globalization.CultureInfo.InvariantCulture) :
            string.Join(", ", counts);
        throw new CannotResolveMethodException(
            $"Callable '{call.Name}' does not accept {arguments.Length} argument(s); expected {expected}.",
            DiagnosticCode.MQ3087_InvalidCallableArity, call.SpanOrEmpty());
    }

    private static SqlFunctionDescriptor[] FindDeclarations(string name, IEnumerable<ISchema> schemas)
    {
        var normalized = name.Replace("_", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
        bool Matches(SqlFunctionDescriptor function) => function.Kind == SqlFunctionKind.Window &&
            function.Visibility == FunctionVisibility.Public &&
            function.Name.Replace("_", string.Empty, StringComparison.Ordinal).ToUpperInvariant() == normalized;
        var declarations = schemas.SelectMany(static schema => schema.GetAllLibraryFunctions()).Where(Matches).ToArray();
        return declarations.Length > 0 ? declarations : StandardLibrary.Value.GetAllFunctions().Where(Matches).ToArray();
    }

    public static MethodInfo Bind(string name, Type[] arguments, IEnumerable<ISchema> schemas)
    {
        var candidates = new HashSet<MethodInfo>();
        foreach (var schema in schemas)
        {
            if (schema.TryResolveWindowFunction(name, arguments, out var declaration))
                candidates.Add(declaration);
        }
        if (candidates.Count > 1)
            throw new CannotResolveMethodException($"Window '{name}' has ambiguous library ownership. Qualify its source alias.",
                DiagnosticCode.MQ3035_AmbiguousMethodOwner, TextSpan.Empty);

        var selected = candidates.SingleOrDefault() ?? ResolveStandard(name, arguments) ??
                       throw new CannotResolveMethodException($"No public window declaration matches '{name}' and its SQL argument types.");
        if (!selected.IsGenericMethodDefinition)
            return selected;
        var inference = arguments.AsEnumerable();
        if (selected.GetCustomAttribute<WindowFunctionAttribute>()?.ContextType is not null)
            inference = inference.Take(1);
        return AggregateDeclarationSpecialization.Bind(selected, inference.ToArray());
    }

    private static MethodsManager CreateStandardLibrary()
    {
        var declarations = new MethodsManager();
        declarations.RegisterLibraries(new LibraryBase());
        return declarations;
    }
}
