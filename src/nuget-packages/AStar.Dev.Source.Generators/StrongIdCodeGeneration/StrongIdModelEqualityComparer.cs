using System;
using System.Collections.Generic;

namespace AStar.Dev.Source.Generators.StrongIdCodeGeneration;

internal sealed class StrongIdModelEqualityComparer : IEqualityComparer<StrongIdModel>
{
    public static readonly StrongIdModelEqualityComparer Instance = new();

    public bool Equals(StrongIdModel? x, StrongIdModel? y)
        => ReferenceEquals(x, y) || (x is not null && y is not null && string.Equals(x.Namespace, y.Namespace, StringComparison.Ordinal) &&
               string.Equals(x.ModelName, y.ModelName, StringComparison.Ordinal) &&
               string.Equals(x.UnderlyingTypeDisplay, y.UnderlyingTypeDisplay, StringComparison.Ordinal) &&
               x.Accessibility == y.Accessibility);

    public int GetHashCode(StrongIdModel obj) => (obj.Namespace, obj.ModelName, obj.UnderlyingTypeDisplay, obj.Accessibility).GetHashCode();
}
