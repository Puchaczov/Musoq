using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Musoq.Converter.Build;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.CodeGeneration;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Converter;

/// <summary>
/// Bounded process-local cache for the provider-neutral semantic handoff.
/// Entries contain no live provider or runtime binding. Every hit receives
/// fresh AST and dictionary ownership before planning resumes.
/// </summary>
internal static class SemanticTemplateCache
{
    private const int MaximumEntries = 64;
    private const int MaximumRetainedTextCharacters = 1_000_000;
    private static readonly ConcurrentDictionary<SemanticTemplateCacheKey, SemanticTemplateEntry> Entries = new();
    private static readonly object MutationGate = new();
    private static readonly Queue<SemanticTemplateCacheKey> InsertionOrder = new();
    private static readonly object FlightGate = new();
    private static readonly Dictionary<SemanticTemplateCacheKey, SemanticTemplateFlight> Flights = new();
    private static long _accessTick;
    private static int _retainedTextCharacters;

    internal static SemanticTemplateCacheKey? CreateKey(SemanticTemplateCacheInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (!IsEligible(input))
            return null;

        var providerType = input.SchemaProvider.GetType();
        var references = string.Join(
            "|",
            input.AdditionalReferenceTypes
                .Select(static type => type.AssemblyQualifiedName ?? type.FullName ?? type.Name)
                .OrderBy(static name => name, StringComparer.Ordinal));

        return new SemanticTemplateCacheKey(
            input.Script,
            ParsedQueryTemplateCache.DefaultParserContract,
            RuntimeV2Contract.ContractSignature,
            ExecutionSemanticsContract.Version1.Fingerprint,
            providerType.AssemblyQualifiedName ?? providerType.FullName ?? providerType.Name,
            InstanceCreator.CreateSemanticProviderContractSignatureForCache(input.SchemaProvider),
            CompilationOptionsFingerprint.Compute(input.CompilationOptions),
            input.ExecutionTarget,
            input.ResultMode,
            input.OutputType?.AssemblyQualifiedName ?? string.Empty,
            references,
            input.SchemaRegistryType);
    }

    internal static IDisposable Acquire(SemanticTemplateCacheKey key)
    {
        SemanticTemplateFlight flight;
        lock (FlightGate)
        {
            if (!Flights.TryGetValue(key, out flight!))
            {
                flight = new SemanticTemplateFlight();
                Flights.Add(key, flight);
            }

            flight.Waiters++;
        }

        Monitor.Enter(flight.Gate);
        return new SemanticTemplateFlightLease(key, flight);
    }

    internal static bool TryGet(SemanticTemplateCacheKey key, out SemanticBuildArtifacts artifacts)
    {
        if (!Entries.TryGetValue(key, out var entry))
        {
            artifacts = null!;
            return false;
        }

        entry.Touch();
        try
        {
            artifacts = Clone(entry.Artifacts);
            return true;
        }
        catch (Exception ex) when (IsCloneFailure(ex))
        {
            Remove(key, entry);
            artifacts = null!;
            return false;
        }
    }

    internal static void Publish(SemanticTemplateCacheKey key, SemanticBuildArtifacts artifacts)
    {
        ArgumentNullException.ThrowIfNull(artifacts);

        lock (MutationGate)
        {
            if (Entries.ContainsKey(key))
                return;

            try
            {
                _ = Clone(artifacts);
            }
            catch (Exception ex) when (IsCloneFailure(ex))
            {
                return;
            }

            EnsureCapacity(key.Script.Length);
            Entries[key] = new SemanticTemplateEntry(artifacts);
            InsertionOrder.Enqueue(key);
            _retainedTextCharacters += key.Script.Length;
        }
    }

    internal static SemanticTemplateCacheSnapshot Snapshot
    {
        get
        {
            lock (MutationGate)
                return new SemanticTemplateCacheSnapshot(Entries.Count, _retainedTextCharacters);
        }
    }

    internal static void Clear()
    {
        lock (MutationGate)
        {
            Entries.Clear();
            InsertionOrder.Clear();
            _retainedTextCharacters = 0;
        }
    }

    private static bool IsEligible(SemanticTemplateCacheInput input)
    {
        return !Debugger.IsAttached &&
               input.CompilationPurpose == CompilationPurpose.Execution &&
               input.ExecutionTarget == ExecutionTargetIds.CSharpClr &&
               input.ResultMode == QueryResultMode.Table &&
               input.OutputType is null &&
               !input.EmitPdb &&
               input.CompilationOptions.InstrumentationMode == QueryInstrumentationMode.Disabled &&
               input.InterpreterSourceCode is null &&
               !input.HasDeclaredSourceRuntimeSettings &&
               !input.HasSourceRuntimeSettingValues &&
               input.HasCustomMetadataVisitor == false &&
               input.SchemaProvider.GetType().IsVisible;
    }

