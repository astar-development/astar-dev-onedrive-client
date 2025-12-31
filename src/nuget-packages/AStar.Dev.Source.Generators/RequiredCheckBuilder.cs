namespace AStar.Dev.Source.Generators;

internal static class RequiredCheckBuilder
{
    public static string? Build(OptionsBindingGenerator.PropModel prop) => prop.Kind switch
    {
        OptionsBindingGenerator.SimpleKind.String => BuildStringRequiredCheck(prop),
        OptionsBindingGenerator.SimpleKind.Int32 => BuildInt32RequiredCheck(prop),
        OptionsBindingGenerator.SimpleKind.Boolean => BuildBooleanRequiredCheck(prop),
        _ => null
    };

    private static string BuildStringRequiredCheck(OptionsBindingGenerator.PropModel prop) => $"        if (string.IsNullOrWhiteSpace(opts.{prop.Name})) {{ ok = false; results.Add(new ValidationResult(\"{prop.Name} is required\", new[]{{\"\" + \"{prop.Name}\" + \"\"}})); }}";

    private static string BuildInt32RequiredCheck(OptionsBindingGenerator.PropModel prop) => $"        if (opts.{prop.Name} == default(int)) {{ ok = false; results.Add(new ValidationResult(\"{prop.Name} must be non-zero\", new[]{{\"\" + \"{prop.Name}\" + \"\"}})); }}";

    private static string BuildBooleanRequiredCheck(OptionsBindingGenerator.PropModel prop) => "        /* schema-required boolean: ensure it's explicitly set if that matters to you */";
}
