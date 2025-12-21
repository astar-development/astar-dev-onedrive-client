$ErrorActionPreference = "Stop"

Write-Host "Restoring packages..."
dotnet restore

Write-Host "Building solution..."
dotnet build -c Debug

Write-Host "Applying EF migrations and ensuring database..."
# Ensure you have dotnet-ef installed or use the runtime migration approach in code
dotnet tool install --global dotnet-ef --version 8.0.0 -s nuget.org -v q
dotnet ef database update --project src\App.Infrastructure --startup-project src\App.UI.Avalonia

Write-Host "Running App.UI.Avalonia..."
dotnet run --project src\App.UI.Avalonia -c Debug
