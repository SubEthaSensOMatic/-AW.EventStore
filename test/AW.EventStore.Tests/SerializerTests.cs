using AW.EventStore.Serialization;
using AW.NamedTypes;

namespace AW.EventStore.Tests;

[NamedType("test-event")]
public readonly record struct TestEvent(string Name, int Age);

[NamedType("test-snapshot")]
public readonly record struct TestSnapshot(string Name, int Age);

[TestClass]
public sealed class SerializerTests
{
    [TestMethod]
    public void SerializePayload()
    {
        var reg = new TypeRegistry();
        reg.AutoRegisterTypes(typeof(TestEvent).Assembly);
        var serializer = new DefaultEventPayloadSerializer(reg);
        
        var (eventType, data) = serializer.Serialize(new TestEvent("Test", 23));

        Assert.AreEqual("test-event", eventType);
        
        var deserialized = serializer.Deserialize(eventType, data);

        Assert.IsNotNull(deserialized);

        var testEvent = (TestEvent)deserialized;

        Assert.AreEqual("Test", testEvent.Name);
        Assert.AreEqual(23, testEvent.Age);
    }

    [TestMethod]
    public void SerializeSnapshot()
    {
        var reg = new TypeRegistry();
        reg.AutoRegisterTypes(typeof(TestEvent).Assembly);
        var serializer = new DefaultSnapshotSerializer(reg);

        var (snapshotType, data) = serializer.Serialize(new TestSnapshot("Test", 23));

        Assert.AreEqual("test-snapshot", snapshotType);

        var deserialized = serializer.Deserialize(snapshotType, data);

        Assert.IsNotNull(deserialized);

        var testSnapshot = (TestSnapshot)deserialized;

        Assert.AreEqual("Test", testSnapshot.Name);
        Assert.AreEqual(23, testSnapshot.Age);
    }
}
