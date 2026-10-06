using GameLauncher.Models;

namespace GameLauncher.Services;

public interface IGameInstallService
{
    /// <summary>解析后的库根目录，如 E:\LongLongGames\common</summary>
    string LibraryRoot { get; }

    GameLocalState GetState(string gameId);

    Task PurchaseAsync(string gameId, CancellationToken ct = default);

    /// <param name="targetVersion">可忽略；以 CDN manifest.latest 为准</param>
    Task DownloadOrUpdateAsync(
        string gameId,
        string? targetVersion = null,
        IProgress<double>? progress = null,
        CancellationToken ct = default);
}
