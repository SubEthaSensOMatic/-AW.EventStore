using AW.NamedTypes;

namespace AW.EventStore.Tests;

[NamedType("test-event")]
public readonly record struct TestEvent(string Name, int Age);
