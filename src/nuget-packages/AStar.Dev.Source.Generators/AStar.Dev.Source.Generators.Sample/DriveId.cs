namespace AStar.Dev.Source.Generators.Sample;

public readonly partial record struct DriveId
{
    public string Value { get; }

    public DriveId(string value)
    {
        if(string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Drive ID cannot be null or empty", nameof(value));
        Value = value;
    }

    public static implicit operator string(DriveId id) => id.Value;
    public override string ToString() => Value;
}
