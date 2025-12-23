using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using AStar.Dev.Utilities;

namespace AStar.Dev.OneDrive.Client.Services.Tests.Unit.ConfigurationSettings;

public class EntraIdSettingsShould
{
    [Fact]
    public void ContainTheExpectedPropertiesWithTheExpectedValues()
        => new EntraIdSettings() { ClientId = "TestClientId", Scopes = ["TestScope", "TestScope1"], RedirectUri = "TestRedirectUri" }
            .ToJson()
            .ShouldMatchApproved();
}
