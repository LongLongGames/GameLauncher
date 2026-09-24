using System.Text.Json.Serialization;

namespace GameLauncher.Models;

public sealed class GameCatalogItem
{
    [JsonPropertyName("game_id")]
    public string GameId { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("icon_url")]
    public string? IconUrl { get; set; }

    [JsonPropertyName("cover_url")]
    public string? CoverUrl { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("latest_version")]
    public string? LatestVersion { get; set; }

    public string Title => !string.IsNullOrWhiteSpace(DisplayName) ? DisplayName! : Name;
}

public sealed class CatalogListResponse
{
    [JsonPropertyName("games")]
    public List<GameCatalogItem>? Games { get; set; }

    [JsonPropertyName("items")]
    public List<GameCatalogItem>? Items { get; set; }

    [JsonPropertyName("data")]
    public CatalogListResponse? Data { get; set; }

    public IReadOnlyList<GameCatalogItem> Resolve()
    {
        if (Games is { Count: > 0 }) return Games;
        if (Items is { Count: > 0 }) return Items;
        if (Data != null) return Data.Resolve();
        return Array.Empty<GameCatalogItem>();
    }
}

/// <summary>本地安装/购买状态（P0 模拟）</summary>
public enum GameLocalStatus
{
    NotOwned,
    NotInstalled,
    UpdateAvailable,
    UpToDate,
    Downloading,
    Installing
}

public sealed class GameLocalState
{
    public string GameId { get; set; } = "";
    public GameLocalStatus Status { get; set; } = GameLocalStatus.NotInstalled;
    public string? InstalledVersion { get; set; }
    public double Progress { get; set; }
}
