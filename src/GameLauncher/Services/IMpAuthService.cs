using GameLauncher.Models;

namespace GameLauncher.Services;

public interface IMpAuthService
{
    Task<LoginResponse> LoginOfficialAsync(string username, string password, CancellationToken ct = default);
    Task<MeResponse?> GetMeAsync(CancellationToken ct = default);
}
