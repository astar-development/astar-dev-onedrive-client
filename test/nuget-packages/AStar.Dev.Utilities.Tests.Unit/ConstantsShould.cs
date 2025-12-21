namespace AStar.Dev.Utilities.Tests.Unit;

public sealed class ConstantsShould
{
    [Fact]
    public void ContainTheExpectedWebDeserialisationSettingsSetting()
        => Constants.WebDeserialisationSettings
            .ToJson()
            .Replace(@"""newLine"": ""\r\n"",", string.Empty)
            .Replace(@"""newLine"": ""\n"",", string.Empty) // Normalize to Linux-style
            .ShouldMatchApproved();
}