    private static bool IsCloneFailure(Exception exception)
    {
        return exception is NotSupportedException or InvalidOperationException or ArgumentException;
    }

    private static void EnsureCapacity(int incomingTextLength)
    {
        while (Entries.Count >= MaximumEntries ||
               _retainedTextCharacters + incomingTextLength > MaximumRetainedTextCharacters)
        {
            if (InsertionOrder.Count == 0)
                return;

            var key = InsertionOrder.Dequeue();
            if (Entries.TryRemove(key, out var removed))
                _retainedTextCharacters = Math.Max(0, _retainedTextCharacters - key.Script.Length);
        }
    }

    private static void Remove(SemanticTemplateCacheKey key, SemanticTemplateEntry entry)
    {
        lock (MutationGate)
        {
            if (Entries.TryGetValue(key, out var current) && ReferenceEquals(current, entry))
            {
                Entries.TryRemove(key, out _);
                _retainedTextCharacters = Math.Max(0, _retainedTextCharacters - key.Script.Length);
            }
        }
    }

    private static SemanticBuildArtifacts Clone(SemanticBuildArtifacts source)
    {
        var parsed = ParsedQueryTemplateCache.CloneForCompilation(source.Phase.ParsedQuery);
        var normalized = ParsedQueryTemplateCache.CloneForCompilation(source.Phase.NormalizedQuery);
        var metadataRoot = ParsedQueryTemplateCache.CloneForCompilation(source.Phase.MetadataQuery);
        var rewritten = source.Phase.RewrittenQuery is { } rewrittenRoot
            ? ParsedQueryTemplateCache.CloneForCompilation(rewrittenRoot)
            : null;
        var transformed = ReferenceEquals(source.TransformedQueryTree, source.Phase.RewrittenQuery)
            ? rewritten ?? throw new InvalidOperationException("Semantic template is missing its rewritten query.")
            : ParsedQueryTemplateCache.CloneForCompilation(source.TransformedQueryTree);
        var metadataMap = CreateNodeMap(source.Phase.MetadataQuery, metadataRoot);
        var metadata = CloneMetadata(source.Phase.Metadata, metadataRoot, metadataMap);
        var phase = source.Phase with
        {
            ParsedQuery = parsed,
            NormalizedQuery = normalized,
            MetadataQuery = metadataRoot,
            RewrittenQuery = rewritten,
            Metadata = metadata
        };

        return source with
        {
            Phase = phase,
            TransformedQueryTree = transformed,
            UsedColumns = ReKey(source.UsedColumns, metadataMap),
            UsedWhereNodes = ReKey(source.UsedWhereNodes, metadataMap, metadataMap),
            SourcePlanRequestsPerSchema = ReKey(source.SourcePlanRequestsPerSchema, metadataMap),
            SourceContractDiagnosticLocationsPerSchema = ReKey(
                source.SourceContractDiagnosticLocationsPerSchema,
                metadataMap),
            PipelineInferredColumns = source.PipelineInferredColumns?.ToDictionary(
                static pair => pair.Key,
                static pair => pair.Value.ToArray(),
                StringComparer.Ordinal),
            PipelineUsedColumns = source.PipelineUsedColumns?.ToDictionary(
                static pair => pair.Key,
                static pair => (IReadOnlySet<string>)new HashSet<string>(pair.Value, StringComparer.Ordinal),
                StringComparer.Ordinal),
            CteExecutionPlan = null
        };
    }

    private static SemanticMetadataSnapshot CloneMetadata(
        SemanticMetadataSnapshot source,
        RootNode root,
        IReadOnlyDictionary<Node, Node> nodeMap)
    {
        return source with
        {
            Root = root,
            InferredColumns = ReKey(source.InferredColumns, nodeMap),
            InferredColumnsByAlias = source.InferredColumnsByAlias.ToDictionary(
                static pair => pair.Key,
                static pair => (IReadOnlyList<ISchemaColumn>)pair.Value.ToArray(),
                StringComparer.Ordinal),
            UsedColumns = ReKey(source.UsedColumns, nodeMap),
            UsedWhereNodes = ReKey(source.UsedWhereNodes, nodeMap, nodeMap),
            SourcePlanRequestsPerSchema = ReKey(source.SourcePlanRequestsPerSchema, nodeMap),
            SourceContractDiagnosticLocationsPerSchema = ReKey(
                source.SourceContractDiagnosticLocationsPerSchema,
                nodeMap),
            ResultShape = source.ResultShape with
            {
                GeneratedColumns = source.ResultShape.GeneratedColumns.ToDictionary(
                    static pair => pair.Key,
                    pair => (IReadOnlyList<FieldNode>)pair.Value.Select(field => MapNode<FieldNode>(field, nodeMap)).ToArray(),
                    StringComparer.Ordinal),
                SelectFieldAliases = source.ResultShape.SelectFieldAliases.ToDictionary(
                    static pair => pair.Key,
                    pair => MapNode(pair.Value, nodeMap),
                    StringComparer.OrdinalIgnoreCase),
                TheMostInnerIdentifier = source.ResultShape.TheMostInnerIdentifier is { } identifier
                    ? MapNode<IdentifierNode>(identifier, nodeMap)
                    : null
            }
        };
    }

