#!/usr/bin/env bash
set -euo pipefail

echo "Restoring packages..."
dotnet restore

echo "Building solution..."
dotnet build -c Debug

echo "Applying EF migrations and ensuring database..."
if ! command -v dotnet-ef >/dev/null 2>&1; then
  echo "Installing dotnet-ef tool..."
  dotnet tool install --global dotnet-ef --version 8.0.0
  export PATH="$PATH:$HOME/.dotnet/tools"
fi

dotnet ef database update --project src/AStar.Dev.OneDrive.Client.Infrastructure --startup-project src/AStar.Dev.OneDrive.Client

echo "Running AStar.Dev.OneDrive.Client..."
dotnet run --project src/AStar.Dev.OneDrive.Client -c Debug
