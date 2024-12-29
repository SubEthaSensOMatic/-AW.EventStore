using AW.NamedTypes;
using System;
using System.Text.Json;

namespace AW.EventStore.Serialization;

public class DefaultEventPayloadSerializer : IEventPayloadSerializer
{
    private readonly TypeRegistry _typerRegistry;
    private readonly JsonSerializerOptions? _jsonSerializerOptions;

    public DefaultEventPayloadSerializer(
        TypeRegistry? typerRegistry = null,
        JsonSerializerOptions? jsonSerializerOptions = null)
    {
        _typerRegistry = typerRegistry ?? TypeRegistry.Instance;
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    public object? Deserialize(string eventType, byte[] payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType, nameof(eventType));
        ArgumentNullException.ThrowIfNull(payload, nameof(payload));
        ArgumentOutOfRangeException.ThrowIfZero(payload.Length, nameof(payload));

        if (_typerRegistry.TryResolveType(eventType, out var clrType) == false || clrType == null)
            throw new InvalidOperationException($"Event '{eventType}' is not mapped.");

        return JsonSerializer.Deserialize(payload, clrType, _jsonSerializerOptions);
    }

    public (string EventType, byte[] Payload) Serialize(object payload)
    {
        ArgumentNullException.ThrowIfNull(payload, nameof(payload));

        if (_typerRegistry.TryResolveName(payload.GetType(), out var eventType) == false || eventType == null)
            throw new InvalidOperationException($"Event type '{payload.GetType()}' is not mapped.");

        return (
            eventType,
            JsonSerializer.SerializeToUtf8Bytes(payload, payload.GetType(), _jsonSerializerOptions));
    }
}
