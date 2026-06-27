namespace Musoq.Evaluator.Helpers;

public sealed partial class WindowPartitionCountBuilder<TPartition>
{
    private int GetOrAddReferencePartition(TPartition partitionKey)
    {
        if (typeof(TPartition).IsValueType)
            return GetOrAddStructuralPartition(partitionKey);

        var referencePartition = TryGetReferencePartition(partitionKey);
        if (referencePartition >= 0)
            return referencePartition;

        var partitionIndex = GetOrAddStructuralPartition(partitionKey);
        AddReferencePartition(partitionKey, partitionIndex);
        return partitionIndex;
    }

    private int TryGetReferencePartition(TPartition partitionKey)
    {
        if (_referenceSlotPartitionIndexes == null)
        {
            var partitionCount = _partitionCount;
            if (partitionCount <= LinearReferencePartitionLimit)
            {
                var partitionKeys = _partitionKeys;
                for (var index = 0; index < partitionCount; index++)
                {
                    if (ReferenceEquals(partitionKeys[index], partitionKey))
                        return index;
                }

                return -1;
            }

            EnsureReferenceSlotCapacity(seedExistingPartitions: true);
        }

        var referenceSlotPartitionIndexes = _referenceSlotPartitionIndexes!;
        var referenceSlotKeys = _referenceSlotKeys!;
        var slot = GetReferenceSlot(partitionKey, referenceSlotPartitionIndexes.Length);
        while (true)
        {
            var partitionIndex = referenceSlotPartitionIndexes[slot] - 1;
            if (partitionIndex < 0)
                return -1;

            if (ReferenceEquals(referenceSlotKeys[slot], partitionKey))
                return partitionIndex;

            slot = (slot + 1) & (referenceSlotPartitionIndexes.Length - 1);
        }
    }

    private void AddReferencePartition(TPartition partitionKey, int partitionIndex)
    {
        if (_referenceSlotPartitionIndexes == null && _partitionCount <= LinearReferencePartitionLimit)
            return;

        if (_referenceSlotPartitionIndexes == null)
        {
            EnsureReferenceSlotCapacity(seedExistingPartitions: true);
            return;
        }

        EnsureReferenceSlotCapacity(seedExistingPartitions: false);

        var referenceSlotKeys = _referenceSlotKeys!;
        var referenceSlotPartitionIndexes = _referenceSlotPartitionIndexes!;
        var slot = GetReferenceSlot(partitionKey, referenceSlotPartitionIndexes.Length);
        while (referenceSlotPartitionIndexes[slot] != 0)
            slot = (slot + 1) & (referenceSlotPartitionIndexes.Length - 1);

        referenceSlotKeys[slot] = partitionKey;
        referenceSlotPartitionIndexes[slot] = partitionIndex + 1;
        _referenceSlotCount++;
    }

    private void EnsureReferenceSlotCapacity(bool seedExistingPartitions)
    {
        if (_referenceSlotPartitionIndexes == null)
        {
            var capacity = CreateSlotCapacity(_rowCount);
            _referenceSlotKeys = new TPartition[capacity];
            _referenceSlotPartitionIndexes = new int[capacity];
            if (seedExistingPartitions)
                SeedReferenceSlots();
            return;
        }

        if ((_referenceSlotCount + 1) * 2 < _referenceSlotPartitionIndexes.Length)
            return;

        GrowReferenceSlots();
    }

    private void SeedReferenceSlots()
    {
        var referenceSlotKeys = _referenceSlotKeys!;
        var referenceSlotPartitionIndexes = _referenceSlotPartitionIndexes!;
        var partitionKeys = _partitionKeys;
        for (var partitionIndex = 0; partitionIndex < _partitionCount; partitionIndex++)
        {
            var key = partitionKeys[partitionIndex];
            if (key == null)
                continue;

            var slot = GetReferenceSlot(key, referenceSlotPartitionIndexes.Length);
            while (referenceSlotPartitionIndexes[slot] != 0)
                slot = (slot + 1) & (referenceSlotPartitionIndexes.Length - 1);

            referenceSlotKeys[slot] = key;
            referenceSlotPartitionIndexes[slot] = partitionIndex + 1;
            _referenceSlotCount++;
        }
    }

    private void GrowReferenceSlots()
    {
        var previousKeys = _referenceSlotKeys!;
        var previousPartitionIndexes = _referenceSlotPartitionIndexes!;
        var newCapacity = previousPartitionIndexes.Length * 2;

        _referenceSlotKeys = new TPartition[newCapacity];
        _referenceSlotPartitionIndexes = new int[newCapacity];

        for (var slot = 0; slot < previousPartitionIndexes.Length; slot++)
        {
            var partitionIndex = previousPartitionIndexes[slot] - 1;
            if (partitionIndex < 0)
                continue;

            var key = previousKeys[slot];
            var newSlot = GetReferenceSlot(key, newCapacity);
            while (_referenceSlotPartitionIndexes[newSlot] != 0)
                newSlot = (newSlot + 1) & (newCapacity - 1);

            _referenceSlotKeys[newSlot] = key;
            _referenceSlotPartitionIndexes[newSlot] = partitionIndex + 1;
        }
    }
}
