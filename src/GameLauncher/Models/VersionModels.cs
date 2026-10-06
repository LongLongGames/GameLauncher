using System.Text.Json.Serialization;

namespace GameLauncher.Models;

/// <summary>
/// CDN 权威版本清单：config/{gameId}/version.json
/// </summary>
public sealed class GameVersionManifest
{
    [JsonPropertyName("game_id")]
    public string GameId { get; set; } = "";

    [JsonPropertyName("platform")]
    public string Platform { get; set; } = "windows";

    [JsonPropertyName("channel")]
    public string Channel { get; set; } = "official";

    [JsonPropertyName("latest")]
    public string Latest { get; set; } = "";

    [JsonPropertyName("min_supported")]
    public string? MinSupported { get; set; }

    [JsonPropertyName("packages")]
    public PackageSet? Packages { get; set; }

    [JsonPropertyName("patches")]
    public List<PatchEntry>? Patches { get; set; }

    [JsonPropertyName("exe_relative")]
    public string? ExeRelative { get; set; }

    [JsonPropertyName("changelog")]
    public string? Changelog { get; set; }
}

public sealed class PackageSet
{
    [JsonPropertyName("full")]
    public PackageEntry? Full { get; set; }
}

public sealed class PackageEntry
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    /// <summary>相对 CDN 根路径，如 download/match3/windows/official/Game_Setup_1.0.2.exe</summary>
    [JsonPropertyName("path")]
    public string Path { get; set; } = "";

    [JsonPropertyName("size")]
    public long? Size { get; set; }

    [JsonPropertyName("sha256")]
    public string? Sha256 { get; set; }
}

public sealed class PatchEntry
{
    [JsonPropertyName("from")]
    public string From { get; set; } = "";

    [JsonPropertyName("to")]
    public string To { get; set; } = "";

    [JsonPropertyName("path")]
    public string Path { get; set; } = "";

    [JsonPropertyName("size")]
    public long? Size { get; set; }

    [JsonPropertyName("sha256")]
    public string? Sha256 { get; set; }
}

/// <summary>根据本地版本与 manifest 算出的下载计划</summary>
public enum DownloadPlanKind
{
    UpToDate,
    FullInstall,
    ForceFull,
    Incremental
}

public sealed class DownloadPlan
{
    public DownloadPlanKind Kind { get; init; }
    public string? TargetVersion { get; init; }
    public IReadOnlyList<DownloadItem> Items { get; init; } = Array.Empty<DownloadItem>();
}

public sealed class DownloadItem
{
    public string Url { get; init; } = "";
    public string RelativePath { get; init; } = "";
    public long? Size { get; init; }
    public string? Sha256 { get; init; }
    public bool IsPatch { get; init; }
}
