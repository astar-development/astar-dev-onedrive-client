using AStar.Dev.OneDrive.Client.Core.ConfigurationSettings;

namespace AStar.Dev.OneDrive.Client.Core.Tests.Unit.ConfigurationSettings;

public class MsalConfigurationSettingsShould
{
    [Fact]
    public void CreateInstanceWithExpectedPropertiesAndValues()
    {
        // Arrange
        var clientId = "test-client-id";
        var redirectUri = "http://mock-redirect-uri";
        var graphUri = "https://graph.mock-uri.com";
        var scopes = new[] { "scope1", "scope2" };

        // Act
        var sut = new MsalConfigurationSettings(clientId, redirectUri, graphUri, scopes);

        // Assert
        sut.ClientId.ShouldBe(clientId);
        sut.RedirectUri.ShouldBe(redirectUri);
        sut.GraphUri.ShouldBe(graphUri);
        sut.Scopes.ShouldBeEquivalentTo(scopes);
    }
}