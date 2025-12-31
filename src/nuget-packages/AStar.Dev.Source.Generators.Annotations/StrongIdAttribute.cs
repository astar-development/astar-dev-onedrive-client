namespace AStar.Dev.Source.Generators.Annotations;

[AttributeUsage(AttributeTargets.Struct)]
public sealed class StrongIdAttribute(Type underlying) : Attribute
{
    /// <summary>
    /// Underlying CLR type. Defaults to System.Guid.
    /// Examples: "System.Guid", "int", "long", "string".
    /// </summary>
    public Type Underlying { get; } = underlying;

    public StrongIdAttribute()
        : this(typeof(Guid))
    {
    }
}
