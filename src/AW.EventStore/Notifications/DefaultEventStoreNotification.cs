using System;

namespace AW.EventStore.Notifications;

public class DefaultEventStoreNotification : IEventStoreNotifications
{
    public event EventHandler<StreamChangedNotification>? StreamChanged;

    public void Publish(StreamChangedNotification notification)
        => StreamChanged?.Invoke(this, notification);
}
