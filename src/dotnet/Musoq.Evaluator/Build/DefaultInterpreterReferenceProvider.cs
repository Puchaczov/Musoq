using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;
using Musoq.Evaluator.Runtime;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.Build;

internal sealed class DefaultInterpreterReferenceProvider : IInterpreterReferenceProvider
{
    private static readonly string[] AdditionalAssemblies =
    [
        "System.Memory.dll",
        "System.Buffers.dll",
        "System.Text.RegularExpressions.dll",
        "System.Dynamic.Runtime.dll",
        "System.Linq.Expressions.dll",
        "System.ObjectModel.dll",
        "System.Collections.dll"
    ];

    private readonly IMetadataReferenceCache _referenceCache;

    public DefaultInterpreterReferenceProvider(IMetadataReferenceCache referenceCache)
    {
        _referenceCache = referenceCache ?? throw new ArgumentNullException(nameof(referenceCache));
    }

    public IReadOnlyList<MetadataReference> GetReferences()
    {
        var references = new List<MetadataReference>();

        var schemaAssembly = typeof(IBytesInterpreter<>).Assembly;
        if (!string.IsNullOrEmpty(schemaAssembly.Location))
            references.Add(_referenceCache.GetOrCreate(schemaAssembly.Location));

        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location);
        if (runtimeDir is null)
            return references;

        foreach (var assemblyName in AdditionalAssemblies)
        {
            var assemblyPath = Path.Combine(runtimeDir, assemblyName);
            if (File.Exists(assemblyPath))
                references.Add(_referenceCache.GetOrCreate(assemblyPath));
        }

        return references;
    }
}
