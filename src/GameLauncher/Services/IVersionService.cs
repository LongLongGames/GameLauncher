using GameLauncher.Models;

namespace GameLauncher.Services;

public interface IVersionService
{
    /// <summary>拉取 CDN config/{gameId}/version.json</summary>
    Task<GameVersionManifest?> GetManifestAsync(string gameId, CancellationToken ct = default);

    /// <summary>根据本地版本与 manifest 生成下载计划</summary>
    DownloadPlan BuildPlan(GameVersionManifest manifest, string? localVersion);
}
