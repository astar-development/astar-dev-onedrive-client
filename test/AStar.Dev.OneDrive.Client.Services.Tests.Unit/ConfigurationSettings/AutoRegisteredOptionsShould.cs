using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using AStar.Dev.Source.Generators.OptionsBindingGeneration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AStar.Dev.OneDrive.Client.Services.Tests.Unit.ConfigurationSettings;

public sealed class AutoRegisteredOptionsShould
{
    [Fact]
    public void RegisterApplicationSettingsWithCorrectConfiguration()
    {
        IConfiguration config = CreateConfiguration();
        var services = new ServiceCollection();

        _ = services.AddAutoRegisteredOptions(config);
        ServiceProvider provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<ApplicationSettings>>();
        options.Value.ShouldNotBeNull();
        options.Value.CacheTag.ShouldBe(2);
        options.Value.ApplicationVersion.ShouldBe("1.2.3");
        options.Value.UserPreferencesPath.ShouldBe("C:\\TestPath");
        options.Value.UserPreferencesFile.ShouldBe("test-prefs.json");
    }

    [Fact]
    public void RegisterEntraIdSettingsWithCorrectConfiguration()
    {
        IConfiguration config = CreateConfiguration();
        var services = new ServiceCollection();

        _ = services.AddAutoRegisteredOptions(config);
        ServiceProvider provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<EntraIdSettings>>();
        options.Value.ShouldNotBeNull();
        options.Value.ClientId.ShouldBe("test-client-id");
        options.Value.RedirectUri.ShouldBe("http://localhost");
        options.Value.Scopes.ShouldBe(new[] { "Files.ReadWrite", "User.Read" });
    }

    [Fact]
    public void ValidateApplicationSettingsOnStartup()
    {
        Dictionary<string, string?> invalidConfig = new()
        {
            ["AStarDevOneDriveClient:CacheTag"] = "0",
            ["AStarDevOneDriveClient:ApplicationVersion"] = "invalid-version",
            ["AStarDevOneDriveClient:UserPreferencesPath"] = "",
            ["AStarDevOneDriveClient:UserPreferencesFile"] = ""
        };
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(invalidConfig)
            .Build();
        var services = new ServiceCollection();
        _ = services.AddAutoRegisteredOptions(config);
        ServiceProvider provider = services.BuildServiceProvider();

        Exception? exception = Record.Exception(() =>
            provider.GetRequiredService<IOptions<ApplicationSettings>>().Value);

        exception.ShouldNotBeNull();
        exception.ShouldBeOfType<OptionsValidationException>();
        var validationException = (OptionsValidationException)exception;
        validationException.Failures.ShouldContain(f => f.Contains("CacheTag"));
        validationException.Failures.ShouldContain(f => f.Contains("ApplicationVersion"));
    }

    [Fact]
    public void ValidateEntraIdSettingsOnStartup()
    {
        Dictionary<string, string?> invalidConfig = new()
        {
            ["EntraId:ClientId"] = "",
            ["EntraId:RedirectUri"] = "not-a-uri",
            ["EntraId:Scopes:0"] = ""
        };
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(invalidConfig)
            .Build();
        var services = new ServiceCollection();
        _ = services.AddAutoRegisteredOptions(config);
        ServiceProvider provider = services.BuildServiceProvider();

        Exception? exception = Record.Exception(() =>
            provider.GetRequiredService<IOptions<EntraIdSettings>>().Value);

        exception.ShouldNotBeNull();
        exception.ShouldBeOfType<OptionsValidationException>();
        var validationException = (OptionsValidationException)exception;
        validationException.Failures.ShouldContain(f => f.Contains("ClientId"));
        validationException.Failures.ShouldContain(f => f.Contains("RedirectUri"));
    }

    [Fact]
    public void AllowMultipleRetrievalsOfSameOptions()
    {
        IConfiguration config = CreateConfiguration();
        var services = new ServiceCollection();
        _ = services.AddAutoRegisteredOptions(config);
        ServiceProvider provider = services.BuildServiceProvider();

        var options1 = provider.GetRequiredService<IOptions<ApplicationSettings>>();
        var options2 = provider.GetRequiredService<IOptions<ApplicationSettings>>();

        options1.Value.ShouldBeSameAs(options2.Value);
    }

    private static IConfiguration CreateConfiguration()
    {
        Dictionary<string, string?> configData = new()
        {
            ["AStarDevOneDriveClient:CacheTag"] = "2",
            ["AStarDevOneDriveClient:ApplicationVersion"] = "1.2.3",
            ["AStarDevOneDriveClient:UserPreferencesPath"] = "C:\\TestPath",
            ["AStarDevOneDriveClient:UserPreferencesFile"] = "test-prefs.json",
            ["AStarDevOneDriveClient:DatabaseName"] = "test-db.db",
            ["AStarDevOneDriveClient:OneDriveRootDirectory"] = "TestOneDrive",
            ["AStarDevOneDriveClient:CachePrefix"] = "test-cache",
            ["AStarDevOneDriveClient:RedirectUri"] = "http://localhost",
            ["EntraId:ClientId"] = "test-client-id",
            ["EntraId:RedirectUri"] = "http://localhost",
            ["EntraId:Scopes:0"] = "Files.ReadWrite",
            ["EntraId:Scopes:1"] = "User.Read"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }
}
