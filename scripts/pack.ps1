#!/usr/bin/env pwsh
# Local Velopack pack for GameLauncher
# Requires: Windows, .NET 10 SDK, `dotnet tool install -g vpk`
#
# Usage:
#   .\scripts\pack.ps1
#   .\scripts\pack.ps1 -Version 0.1.1
#   .\scripts\pack.ps1 -Version 0.1.1 -Upload   # also push to GitHub Release (needs gh auth / token)

param(
    [string]$Version = "",
    [string]$Runtime = "win-x64",
    [switch]$Upload,
    [string]$RepoUrl = "https://github.com/LongLongGames/GameLauncher",
    [string]$Token = $env:GITHUB_TOKEN
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path (Join-Path $Root "src/GameLauncher/GameLauncher.csproj"))) {
    $Root = Get-Location
}

$Csproj = Join-Path $Root "src/GameLauncher/GameLauncher.csproj"
$PackId = "LongLongGames.GameLauncher"
$MainExe = "GameLauncher.exe"

if (-not $Version) {
    [xml]$proj = Get-Content $Csproj
    $Version = ($proj.Project.PropertyGroup.Version | Select-Object -First 1)
    if (-not $Version) { $Version = "0.0.0-dev" }
}

Write-Host "==> Version: $Version  PackId: $PackId"

$PublishDir = Join-Path $Root "artifacts/publish"
$ReleasesDir = Join-Path $Root "artifacts/Releases"

if (Test-Path $PublishDir) { Remove-Item -Recurse -Force $PublishDir }
New-Item -ItemType Directory -Path $PublishDir -Force | Out-Null
New-Item -ItemType Directory -Path $ReleasesDir -Force | Out-Null

Write-Host "==> Publish"
dotnet publish $Csproj `
    -c Release `
    -r $Runtime `
    --self-contained true `
    -p:Version=$Version `
    -p:AssemblyVersion=$Version `
    -p:FileVersion=$Version `
    -o $PublishDir
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Ensure vpk is available
$vpk = Get-Command vpk -ErrorAction SilentlyContinue
if (-not $vpk) {
    Write-Host "==> Installing vpk global tool"
    dotnet tool install -g vpk --version 0.0.1298
}

Write-Host "==> vpk download (for delta)"
if ($Token) {
    vpk download github --repoUrl $RepoUrl --token $Token --outputDir $ReleasesDir
} else {
    vpk download github --repoUrl $RepoUrl --outputDir $ReleasesDir
}

Write-Host "==> vpk pack"
vpk pack `
    --packId $PackId `
    --packVersion $Version `
    --packDir $PublishDir `
    --mainExe $MainExe `
    --packTitle "GameLauncher" `
    --outputDir $ReleasesDir

Write-Host "==> Done. Artifacts in: $ReleasesDir"
Get-ChildItem $ReleasesDir | Format-Table Name, Length

if ($Upload) {
    if (-not $Token) {
        Write-Error "Upload requires -Token or env GITHUB_TOKEN"
    }
    Write-Host "==> vpk upload github"
    vpk upload github `
        --repoUrl $RepoUrl `
        --token $Token `
        --publish `
        --releaseName "GameLauncher v$Version" `
        --tag "v$Version" `
        --outputDir $ReleasesDir
}
