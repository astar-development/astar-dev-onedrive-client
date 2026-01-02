using System.ComponentModel.DataAnnotations;

namespace AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;

/// <summary>
///     Represents the Entra ID settings used for configuring the OneDrive client.
///     Provides properties to define various configuration parameters such as client identifiers,
///     download preferences, caching, paths, and scope definitions.
/// </summary>
public class EntraIdSettings
{
    /// <summary>
    ///    The configuration section name for Entra ID settings.
    /// </summary>
    public const string SectionName = "EntraId";

    /// <summary>
    ///     Gets or sets the client identifier used to authenticate the application
    ///     with the OneDrive API and related services. This value is required for
    ///     configuring the application to interact with the Microsoft Graph API.
    /// </summary>
    [Required]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the scopes that the application requires access to.
    /// </summary>
    [Required]
    public string[] Scopes { get; set; } = [];

    /// <summary>
    /// Gets the URI to which the authentication response will be redirected.
    /// </summary>
    [Required]
    [RegularExpression(@"^https?://.+", ErrorMessage = "RedirectUri must be a valid URI starting with http:// or https://")]
    public string RedirectUri { get; set; } = string.Empty;
}
