using Microsoft.Extensions.Configuration;

namespace AStar.Dev.OneDrive.Client.FromV3.Authentication;

/// <summary>
/// Configuration settings for MSAL authentication.
/// </summary>
public sealed class AuthConfiguration
{
    /// <summary>
    /// Gets or sets the Azure AD client ID for the application.
    /// </summary>
    public required string ClientId { get; init; }

    /// <summary>
    /// Gets or sets the redirect URI for OAuth callbacks.
    /// </summary>
    public required string RedirectUri { get; init; }

    /// <summary>
    /// Gets or sets the Microsoft Graph API scopes required for OneDrive access.
    /// </summary>
    public required string[] Scopes { get; init; }

    /// <summary>
    /// Gets or sets the authority URL for Microsoft identity platform.
    /// </summary>
    public required string Authority { get; init; }

    /// <summary>
    /// Loads authentication configuration from IConfiguration.
    /// </summary>
    /// <param name="configuration">The configuration source.</param>
    /// <returns>Configured AuthConfiguration instance.</returns>
    public static AuthConfiguration LoadFromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        IConfigurationSection authSection = configuration.GetSection("Authentication");
        if(!authSection.Exists())
        {
            throw new InvalidOperationException("Authentication configuration section not found. Ensure appsettings.json contains an 'Authentication' section.");
        }

        var clientId = authSection["ClientId"];
        return string.IsNullOrWhiteSpace(clientId)
            ? throw new InvalidOperationException("Authentication:ClientId is not configured. Please set it in appsettings.json or user secrets.")
            : new AuthConfiguration
            {
                ClientId = clientId,
                RedirectUri = authSection["RedirectUri"] ?? "http://localhost",
                Authority = authSection["Authority"] ?? "https://login.microsoftonline.com/common",
                Scopes = authSection.GetSection("Scopes").Get<string[]>() ?? ["Files.ReadWrite", "User.Read", "offline_access"]
            };
    }
}
