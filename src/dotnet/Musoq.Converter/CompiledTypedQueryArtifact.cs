using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.CodeGeneration;
using Musoq.Schema;
using Musoq.Schema.Optimization;

namespace Musoq.Converter;

public sealed class CompiledTypedQueryArtifact : ICompiledTypedQueryArtifact
{
    public const int CurrentArtifactVersion = 1;

    private static readonly string CurrentEngineVersion = GetAssemblyVersion(typeof(InstanceCreator));
    private static readonly string CurrentRuntimeVersion = GetAssemblyVersion(typeof(global::Musoq.Evaluator.CompiledTypedQuery<>));
    private readonly byte[] _dllFile;
    private readonly byte[]? _pdbFile;
    private readonly IReadOnlyList<TypedArtifactSourceSlotIdentity> _sourceSlotIdentities;

    public CompiledTypedQueryArtifact(
        byte[] dllFile,
        byte[]? pdbFile,
        string runnableTypeName,
        QueryResultMode resultMode,
        Type outputType,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId,
        IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> sourceRuntimeSettingDescriptionsBySourceContextId,
        IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans,
        IReadOnlyList<ScriptParameterDefinition> parameterDefinitions)
        : this(
            dllFile,
            pdbFile,
            runnableTypeName,
            resultMode,
            outputType,
            sourceRuntimeSettingsBySourceContextId,
            sourceRuntimeSettingDescriptionsBySourceContextId,
            sourceExecutionPlans,
            parameterDefinitions,
            [])
    {
    }

    internal CompiledTypedQueryArtifact(
        byte[] dllFile,
        byte[]? pdbFile,
        string runnableTypeName,
        QueryResultMode resultMode,
        Type outputType,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId,
        IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> sourceRuntimeSettingDescriptionsBySourceContextId,
        IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans,
        IReadOnlyList<ScriptParameterDefinition> parameterDefinitions,
        IReadOnlyList<InMemorySourceSlot> inMemorySourceSlots)
    {
        _dllFile = CopyBytes(dllFile ?? throw new ArgumentNullException(nameof(dllFile)));
        _pdbFile = CopyBytesOrNull(pdbFile);
        RunnableTypeName = string.IsNullOrWhiteSpace(runnableTypeName)
            ? throw new ArgumentException("Runnable type name cannot be null or whitespace.", nameof(runnableTypeName))
            : runnableTypeName;
        ResultMode = resultMode;
        OutputType = outputType ?? throw new ArgumentNullException(nameof(outputType));
        OutputTypeName = outputType.AssemblyQualifiedName ?? outputType.FullName ?? outputType.Name;
        SourceRuntimeSettingsBySourceContextId = ArtifactMetadataSnapshot.CopySourceRuntimeSettings(
            sourceRuntimeSettingsBySourceContextId ??
            throw new ArgumentNullException(nameof(sourceRuntimeSettingsBySourceContextId)));
        SourceRuntimeSettingDescriptionsBySourceContextId = ArtifactMetadataSnapshot.CopySourceRuntimeSettingDescriptions(
            sourceRuntimeSettingDescriptionsBySourceContextId ??
            throw new ArgumentNullException(nameof(sourceRuntimeSettingDescriptionsBySourceContextId)));
        SourceExecutionPlans = ArtifactMetadataSnapshot.CopySourceExecutionPlans(
            sourceExecutionPlans ?? throw new ArgumentNullException(nameof(sourceExecutionPlans)));
        ParameterDefinitions = (parameterDefinitions ?? throw new ArgumentNullException(nameof(parameterDefinitions))).ToArray();
        ParameterContracts = ParameterDefinitions
            .Select(static definition => definition.Contract)
            .ToArray();
        InMemorySourceSlots = (inMemorySourceSlots ?? throw new ArgumentNullException(nameof(inMemorySourceSlots))).ToArray();
        ArtifactVersion = CurrentArtifactVersion;
        EngineVersion = CurrentEngineVersion;
        RuntimeVersion = CurrentRuntimeVersion;
        RuntimeContractSignature = RuntimeV2Contract.ContractSignature;
        _sourceSlotIdentities = Array.AsReadOnly(InMemorySourceSlots
            .Select(TypedArtifactSourceSlotIdentity.FromSlot)
            .ToArray());
    }

    public int ArtifactVersion { get; }

    public string EngineVersion { get; }

    public string RuntimeVersion { get; }

    public string RuntimeContractSignature { get; }

    public byte[] DllFile => CopyBytes(_dllFile);

    public byte[]? PdbFile => CopyBytesOrNull(_pdbFile);

    public string RunnableTypeName { get; }

    public QueryResultMode ResultMode { get; }

    public Type OutputType { get; }

    public string OutputTypeName { get; }

    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; }

    public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; }

    public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; }

    public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; }

    public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; }

    public IReadOnlyList<TypedArtifactSourceSlotIdentity> SourceSlotIdentities => _sourceSlotIdentities;

    internal IReadOnlyList<InMemorySourceSlot> InMemorySourceSlots { get; }

    internal byte[] DllFileUnsafe => _dllFile;

    internal byte[]? PdbFileUnsafe => _pdbFile;

    internal bool HasMatchingInMemorySourceSlotIdentities()
    {
        if (InMemorySourceSlots.Count != _sourceSlotIdentities.Count)
            return false;

        for (var i = 0; i < InMemorySourceSlots.Count; i++)
        {
            if (!_sourceSlotIdentities[i].Matches(InMemorySourceSlots[i]))
                return false;
        }

        return true;
    }

    private static byte[] CopyBytes(byte[] bytes)
    {
        return bytes.ToArray();
    }

    private static byte[]? CopyBytesOrNull(byte[]? bytes)
    {
        return bytes?.ToArray();
    }

    private static string GetAssemblyVersion(Type type)
    {
        var assemblyName = type.Assembly.GetName();
        var name = assemblyName.Name ?? type.Assembly.FullName ?? type.FullName ?? type.Name;
        var version = assemblyName.Version?.ToString() ?? "0.0.0.0";
        return $"{name}/{version}";
    }
}
