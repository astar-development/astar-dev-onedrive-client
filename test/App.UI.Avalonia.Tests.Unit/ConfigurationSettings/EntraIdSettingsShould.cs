using App.UI.Avalonia.ConfigurationSettings;
using AStar.Dev.Utilities;

namespace App.UI.Avalonia.Tests.Unit.ConfigurationSettings;

public class EntraIdSettingsShould
{
    [Fact]
    public void ContainTheExpectedPropertiesWithTheExpectedValues()
        => new EntraIdSettings() { ClientId = "TestClientId", Scopes = ["TestScope", "TestScope1"], RedirectUri = "TestRedirectUri" }
            .ToJson()
            .ShouldMatchApproved();
}
