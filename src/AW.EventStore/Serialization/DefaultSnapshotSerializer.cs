using AW.NamedTypes;
using System;
using System.Text.Json;

namespace AW.EventStore.Serialization;

public class DefaultSnapshotSerializer : ISnapshotSerializer
{
    private readonly TypeRegistry _typerRegistry;
    private readonly JsonSerializerOptions? _jsonSerializerOptions;

    public DefaultSnapshotSerializer(
        TypeRegistry? typerRegistry = null,
        JsonSerializerOptions? jsonSerializerOptions = null)
    {
        _typerRegistry = typerRegistry ?? TypeRegistry.Instance;
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    public object? Deserialize(string snapshotType, byte[] snapshot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(snapshotType, nameof(snapshotType));
        ArgumentNullException.ThrowIfNull(snapshot, nameof(snapshot));
        ArgumentOutOfRangeException.ThrowIfZero(snapshot.Length, nameof(snapshot));

        if (_typerRegistry.TryResolveType(snapshotType, out var clrType) == false || clrType == null)
            throw new InvalidOperationException($"Snapshot '{snapshotType}' is not mapped.");

        return JsonSerializer.Deserialize(snapshot, clrType, _jsonSerializerOptions);
    }

    public (string SnapshotType, byte[] Snapshot) Serialize(object snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot, nameof(snapshot));

        if (_typerRegistry.TryResolveName(snapshot.GetType(), out var snapshotName) == false || snapshotName == null)
            throw new InvalidOperationException($"Snapshot type '{snapshot.GetType()}' is not mapped.");

        return (
            snapshotName,
            JsonSerializer.SerializeToUtf8Bytes(snapshot, snapshot.GetType(), _jsonSerializerOptions));
    }
}
