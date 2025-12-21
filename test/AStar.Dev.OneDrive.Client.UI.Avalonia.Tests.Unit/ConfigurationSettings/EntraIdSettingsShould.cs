using AStar.Dev.Utilities;
using AStar.Dev.OneDrive.Client.ConfigurationSettings;

namespace AStar.Dev.OneDrive.Client.UI.Avalonia.Tests.Unit.ConfigurationSettings;

public class EntraIdSettingsShould
{
    [Fact]
    public void ContainTheExpectedPropertiesWithTheExpectedValues()
        => new EntraIdSettings() { ClientId = "TestClientId", Scopes = ["TestScope", "TestScope1"], RedirectUri = "TestRedirectUri" }
            .ToJson()
            .ShouldMatchApproved();
}
