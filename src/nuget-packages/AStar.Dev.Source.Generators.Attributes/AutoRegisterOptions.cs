namespace AStar.Dev.Source.Generators.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public class AutoRegisterOptionsAttribute(string? sectionName) : Attribute
{
    /// <summary>
    /// Gets the name of the configuration section associated with this instance.
    /// When not set, the section name defaults to the class or struct name.
    /// </summary>
    public string? SectionName { get; } = sectionName;
}
