using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using AStar.Dev.Utilities;

namespace AStar.Dev.OneDrive.Client.Services.Tests.Unit.ConfigurationSettings;

public class EntraIdSettingsShould
{
    [Fact]
    public void ContainExpectedPropertiesWithExpectedValues()
    {
        var settings = new EntraIdSettings
        {
            ClientId = "TestClientId",
            Scopes = ["TestScope", "TestScope1"],
            RedirectUri = "TestRedirectUri"
        };

        var result = settings.ToJson();

        result.ShouldMatchApproved();
    }
}
