<#
.SYNOPSIS
  生成 LocalCDN config/{game}/version.json（供游戏 Release CI 调用）

.EXAMPLE
  pwsh ./generate-version-json.ps1 `
    -GameId match3 `
    -Version 1.0.2 `
    -Platform windows `
    -Channel official `
    -FullPackagePath "D:\build\Game_Setup_1.0.2.exe" `
    -FullCdnRelativePath "download/match3/windows/official/Game_Setup_1.0.2.exe" `
    -OutFile "D:\LongLongGames\LocalCDN\data\config\match3\version.json"
#>
param(
    [Parameter(Mandatory = $true)][string]$GameId,
    [Parameter(Mandatory = $true)][string]$Version,
    [string]$Platform = "windows",
    [string]$Channel = "official",
    [Parameter(Mandatory = $true)][string]$FullPackagePath,
    [Parameter(Mandatory = $true)][string]$FullCdnRelativePath,
    [string]$MinSupported = "",
    [string]$ExeRelative = "Game.exe",
    [string]$Changelog = "",
    # 可选：补丁列表 JSON 文件，数组元素 { from, to, path }，path 为本地文件用于算 hash
    [string]$PatchesJson = "",
    [Parameter(Mandatory = $true)][string]$OutFile
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-Sha256Hex([string]$Path) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $fs = [System.IO.File]::OpenRead($Path)
        try {
            $hash = $sha.ComputeHash($fs)
            return ([System.BitConverter]::ToString($hash) -replace "-", "").ToLowerInvariant()
        }
        finally { $fs.Dispose() }
    }
    finally { $sha.Dispose() }
}

if (-not (Test-Path -LiteralPath $FullPackagePath)) {
    throw "Full package not found: $FullPackagePath"
}

$fullSize = (Get-Item -LiteralPath $FullPackagePath).Length
$fullSha = Get-Sha256Hex $FullPackagePath

$patches = @()
if ($PatchesJson -and (Test-Path -LiteralPath $PatchesJson)) {
    $raw = Get-Content -LiteralPath $PatchesJson -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach ($p in $raw) {
        $local = $p.local_path
        if (-not $local) { $local = $p.LocalPath }
        $cdnPath = $p.path
        if (-not $cdnPath) { $cdnPath = $p.Path }
        if (-not $local -or -not (Test-Path -LiteralPath $local)) {
            throw "Patch local file missing: $local"
        }
        $patches += [ordered]@{
            from   = [string]$p.from
            to     = [string]$p.to
            path   = [string]$cdnPath
            size   = [int64](Get-Item -LiteralPath $local).Length
            sha256 = (Get-Sha256Hex $local)
        }
    }
}

$manifest = [ordered]@{
    game_id        = $GameId
    platform       = $Platform
    channel        = $Channel
    latest         = $Version
    packages       = [ordered]@{
        full = [ordered]@{
            version = $Version
            path    = $FullCdnRelativePath.Replace("\", "/")
            size    = [int64]$fullSize
            sha256  = $fullSha
        }
    }
    patches        = $patches
    exe_relative   = $ExeRelative
    changelog      = $Changelog
}

if ($MinSupported) {
    $manifest["min_supported"] = $MinSupported
}

$dir = Split-Path -Parent $OutFile
if ($dir -and -not (Test-Path -LiteralPath $dir)) {
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
}

$json = $manifest | ConvertTo-Json -Depth 8
# PowerShell ConvertTo-Json 可能改变属性顺序；对 CI 足够
[System.IO.File]::WriteAllText($OutFile, $json, [System.Text.UTF8Encoding]::new($false))
Write-Host "Wrote $OutFile"
Write-Host "  latest=$Version size=$fullSize sha256=$fullSha"
