using System.Collections.Generic;
using System.Threading;

namespace Musoq.Converter;

public static class Musoq
{
    public static MusoqQueryBuilder Query(string query)
    {
        return new MusoqQueryBuilder(query);
    }

    public static MusoqSourceRows Source<T>(
        string schemaName,
        string sourceName,
        IEnumerable<IReadOnlyList<T>> chunks)
    {
        return MusoqSourceRows.Create(schemaName, sourceName, chunks);
    }

    public static ICompiledTypedQuery<TOut> Compile<TArg, TOut>(string query)
    {
        var builder = Query(query);
        return TypedShorthandSourceMapper
            .AddSource<TArg>(builder, 0)
            .Compile<TOut>();
    }

    public static ICompiledTypedQuery<TOut> Load<TOut>(CompiledTypedQueryArtifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        if (artifact.InMemorySourceSlots.Count == 0)
        {
            throw new InvalidOperationException(
                "Only typed artifacts produced by Musoq.Query(...).Source(...).CompileArtifact<TOut>() can be loaded through the public in-memory API.");
        }

        if (!artifact.HasMatchingInMemorySourceSlotIdentities())
        {
            throw new InvalidOperationException(
                "Typed query artifact source slot metadata does not match the runtime source slots embedded in the artifact.");
        }

        var factory = InstanceCreator.LoadTypedArtifactFactory<TOut>(
            artifact,
            NullLoggerResolver.Instance);
        return new PublicCompiledTypedQuery<TOut>(factory, new InMemorySourceBinding(artifact.InMemorySourceSlots));
    }

    public static IEnumerable<TOut> CompileAndRun<TArg, TOut>(
        string query,
        IEnumerable<IReadOnlyList<TArg>> source,
        CancellationToken token)
    {
        var builder = Query(query);
        return TypedShorthandSourceMapper
            .AddSource(builder, 0, source)
            .CompileAndRun<TOut>(token);
    }

    public static ICompiledTypedQuery<TOut> Compile<TArg1, TArg2, TOut>(string query)
    {
        var builder = Query(query);
        TypedShorthandSourceMapper.AddSource<TArg1>(builder, 0);
        return TypedShorthandSourceMapper
            .AddSource<TArg2>(builder, 1)
            .Compile<TOut>();
    }

    public static IEnumerable<TOut> CompileAndRun<TArg1, TArg2, TOut>(
        string query,
        IEnumerable<IReadOnlyList<TArg1>> source1,
        IEnumerable<IReadOnlyList<TArg2>> source2,
        CancellationToken token)
    {
        var builder = Query(query);
        TypedShorthandSourceMapper.AddSource(builder, 0, source1);
        return TypedShorthandSourceMapper
            .AddSource(builder, 1, source2)
            .CompileAndRun<TOut>(token);
    }

    public static ICompiledTypedQuery<TOut> Compile<TArg1, TArg2, TArg3, TOut>(string query)
    {
        var builder = Query(query);
        TypedShorthandSourceMapper.AddSource<TArg1>(builder, 0);
        TypedShorthandSourceMapper.AddSource<TArg2>(builder, 1);
        return TypedShorthandSourceMapper
            .AddSource<TArg3>(builder, 2)
            .Compile<TOut>();
    }

    public static IEnumerable<TOut> CompileAndRun<TArg1, TArg2, TArg3, TOut>(
        string query,
        IEnumerable<IReadOnlyList<TArg1>> source1,
        IEnumerable<IReadOnlyList<TArg2>> source2,
        IEnumerable<IReadOnlyList<TArg3>> source3,
        CancellationToken token)
    {
        var builder = Query(query);
        TypedShorthandSourceMapper.AddSource(builder, 0, source1);
        TypedShorthandSourceMapper.AddSource(builder, 1, source2);
        return TypedShorthandSourceMapper
            .AddSource(builder, 2, source3)
            .CompileAndRun<TOut>(token);
    }

    public static ICompiledTypedQuery<TOut> Compile<TArg1, TArg2, TArg3, TArg4, TOut>(string query)
    {
        var builder = Query(query);
        TypedShorthandSourceMapper.AddSource<TArg1>(builder, 0);
        TypedShorthandSourceMapper.AddSource<TArg2>(builder, 1);
        TypedShorthandSourceMapper.AddSource<TArg3>(builder, 2);
        return TypedShorthandSourceMapper
            .AddSource<TArg4>(builder, 3)
            .Compile<TOut>();
    }

    public static IEnumerable<TOut> CompileAndRun<TArg1, TArg2, TArg3, TArg4, TOut>(
        string query,
        IEnumerable<IReadOnlyList<TArg1>> source1,
        IEnumerable<IReadOnlyList<TArg2>> source2,
        IEnumerable<IReadOnlyList<TArg3>> source3,
        IEnumerable<IReadOnlyList<TArg4>> source4,
        CancellationToken token)
    {
        var builder = Query(query);
        TypedShorthandSourceMapper.AddSource(builder, 0, source1);
        TypedShorthandSourceMapper.AddSource(builder, 1, source2);
        TypedShorthandSourceMapper.AddSource(builder, 2, source3);
        return TypedShorthandSourceMapper
            .AddSource(builder, 3, source4)
            .CompileAndRun<TOut>(token);
    }
}
