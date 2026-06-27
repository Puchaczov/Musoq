using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Musoq.Evaluator.Helpers;

public sealed partial class WindowPartitionCountBuilder<TPartition>
{
    private const int LinearReferencePartitionLimit = 16;

    private readonly int _rowCount;
    private readonly EqualityComparer<TPartition> _comparer;
    private readonly bool _useReferenceCache;
    private readonly int[] _rowPartitions;
    private TPartition[] _partitionKeys;
    private TPartition[] _slotKeys;
    private int[] _slotPartitionIndexes;
    private TPartition[]? _referenceSlotKeys;
    private int[]? _referenceSlotPartitionIndexes;
    private int[] _counts;
    private int _slotCount;
    private int _referenceSlotCount;
    private int _partitionCount;
    private int _nullPartitionIndex = -1;
    private int _addedCount;

    public WindowPartitionCountBuilder(int rowCount)
    {
        _rowCount = rowCount;
        _comparer = EqualityComparer<TPartition>.Default;
        _useReferenceCache = !typeof(TPartition).IsValueType;
        _rowPartitions = new int[rowCount];
        _partitionKeys = new TPartition[CreatePartitionCapacity(rowCount)];
        _slotKeys = new TPartition[CreateSlotCapacity(rowCount)];
        _slotPartitionIndexes = new int[_slotKeys.Length];
        _counts = new int[_partitionKeys.Length];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(TPartition? partitionKey, bool includeValue, int rowIndex)
    {
        _addedCount++;
        AddUnchecked(partitionKey, includeValue, rowIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddUnchecked(TPartition? partitionKey, bool includeValue, int rowIndex)
    {
        if (partitionKey == null)
        {
            var nullPartitionIndex = GetOrAddNullPartition();
            _rowPartitions[rowIndex] = nullPartitionIndex;
            if (includeValue)
                _counts[nullPartitionIndex]++;
            return;
        }

        var partitionIndex = GetOrAddPartition(partitionKey);

        _rowPartitions[rowIndex] = partitionIndex;
        if (includeValue)
            _counts[partitionIndex]++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddReference(TPartition? partitionKey, bool includeValue, int rowIndex)
    {
        _addedCount++;
        AddReferenceUnchecked(partitionKey, includeValue, rowIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddReferenceUnchecked(TPartition? partitionKey, bool includeValue, int rowIndex)
    {
        if (partitionKey == null)
        {
            var nullPartitionIndex = GetOrAddNullPartition();
            _rowPartitions[rowIndex] = nullPartitionIndex;
            if (includeValue)
                _counts[nullPartitionIndex]++;
            return;
        }

        var partitionIndex = GetOrAddReferencePartition(partitionKey);

        _rowPartitions[rowIndex] = partitionIndex;
        if (includeValue)
            _counts[partitionIndex]++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int[] ToResult()
    {
        EnsureComplete();
        return ToResultUnchecked();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int[] ToResultUnchecked()
    {
        var result = new int[_rowCount];
        for (var rowIndex = 0; rowIndex < _rowCount; rowIndex++)
        {
            var partitionIndex = _rowPartitions[rowIndex];
            result[rowIndex] = _counts[partitionIndex];
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int[] ToResultInPlaceUnchecked()
    {
        for (var rowIndex = 0; rowIndex < _rowCount; rowIndex++)
        {
            var partitionIndex = _rowPartitions[rowIndex];
            _rowPartitions[rowIndex] = _counts[partitionIndex];
        }

        return _rowPartitions;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public WindowPartitionCountResult ToCountResult()
    {
        EnsureComplete();
        return ToCountResultUnchecked();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public WindowPartitionCountResult ToCountResultUnchecked()
    {
        return new WindowPartitionCountResult(_rowPartitions, _counts);
    }

    private int GetOrAddNullPartition()
    {
        if (_nullPartitionIndex >= 0)
            return _nullPartitionIndex;

        var partitionIndex = _partitionCount;
        EnsurePartitionCapacity(partitionIndex + 1);
        _partitionCount++;
        _nullPartitionIndex = partitionIndex;
        return partitionIndex;
    }

    private int GetOrAddPartition(TPartition partitionKey)
    {
        return _useReferenceCache
            ? GetOrAddReferencePartition(partitionKey)
            : GetOrAddStructuralPartition(partitionKey);
    }


}