    private static IReadOnlyDictionary<TKey, TValue> ReKey<TKey, TValue>(
        IReadOnlyDictionary<TKey, TValue> source,
        IReadOnlyDictionary<Node, Node> nodeMap)
        where TKey : Node
    {
        return source.ToDictionary(
            pair => (TKey)MapNode(pair.Key, nodeMap),
            static pair => pair.Value);
    }

    private static IReadOnlyDictionary<TKey, TValue> ReKey<TKey, TValue>(
        IReadOnlyDictionary<TKey, TValue> source,
        IReadOnlyDictionary<Node, Node> keyMap,
        IReadOnlyDictionary<Node, Node> valueMap)
        where TKey : Node
        where TValue : Node
    {
        return source.ToDictionary(
            pair => (TKey)MapNode(pair.Key, keyMap),
            pair => (TValue)MapNode(pair.Value, valueMap));
    }

    private static T MapNode<T>(T source, IReadOnlyDictionary<Node, Node> nodeMap)
        where T : Node
    {
        if (!nodeMap.TryGetValue(source, out var clone))
            throw new InvalidOperationException($"Semantic template node '{source.Id}' was not cloned.");

        return (T)clone;
    }

    private static Node MapNode(Node source, IReadOnlyDictionary<Node, Node> nodeMap)
    {
        return MapNode<Node>(source, nodeMap);
    }

    private static IReadOnlyDictionary<Node, Node> CreateNodeMap(RootNode source, RootNode clone)
    {
        var sourceNodes = EnumerateNodes(source).ToArray();
        var clonedNodes = EnumerateNodes(clone).ToArray();
        if (sourceNodes.Length != clonedNodes.Length)
            throw new InvalidOperationException("Semantic template clone changed AST shape.");

        var result = new Dictionary<Node, Node>(sourceNodes.Length, ReferenceEqualityComparer.Instance);
        for (var index = 0; index < sourceNodes.Length; index++)
        {
            if (sourceNodes[index].GetType() != clonedNodes[index].GetType())
                throw new InvalidOperationException("Semantic template clone changed AST node types.");

            result.Add(sourceNodes[index], clonedNodes[index]);
        }

        return result;
    }

    private static IEnumerable<Node> EnumerateNodes(Node root)
    {
        var pending = new Stack<Node>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            yield return current;
            var children = ParserNodeChildTraversal.EnumerateChildren(current).ToArray();
            for (var index = children.Length - 1; index >= 0; index--)
                pending.Push(children[index]);
        }
    }

    private sealed class SemanticTemplateEntry(SemanticBuildArtifacts artifacts)
    {
        private long _lastAccessTick = Interlocked.Increment(ref _accessTick);

        public SemanticBuildArtifacts Artifacts { get; } = artifacts;

        public void Touch() => Volatile.Write(ref _lastAccessTick, Interlocked.Increment(ref _accessTick));
    }

    private sealed class SemanticTemplateFlight
    {
        public object Gate { get; } = new();

        public int Waiters { get; set; }
    }

    private sealed class SemanticTemplateFlightLease(
        SemanticTemplateCacheKey key,
        SemanticTemplateFlight flight) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            Monitor.Exit(flight.Gate);
            lock (FlightGate)
            {
                if (--flight.Waiters == 0)
                    Flights.Remove(key);
            }
        }
    }
}

internal readonly record struct SemanticTemplateCacheKey(
    string Script,
    string ParserContract,
    string RuntimeContract,
    string ExecutionSemanticsContract,
    string ProviderType,
    string ProviderContract,
    string CompilationOptions,
    ExecutionTargetId ExecutionTarget,
    QueryResultMode ResultMode,
    string OutputType,
    string References,
    string SchemaRegistryType);

internal sealed record SemanticTemplateCacheInput(
    string Script,
    ISchemaProvider SchemaProvider,
    CompilationOptions CompilationOptions,
    CompilationPurpose CompilationPurpose,
    ExecutionTargetId ExecutionTarget,
    QueryResultMode ResultMode,
    Type? OutputType,
    bool EmitPdb,
    string? InterpreterSourceCode,
    bool HasDeclaredSourceRuntimeSettings,
    bool HasSourceRuntimeSettingValues,
    bool HasCustomMetadataVisitor,
    IReadOnlyList<Type> AdditionalReferenceTypes,
    string SchemaRegistryType);

internal readonly record struct SemanticTemplateCacheSnapshot(int Count, int RetainedTextCharacters);
