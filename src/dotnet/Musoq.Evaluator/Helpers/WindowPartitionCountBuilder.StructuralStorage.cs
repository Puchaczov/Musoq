using System.Runtime.CompilerServices;

namespace Musoq.Evaluator.Helpers;

public sealed partial class WindowPartitionCountBuilder<TPartition>
{
    private int GetOrAddStructuralPartition(TPartition partitionKey)
    {
        if ((_slotCount + 1) * 2 >= _slotPartitionIndexes.Length)
            GrowSlots();

        var slot = GetSlot(partitionKey, _slotPartitionIndexes.Length);
        while (true)
        {
            var partitionIndex = _slotPartitionIndexes[slot] - 1;
            if (partitionIndex < 0)
            {
                partitionIndex = _partitionCount;
                EnsurePartitionCapacity(partitionIndex + 1);
                _partitionCount++;
                _slotCount++;
                _partitionKeys[partitionIndex] = partitionKey;
                _slotKeys[slot] = partitionKey;
                _slotPartitionIndexes[slot] = partitionIndex + 1;
                return partitionIndex;
            }

            if (_comparer.Equals(_slotKeys[slot], partitionKey))
                return partitionIndex;

            slot = (slot + 1) & (_slotPartitionIndexes.Length - 1);
        }
    }

    private void GrowSlots()
    {
        var previousKeys = _slotKeys;
        var previousPartitionIndexes = _slotPartitionIndexes;
        var newCapacity = previousPartitionIndexes.Length * 2;

        _slotKeys = new TPartition[newCapacity];
        _slotPartitionIndexes = new int[newCapacity];

        for (var slot = 0; slot < previousPartitionIndexes.Length; slot++)
        {
            var partitionIndex = previousPartitionIndexes[slot] - 1;
            if (partitionIndex < 0)
                continue;

            var key = previousKeys[slot];
            var newSlot = GetSlot(key, newCapacity);
            while (_slotPartitionIndexes[newSlot] != 0)
                newSlot = (newSlot + 1) & (newCapacity - 1);

            _slotKeys[newSlot] = key;
            _slotPartitionIndexes[newSlot] = partitionIndex + 1;
        }
    }

    private void EnsurePartitionCapacity(int capacity)
    {
        if (capacity <= _counts.Length)
            return;

        var newSize = Math.Max(capacity, _counts.Length * 2);
        Array.Resize(ref _counts, newSize);
        Array.Resize(ref _partitionKeys, newSize);
    }

    private void EnsureComplete()
    {
        if (_addedCount != _rowCount)
            throw new InvalidOperationException("Window partition count builder must receive exactly rowCount rows.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetSlot(TPartition partitionKey, int capacity)
    {
        if (partitionKey is null)
            throw new InvalidOperationException("Null window partition keys must use the null partition slot.");

        return (_comparer.GetHashCode(partitionKey) & 0x7fffffff) & (capacity - 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetReferenceSlot(TPartition partitionKey, int capacity)
    {
        if (partitionKey is null)
            throw new InvalidOperationException("Null window partition keys must use the null partition slot.");

        return (RuntimeHelpers.GetHashCode(partitionKey) & 0x7fffffff) & (capacity - 1);
    }

    private static int CreateSlotCapacity(int rowCount)
    {
        var capacity = 2;
        var target = Math.Max(2, Math.Min(rowCount, 32));
        while (capacity < target)
            capacity *= 2;

        return capacity;
    }

    private static int CreatePartitionCapacity(int rowCount)
    {
        return Math.Max(1, Math.Min(rowCount, 16));
    }
}
