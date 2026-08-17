using System.IO;

namespace Musoq.Targets.CSharpClr;

internal sealed record ClrAssemblyExecutableArtifact : ExecutableQueryArtifact
{
    private readonly object _streamGate = new();
    private byte[]? _dllFile;
    private byte[]? _pdbFile;
    private Stream? _dllStream;
    private Stream? _pdbStream;

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

    internal ClrAssemblyExecutableArtifact(
        Stream dllStream,
        Stream? pdbStream,
        string runnableTypeName)
        : base(ExecutionTargetIds.CSharpClr)
    {
        if (dllStream is null || !dllStream.CanRead)
            throw new ArgumentException("CLR assembly stream must be readable.", nameof(dllStream));
        if (pdbStream is not null && !pdbStream.CanRead)
            throw new ArgumentException("CLR symbols stream must be readable.", nameof(pdbStream));
        if (string.IsNullOrWhiteSpace(runnableTypeName))
            throw new ArgumentException("Runnable type name cannot be empty.", nameof(runnableTypeName));

        _dllStream = dllStream;
        _pdbStream = pdbStream;
        RunnableTypeName = runnableTypeName;
    }

    public byte[] DllFile => (byte[])GetBytes(ref _dllFile, _dllStream).Clone();

    public byte[]? PdbFile => _pdbStream is null && _pdbFile is null
        ? null
        : (byte[])GetBytes(ref _pdbFile, _pdbStream).Clone();

    public string RunnableTypeName { get; }

    internal Stream OpenDllStream(out bool disposeAfterUse)
    {
        return OpenStream(_dllStream, _dllFile, out disposeAfterUse);
    }

    internal Stream? OpenPdbStream(out bool disposeAfterUse)
    {
        if ((_pdbStream is null && _pdbFile is null) ||
            (_pdbStream is { CanSeek: true, Length: 0 }) ||
            (_pdbFile is { Length: 0 }))
        {
            disposeAfterUse = false;
            return null;
        }

        return OpenStream(_pdbStream, _pdbFile, out disposeAfterUse);
    }

    private Stream OpenStream(Stream? stream, byte[]? bytes, out bool disposeAfterUse)
    {
        lock (_streamGate)
        {
            if (stream is not null)
            {
                if (stream is MemoryStream memoryStream &&
                    memoryStream.TryGetBuffer(out var buffer) &&
                    memoryStream.Length <= int.MaxValue)
                {
                    disposeAfterUse = true;
                    return new MemoryStream(
                        buffer.Array!,
                        buffer.Offset,
                        (int)memoryStream.Length,
                        writable: false,
                        publiclyVisible: true);
                }

                using var copy = new MemoryStream();
                if (stream.CanSeek)
                    stream.Position = 0;
                stream.CopyTo(copy);
                disposeAfterUse = true;
                return new MemoryStream(copy.ToArray(), writable: false);
            }

            disposeAfterUse = true;
            return new MemoryStream(bytes ?? [], writable: false);
        }
    }

    private byte[] GetBytes(ref byte[]? bytes, Stream? stream)
    {
        lock (_streamGate)
        {
            if (bytes is not null)
                return bytes;

            if (stream is null)
                return bytes = [];

            var originalPosition = stream.CanSeek ? stream.Position : 0;
            if (stream.CanSeek)
                stream.Position = 0;

            using var copy = new MemoryStream();
            stream.CopyTo(copy);
            if (stream.CanSeek)
                stream.Position = originalPosition;

            return bytes = copy.ToArray();
        }
    }
}

internal sealed record ClrLoadedExecutableArtifact(
    Type RunnableType,
    IDisposable? LifetimeOwner = null) : ExecutableQueryArtifact(ExecutionTargetIds.CSharpClr);
