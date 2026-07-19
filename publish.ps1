# Publishes Guardian.Service, Guardian.Tray and Guardian.Dashboard into one shared
# folder (dist/), the way they would sit together after a real installer runs.
# Guardian.Tray looks for the other two .exe files right next to itself, so this
# is the setup required to test GuardianSense as a single program.

$ErrorActionPreference = "Stop"

$dist = Join-Path $PSScriptRoot "dist"

if (Test-Path $dist) {
    Remove-Item $dist -Recurse -Force
}

New-Item -ItemType Directory -Path $dist | Out-Null

dotnet publish "$PSScriptRoot\Guardian.Service\Guardian.Service.csproj" -c Release -o $dist
dotnet publish "$PSScriptRoot\Guardian.Tray\Guardian.Tray.csproj" -c Release -o $dist
dotnet publish "$PSScriptRoot\Guardian.Dashboard\Guardian.Dashboard.csproj" -c Release -o $dist

Write-Host ""
Write-Host "Published GuardianSense to $dist"
Write-Host "Run $dist\Guardian.Tray.exe to start protection, the tray icon and the dashboard together."
