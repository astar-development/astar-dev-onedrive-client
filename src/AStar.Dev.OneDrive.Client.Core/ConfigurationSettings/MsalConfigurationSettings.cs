
namespace AStar.Dev.OneDrive.Client.Core.ConfigurationSettings;

public record MsalConfigurationSettings
(
    string ClientId,
    string RedirectUri,
    string GraphUri,
    string[] Scopes,
    string CachePrefix
);
