using GameLauncher.Models;

namespace GameLauncher.Services;

public interface ICatalogService
{
    Task<IReadOnlyList<GameCatalogItem>> GetActiveGamesAsync(CancellationToken ct = default);
}
