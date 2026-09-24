using System.Collections.Concurrent;
using GameLauncher.Models;

namespace GameLauncher.Services;

/// <summary>
/// P0 本地状态机模拟：购买 / 下载 / 更新。
/// 后续对接真实 entitlement + game-core version-check + 增量补丁。
/// </summary>
public sealed class GameInstallService : IGameInstallService
{
    private readonly ConcurrentDictionary<string, GameLocalState> _states = new();

    public GameLocalState GetState(string gameId)
    {
        return _states.GetOrAdd(gameId, id => new GameLocalState
        {
            GameId = id,
            // P0：默认已购买、未安装，便于演示下载流
            Status = GameLocalStatus.NotInstalled,
            InstalledVersion = null
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
        string targetVersion,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        var s = GetState(gameId);
        s.Status = GameLocalStatus.Downloading;
        s.Progress = 0;

        for (var i = 1; i <= 20; i++)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(80, ct);
            s.Progress = i / 20.0;
            progress?.Report(s.Progress);
        }

        s.Status = GameLocalStatus.Installing;
        await Task.Delay(300, ct);

        s.InstalledVersion = targetVersion;
        s.Status = GameLocalStatus.UpToDate;
        s.Progress = 1;
        progress?.Report(1);
    }
}
