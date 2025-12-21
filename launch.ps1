$ErrorActionPreference = "Stop"

Write-Host "Restoring packages..."
dotnet restore

Write-Host "Building solution..."
dotnet build -c Debug

Write-Host "Applying EF migrations and ensuring database..."
# Ensure you have dotnet-ef installed or use the runtime migration approach in code
dotnet tool install --global dotnet-ef --version 10.0.100 -s nuget.org -v q
dotnet ef database update --project src\AStar.Dev.OneDrive.Client.Infrastructure --startup-project src\AStar.Dev.OneDrive.Client.UI.Avalonia

Write-Host "Running AStar.Dev.OneDrive.Client..."
dotnet run --project src\AStar.Dev.OneDrive.Client -c Debug
