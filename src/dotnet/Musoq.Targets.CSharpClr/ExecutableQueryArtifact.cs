using System;

namespace Musoq.Targets.CSharpClr;

internal sealed record ClrAssemblyExecutableArtifact : ExecutableQueryArtifact
{
    private readonly byte[] _dllFile;
    private readonly byte[]? _pdbFile;

    public ClrAssemblyExecutableArtifact(byte[] dllFile, byte[]? pdbFile, string runnableTypeName)
        : base(ExecutionTargetIds.CSharpClr)
    {
        ArgumentNullException.ThrowIfNull(dllFile);
        if (dllFile.Length == 0)
            throw new ArgumentException("CLR assembly payload cannot be empty.", nameof(dllFile));
        if (string.IsNullOrWhiteSpace(runnableTypeName))
            throw new ArgumentException("Runnable type name cannot be empty.", nameof(runnableTypeName));

        _dllFile = (byte[])dllFile.Clone();
        _pdbFile = pdbFile is null ? null : (byte[])pdbFile.Clone();
        RunnableTypeName = runnableTypeName;
    }

    public byte[] DllFile => (byte[])_dllFile.Clone();

    public byte[]? PdbFile => _pdbFile is null ? null : (byte[])_pdbFile.Clone();

    public string RunnableTypeName { get; }
}

internal sealed record ClrLoadedExecutableArtifact(
    Type RunnableType,
    IDisposable? LifetimeOwner = null) : ExecutableQueryArtifact(ExecutionTargetIds.CSharpClr);
