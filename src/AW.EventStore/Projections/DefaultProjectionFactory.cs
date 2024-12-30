using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AW.EventStore.Projections;

public class DefaultProjectionFactory : IProjectionFactory
{
    private readonly IEventStore _eventStore;
    private readonly IEventStoreNotifications _notifications;
    private readonly ILogger<DefaultProjectionFactory> _logger;

    public DefaultProjectionFactory(
        ILogger<DefaultProjectionFactory> logger,
        IEventStore eventStore,
        IEventStoreNotifications notifications)
    {
        _logger = logger;
        _eventStore = eventStore;
        _notifications = notifications;
    }

    public Task BeginProjection(
        string identifier, IProjector projector, CancellationToken cancel,
        int batchSize = 10000, IProjectionCheckpointStorage? storage = null,
        ProjectionStateChangedCallback? stateChangedCallback = null)
        => Task.Run(async () =>
        {
            _logger.LogInformation("Starting projection {identifier}.", identifier);
            stateChangedCallback?.Invoke(ProjectionStates.Starting);

            try
            {
                var signal = new AsyncAutoResetEvent(true);
                IEventId? checkpoint = null;

                if (storage == null || await storage.Exists() == false)
                {
                    _logger.LogInformation("No checkpoint found for projection {identifier}. Purging projection data.", identifier);
                    await projector.Purge(cancel);
                }

                if (storage != null && await storage.Exists())
                {
                    _logger.LogInformation("Checkpoint found for projection {identifier}. Loading last checkpoint.", identifier);
                    checkpoint = await storage.Load();
                }

                void onStreamChanged(object? _, StreamChangedNotification __)
                    => signal.Set();

                _notifications.StreamChanged += onStreamChanged;

                try
                {
                    while (cancel.IsCancellationRequested == false)
                    {
                        await signal.WaitAsync(cancel);

                        while (cancel.IsCancellationRequested == false)
                        {
                            stateChangedCallback?.Invoke(ProjectionStates.FetchingEvents);

                            var events = await _eventStore
                                .StreamEvents(checkpoint, batchSize, cancel);

                            if (events == null || events.Any() == false)
                                break;

                            stateChangedCallback?.Invoke(ProjectionStates.ProjectingEvents);

                            await projector.Project([..events], cancel);

                            stateChangedCallback?.Invoke(ProjectionStates.SavingCheckpoint);

                            checkpoint = events.Last().EventId;
                            if (storage != null)
                                await storage.Save(checkpoint);
                        }

                        stateChangedCallback?.Invoke(ProjectionStates.WaitingForEvents);
                    }
                }
                finally
                {
                    _notifications.StreamChanged -= onStreamChanged;
                }
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, "Error occured in projection {identifier}.", identifier);
                stateChangedCallback?.Invoke(ProjectionStates.ProjectionError);
                throw;
            }
            finally
            {
                _logger.LogInformation("Projection {identifier} exited.", identifier);
                stateChangedCallback?.Invoke(ProjectionStates.Terminated);
            }
        });
}
