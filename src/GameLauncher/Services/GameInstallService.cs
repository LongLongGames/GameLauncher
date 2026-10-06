using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using GameLauncher.Helpers;
using GameLauncher.Models;
// AppConfig 在 App.xaml.cs / namespace GameLauncher

namespace GameLauncher.Services;

public sealed class GameInstallService : IGameInstallService
{
    private readonly ConcurrentDictionary<string, GameLocalState> _states = new();
    private readonly HttpClient _http;
    private readonly IVersionService _versions;
    private readonly string _libraryRoot;

    public GameInstallService(
        HttpClient http,
        IVersionService versions,
        AppConfig config)
    {
        _http = http;
        _versions = versions;
        _libraryRoot = InstallPathResolver.ResolveLibraryRoot(
            config.InstallRoot,
            config.InstallLibraryFolder);
    }

    public string LibraryRoot => _libraryRoot;

    public GameLocalState GetState(string gameId)
    {
        return _states.GetOrAdd(gameId, id =>
        {
            var local = InstallPathResolver.ReadLocalVersion(_libraryRoot, id);
            return new GameLocalState
            {
                GameId = id,
                Status = local is null ? GameLocalStatus.NotInstalled : GameLocalStatus.UpToDate,
                InstalledVersion = local
            };
        });
    }

    public Task PurchaseAsync(string gameId, CancellationToken ct = default)
    {
        var s = GetState(gameId);
        if (s.Status == GameLocalStatus.NotOwned)
        {
            s.Status = GameLocalStatus.NotInstalled;
            s.InstalledVersion = null;
        }
        return Task.CompletedTask;
    }

    public async Task DownloadOrUpdateAsync(
        string gameId,
        string? targetVersion = null,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        var s = GetState(gameId);
        s.Status = GameLocalStatus.Downloading;
        s.Progress = 0;
        progress?.Report(0);

        var manifest = await _versions.GetManifestAsync(gameId, ct)
            ?? throw new InvalidOperationException($"无法获取版本清单: config/{gameId}/version.json");

        var local = InstallPathResolver.ReadLocalVersion(_libraryRoot, gameId);
        var plan = _versions.BuildPlan(manifest, local);

        if (plan.Kind == DownloadPlanKind.UpToDate)
        {
            s.InstalledVersion = plan.TargetVersion ?? local;
            s.Status = GameLocalStatus.UpToDate;
            s.Progress = 1;
            progress?.Report(1);
            return;
        }

        if (plan.Items.Count == 0)
            throw new InvalidOperationException("下载计划为空（缺少 full 包或补丁链）");

        var gameDir = InstallPathResolver.GameDirectory(_libraryRoot, gameId);
        Directory.CreateDirectory(gameDir);
        var staging = Path.Combine(gameDir, ".download");
        Directory.CreateDirectory(staging);

        var totalBytes = plan.Items.Sum(i => i.Size ?? 0);
        long doneBytes = 0;

        for (var i = 0; i < plan.Items.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var item = plan.Items[i];
            var fileName = Path.GetFileName(
                item.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (string.IsNullOrEmpty(fileName))
                fileName = $"part_{i}";
            var dest = Path.Combine(staging, fileName);

            await DownloadFileAsync(item, dest, bytes =>
            {
                var overall = totalBytes > 0
                    ? (double)(doneBytes + bytes) / totalBytes
                    : (i + (double)bytes / Math.Max(1, item.Size ?? 1)) / plan.Items.Count;
                s.Progress = Math.Clamp(overall, 0, 0.99);
                progress?.Report(s.Progress);
            }, ct);

            doneBytes += item.Size ?? new FileInfo(dest).Length;
        }

        s.Status = GameLocalStatus.Installing;
        await Task.Yield();

        var finalVersion = plan.TargetVersion ?? manifest.Latest;
        InstallPathResolver.WriteLocalVersion(_libraryRoot, gameId, finalVersion);
        try { Directory.Delete(staging, recursive: true); } catch { /* ignore */ }

        s.InstalledVersion = finalVersion;
        s.Status = GameLocalStatus.UpToDate;
        s.Progress = 1;
        progress?.Report(1);
    }

    private async Task DownloadFileAsync(
        DownloadItem item,
        string destPath,
        Action<long> onBytes,
        CancellationToken ct)
    {
        using var resp = await _http.GetAsync(item.Url, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();

        var tmp = destPath + ".partial";
        await using (var input = await resp.Content.ReadAsStreamAsync(ct))
        await using (var output = File.Create(tmp))
        {
            var buffer = new byte[81920];
            long readTotal = 0;
            int n;
            while ((n = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
            {
                await output.WriteAsync(buffer.AsMemory(0, n), ct);
                readTotal += n;
                onBytes(readTotal);
            }
        }

        if (!string.IsNullOrWhiteSpace(item.Sha256))
        {
            var actual = await ComputeSha256HexAsync(tmp, ct);
            if (!string.Equals(actual, item.Sha256.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(tmp);
                throw new InvalidDataException($"SHA256 不匹配: {item.RelativePath}");
            }
        }

        if (File.Exists(destPath))
            File.Delete(destPath);
        File.Move(tmp, destPath);
    }

    private static async Task<string> ComputeSha256HexAsync(string path, CancellationToken ct)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, ct);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
