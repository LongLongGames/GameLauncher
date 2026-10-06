using System.Net.Http;
using System.Text.Json;
using GameLauncher.Helpers;
using GameLauncher.Models;
using Microsoft.Extensions.Configuration;

namespace GameLauncher.Services;

public sealed class VersionService : IVersionService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly string _cdnBaseUrl;

    public VersionService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _cdnBaseUrl = config["CdnBaseUrl"]?.Trim() ?? "http://localhost:12280/";
        if (!_cdnBaseUrl.EndsWith('/'))
            _cdnBaseUrl += "/";
    }

    public async Task<GameVersionManifest?> GetManifestAsync(string gameId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(gameId))
            return null;

        var url = CdnPaths.VersionManifest(_cdnBaseUrl, gameId);
        using var resp = await _http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode)
            return null;

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        return await JsonSerializer.DeserializeAsync<GameVersionManifest>(stream, JsonOpts, ct);
    }

    public DownloadPlan BuildPlan(GameVersionManifest manifest, string? localVersion)
    {
        var latest = manifest.Latest?.Trim() ?? "";
        if (string.IsNullOrEmpty(latest))
        {
            return new DownloadPlan { Kind = DownloadPlanKind.UpToDate };
        }

        // 已最新
        if (!string.IsNullOrEmpty(localVersion)
            && string.Equals(localVersion.Trim(), latest, StringComparison.OrdinalIgnoreCase))
        {
            return new DownloadPlan
            {
                Kind = DownloadPlanKind.UpToDate,
                TargetVersion = latest
            };
        }

        var min = manifest.MinSupported?.Trim();
        var forceFull = !string.IsNullOrEmpty(min)
            && !string.IsNullOrEmpty(localVersion)
            && CompareVersion(localVersion!, min) < 0;

        // 未安装或强制全量
        if (string.IsNullOrEmpty(localVersion) || forceFull)
        {
            var full = manifest.Packages?.Full;
            if (full is null || string.IsNullOrWhiteSpace(full.Path))
            {
                return new DownloadPlan { Kind = DownloadPlanKind.UpToDate, TargetVersion = latest };
            }

            return new DownloadPlan
            {
                Kind = string.IsNullOrEmpty(localVersion)
                    ? DownloadPlanKind.FullInstall
                    : DownloadPlanKind.ForceFull,
                TargetVersion = full.Version is { Length: > 0 } ? full.Version : latest,
                Items =
                [
                    new DownloadItem
                    {
                        Url = CdnPaths.File(_cdnBaseUrl, full.Path),
                        RelativePath = full.Path,
                        Size = full.Size,
                        Sha256 = full.Sha256,
                        IsPatch = false
                    }
                ]
            };
        }

        // 增量：从 local 走到 latest
        var chain = FindPatchChain(manifest.Patches, localVersion!, latest);
        if (chain is { Count: > 0 })
        {
            return new DownloadPlan
            {
                Kind = DownloadPlanKind.Incremental,
                TargetVersion = latest,
                Items = chain.Select(p => new DownloadItem
                {
                    Url = CdnPaths.File(_cdnBaseUrl, p.Path),
                    RelativePath = p.Path,
                    Size = p.Size,
                    Sha256 = p.Sha256,
                    IsPatch = true
                }).ToList()
            };
        }

        // 无可用补丁链 → 回退全量
        var fallback = manifest.Packages?.Full;
        if (fallback is not null && !string.IsNullOrWhiteSpace(fallback.Path))
        {
            return new DownloadPlan
            {
                Kind = DownloadPlanKind.ForceFull,
                TargetVersion = fallback.Version is { Length: > 0 } ? fallback.Version : latest,
                Items =
                [
                    new DownloadItem
                    {
                        Url = CdnPaths.File(_cdnBaseUrl, fallback.Path),
                        RelativePath = fallback.Path,
                        Size = fallback.Size,
                        Sha256 = fallback.Sha256,
                        IsPatch = false
                    }
                ]
            };
        }

        return new DownloadPlan { Kind = DownloadPlanKind.UpToDate, TargetVersion = latest };
    }

    /// <summary>简单 BFS：patches 中 from→to 边，求 local → target 路径</summary>
    private static List<PatchEntry>? FindPatchChain(
        List<PatchEntry>? patches,
        string fromVersion,
        string targetVersion)
    {
        if (patches is null || patches.Count == 0)
            return null;

        var edges = patches
            .Where(p => !string.IsNullOrWhiteSpace(p.From) && !string.IsNullOrWhiteSpace(p.To) && !string.IsNullOrWhiteSpace(p.Path))
            .GroupBy(p => p.From.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var start = fromVersion.Trim();
        var goal = targetVersion.Trim();

        var queue = new Queue<(string ver, List<PatchEntry> path)>();
        queue.Enqueue((start, new List<PatchEntry>()));
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { start };

        while (queue.Count > 0)
        {
            var (ver, path) = queue.Dequeue();
            if (string.Equals(ver, goal, StringComparison.OrdinalIgnoreCase))
                return path;

            if (!edges.TryGetValue(ver, out var nexts))
                continue;

            foreach (var edge in nexts)
            {
                var to = edge.To.Trim();
                if (!visited.Add(to))
                    continue;
                var nextPath = new List<PatchEntry>(path) { edge };
                queue.Enqueue((to, nextPath));
            }
        }

        return null;
    }

    /// <summary>粗略比较：尽量按点分数字；失败则 OrdinalIgnoreCase</summary>
    private static int CompareVersion(string a, string b)
    {
        static int[] Parts(string s) =>
            s.Split('.', StringSplitOptions.RemoveEmptyEntries)
             .Select(p => int.TryParse(p.Trim(), out var n) ? n : 0)
             .ToArray();

        var pa = Parts(a);
        var pb = Parts(b);
        var len = Math.Max(pa.Length, pb.Length);
        for (var i = 0; i < len; i++)
        {
            var x = i < pa.Length ? pa[i] : 0;
            var y = i < pb.Length ? pb[i] : 0;
            if (x != y) return x.CompareTo(y);
        }
        return 0;
    }
}
