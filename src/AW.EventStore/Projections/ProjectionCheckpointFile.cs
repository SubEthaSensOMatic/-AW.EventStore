using System;
using System.IO;
using System.Threading.Tasks;

namespace AW.EventStore.Projections;

public class ProjectionCheckpointFile : IProjectionCheckpointStorage
{
    private readonly IEventIdSerializer _serializer;
    private readonly string _fileName;

    public ProjectionCheckpointFile(IEventIdSerializer serializer, string fileName)
    {
        ArgumentNullException.ThrowIfNull(serializer, nameof(serializer));
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName, nameof(fileName));
        
        _serializer = serializer;
        _fileName = fileName;
    }

    public Task<bool> Exists()
        => Task.FromResult(File.Exists(_fileName));

    public async Task<IEventId?> Load()
    {
        if (File.Exists(_fileName) == false)
            return null;

        var bytes = await File.ReadAllBytesAsync(_fileName);

        return _serializer.Deserialize(bytes);
    }

    public Task Save(IEventId? checkpoint)
    {
        var bytes = checkpoint == null
            ? []
            : _serializer.Serialize(checkpoint);

        return File.WriteAllBytesAsync(_fileName, bytes ?? []);
    }
}
