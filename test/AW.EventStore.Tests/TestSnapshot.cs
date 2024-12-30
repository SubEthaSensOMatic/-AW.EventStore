using AW.NamedTypes;

namespace AW.EventStore.Tests;

[NamedType("test-snapshot")]
public readonly record struct TestSnapshot(string Name, int Age);
