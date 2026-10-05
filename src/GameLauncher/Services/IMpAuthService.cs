using GameLauncher.Models;

namespace GameLauncher.Services;

public interface IMpAuthService
{
    Task<LoginResponse> LoginOfficialAsync(string username, string password, CancellationToken ct = default);

    Task<MeResponse?> GetMeAsync(CancellationToken ct = default);

    /// <summary>从本地恢复 Token 到内存（不校验有效性）。对齐 ADR-0004 TryRestoreToken。</summary>
    void TryRestoreToken();

    /// <summary>
    /// 有本地 Token 时向 MP GET /api/v1/auth/me 探活。
    /// 成功返回 true；401/无效会 Logout 并返回 false。
    /// 网络/5xx 不强制登出，返回 false（走登录更安全）。
    /// </summary>
    Task<bool> ValidateSessionAsync(CancellationToken ct = default);

    /// <summary>清内存 + 清持久化。</summary>
    void Logout();

    /// <summary>启动入口：Restore → 有 Token 则 Validate → 成功即已登录。</summary>
    Task<bool> TryAutoLoginAsync(CancellationToken ct = default);
}
