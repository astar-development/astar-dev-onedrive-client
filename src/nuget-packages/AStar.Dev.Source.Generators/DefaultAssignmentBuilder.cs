namespace AStar.Dev.Source.Generators;

internal static class DefaultAssignmentBuilder
{
    public static string? Build(OptionsBindingGenerator.PropModel prop, string defaultLiteral) => prop.Kind switch
    {
        OptionsBindingGenerator.SimpleKind.String => BuildStringAssignment(prop, defaultLiteral),
        OptionsBindingGenerator.SimpleKind.Int32 => BuildInt32Assignment(prop, defaultLiteral),
        OptionsBindingGenerator.SimpleKind.Boolean => BuildBooleanAssignment(prop, defaultLiteral),
        _ => null
    };

    private static string BuildStringAssignment(OptionsBindingGenerator.PropModel prop, string defaultLiteral)
    {
        var escapedValue = StringEscaper.ToCSharpString(defaultLiteral);
        return $"        if (string.IsNullOrWhiteSpace(opts.{prop.Name})) opts.{prop.Name} = {escapedValue};";
    }

    private static string? BuildInt32Assignment(OptionsBindingGenerator.PropModel prop, string defaultLiteral) => !int.TryParse(defaultLiteral, out _)
            ? null
            : $"        if (opts.{prop.Name} == default(int)) opts.{prop.Name} = {defaultLiteral};";

    private static string? BuildBooleanAssignment(OptionsBindingGenerator.PropModel prop, string defaultLiteral)
    {
        if(!bool.TryParse(defaultLiteral, out _))
            return null;

        var lowerValue = defaultLiteral.ToLowerInvariant();
        return $"        /* boolean default from schema */ if (opts.{prop.Name} == default(bool)) opts.{prop.Name} = {lowerValue};";
    }
}
