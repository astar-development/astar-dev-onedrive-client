namespace AStar.Dev.Source.Generators.Annotations;

public enum Lifetime { Singleton, Scoped, Transient }

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ServiceAttribute(Lifetime lifetime = Lifetime.Scoped) : Attribute
{
    /// <summary>
    /// Choose the service registration lifetime (default Scoped)
    /// </summary>
    public Lifetime Lifetime { get; } = lifetime;

    /// <summary>
    /// Override the service interface to register against. When not specified, the first Interface will be used
    /// </summary>
    public Type? As { get; set; }

    /// <summary>
    /// Also register the concrete type as itself (optional)
    /// </summary>
    public bool AsSelf { get; set; } = false;
}
