# Secrets Management with User Secrets

## Overview

The OneDrive Sync application uses **User Secrets** for managing sensitive configuration data like API keys and client IDs. This keeps sensitive data out of source control while maintaining easy local development.

## Why User Secrets?

- **Security**: Sensitive data never committed to source control
- **Per-Developer**: Each developer can have their own secrets
- **Environment-Specific**: Different secrets for development, testing, production
- **Easy Setup**: Simple command-line configuration

## Setup Instructions

### 1. Initialize User Secrets

The project is already configured with a UserSecretsId. To set up your local secrets:

```bash
cd src/AStar.Dev.OneDrive.Client
dotnet user-secrets init
```

### 2. Configure Entra ID Client ID

The Microsoft Entra ID (Azure AD) Client ID is required for authentication:

```bash
dotnet user-secrets set "EntraId:ClientId" "YOUR-CLIENT-ID-HERE"
```

**To get a Client ID:**
1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **Microsoft Entra ID** > **App registrations**
3. Register a new application or use existing one
4. Copy the **Application (client) ID**
5. Configure redirect URI: `http://localhost`
6. Add required permissions:
   - `User.Read`
   - `Files.ReadWrite.All`
   - `offline_access`

### 3. View Current Secrets

```bash
dotnet user-secrets list
```

### 4. Remove a Secret

```bash
dotnet user-secrets remove "EntraId:ClientId"
```

### 5. Clear All Secrets

```bash
dotnet user-secrets clear
```

## Configuration Priority

Configuration sources are loaded in this order (later sources override earlier):

1. **appsettings.json** - Default configuration
2. **User Secrets** - Developer-specific overrides
3. **Environment Variables** - Runtime overrides

## Required Secrets

### Entra ID Configuration

| Secret Key | Description | Example |
|------------|-------------|---------|
| `EntraId:ClientId` | Azure AD Application (client) ID | `3057f494-687d-4abb-a653-4b8066230b6e` |

## Optional Secrets

### Future Configuration Options

| Secret Key | Description | Default |
|------------|-------------|---------|
| `EntraId:TenantId` | Azure AD Tenant ID (if multi-tenant) | `common` |
| `AStarDevOneDriveClient:ApiKey` | Future API key for cloud sync | N/A |

## Secrets File Location

User secrets are stored outside your project directory:

- **Windows**: `%APPDATA%\Microsoft\UserSecrets\astar-dev-onedrive-client-secrets\secrets.json`
- **macOS/Linux**: `~/.microsoft/usersecrets/astar-dev-onedrive-client-secrets/secrets.json`

### Manual Editing (Not Recommended)

You can manually edit the secrets.json file, but using `dotnet user-secrets` is recommended:

```json
{
  "EntraId": {
    "ClientId": "YOUR-CLIENT-ID-HERE"
  }
}
```

## Validation

The application validates required secrets at startup:

```csharp
// In HostExtensions.cs
private static void ValidateConfiguration(IConfiguration configuration)
{
    var clientId = configuration["EntraId:ClientId"];
    if (string.IsNullOrEmpty(clientId))
    {
        throw new InvalidOperationException(
            "EntraId:ClientId is not configured. " +
            "Run: dotnet user-secrets set \"EntraId:ClientId\" \"YOUR-CLIENT-ID\"");
    }
}
```

## Production Deployment

### Option 1: Azure App Configuration

```csharp
builder.Configuration.AddAzureAppConfiguration(options =>
{
    options.Connect(Environment.GetEnvironmentVariable("AppConfigConnectionString"))
           .Select(KeyFilter.Any, LabelFilter.Null)
           .ConfigureKeyVault(kv => kv.SetCredential(new DefaultAzureCredential()));
});
```

### Option 2: Environment Variables

```bash
# Windows
setx EntraId__ClientId "production-client-id"

# Linux/macOS
export EntraId__ClientId="production-client-id"
```

Note: Use double underscore `__` for nested configuration in environment variables.

### Option 3: Azure Key Vault

```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri("https://your-keyvault.vault.azure.net/"),
    new DefaultAzureCredential());
```

## Troubleshooting

### Error: "EntraId:ClientId is not configured"

**Solution**: Set up your user secrets:
```bash
dotnet user-secrets set "EntraId:ClientId" "YOUR-CLIENT-ID"
```

### Error: "User secrets ID not found"

**Solution**: Initialize user secrets:
```bash
dotnet user-secrets init
```

### Secrets Not Loading

1. Verify UserSecretsId in `.csproj`:
   ```xml
   <UserSecretsId>astar-dev-onedrive-client-secrets</UserSecretsId>
   ```

2. Check configuration in `Program.cs`:
   ```csharp
   cfg.AddUserSecrets<App>(true);
   ```

3. Verify secrets file exists:
   ```bash
   dotnet user-secrets list
   ```

## Best Practices

1. **Never commit secrets to source control**
   - Add `secrets.json` to `.gitignore` (if manually editing)
   - User secrets are stored outside project directory by default

2. **Use different ClientIds for different environments**
   - Development: Personal Azure AD app registration
   - Staging: Staging environment app registration
   - Production: Production environment app registration

3. **Rotate secrets regularly**
   - Update ClientId when compromised
   - Regenerate client secrets periodically

4. **Document required secrets**
   - Keep this document updated
   - Include setup instructions in team onboarding

5. **Validate configuration at startup**
   - Fail fast with clear error messages
   - Provide helpful remediation instructions

## Security Notes

- User secrets are **NOT encrypted** on disk
- User secrets are for **development only**
- Use Azure Key Vault or similar for production
- Never log or display secret values
- Limit access to production secret stores

## Additional Resources

- [Safe storage of app secrets in development](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [Configuration in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Azure Key Vault](https://azure.microsoft.com/services/key-vault/)
- [Microsoft Entra ID app registration](https://learn.microsoft.com/en-us/entra/identity-platform/quickstart-register-app)
