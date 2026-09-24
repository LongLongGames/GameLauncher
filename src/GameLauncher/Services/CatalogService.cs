using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using GameLauncher.Models;

namespace GameLauncher.Services;

public sealed class CatalogService : ICatalogService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly SessionState _session;

    public CatalogService(HttpClient http, SessionState session)
    {
        _http = http;
        _session = session;
    }

    public async Task<IReadOnlyList<GameCatalogItem>> GetActiveGamesAsync(CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "api/v1/catalog/games?status=active");
        if (!string.IsNullOrEmpty(_session.AccessToken))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.AccessToken);

        using var resp = await _http.SendAsync(req, ct);
        var raw = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            // P0 fallback: return demo entries so UI can be exercised without MP
            return DemoGames();
        }

        // try array root
        try
        {
            var arr = JsonSerializer.Deserialize<List<GameCatalogItem>>(raw, JsonOpts);
            if (arr is { Count: > 0 }) return arr;
        }
        catch { /* fall through */ }

        var wrapped = JsonSerializer.Deserialize<CatalogListResponse>(raw, JsonOpts);
        var list = wrapped?.Resolve() ?? Array.Empty<GameCatalogItem>();
        return list.Count > 0 ? list : DemoGames();
    }

    private static IReadOnlyList<GameCatalogItem> DemoGames() =>
    [
        new GameCatalogItem
        {
            GameId = "match3",
            Name = "match3",
            DisplayName = "三消 Match3",
            Status = "active",
            LatestVersion = "1.0.2",
            Description = "首个落地实例 · 三消玩法"
        },
        new GameCatalogItem
        {
            GameId = "act",
            Name = "act",
            DisplayName = "即时战斗 ACT",
            Status = "active",
            LatestVersion = "0.1.0",
            Description = "类 Diablo + ROR2 · 建设中"
        }
    ];
}
