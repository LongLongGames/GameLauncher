using GameLauncher.Models;

namespace GameLauncher.Services;

public interface IGameInstallService
{
    GameLocalState GetState(string gameId);
    Task PurchaseAsync(string gameId, CancellationToken ct = default);
    Task DownloadOrUpdateAsync(string gameId, string targetVersion, IProgress<double>? progress = null, CancellationToken ct = default);
}
