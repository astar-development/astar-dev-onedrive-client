using AStar.Dev.Source.Generators.Annotations;

namespace AStar.Dev.Source.Generators.Tests.Unit.StrongIdCodeGeneration;

// Test StrongId types used by the unit tests
[StrongId(typeof(string))]
public readonly partial record struct UserId;

[StrongId(typeof(int))]
public readonly partial record struct OrderId;

[StrongId(typeof(System.Guid))]
public readonly partial record struct EntityId;
