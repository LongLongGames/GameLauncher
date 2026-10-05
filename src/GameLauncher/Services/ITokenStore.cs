namespace GameLauncher.Services;

/// <summary>
/// 持久化 access/refresh token。对齐 ADR-0004 / match3·act TokenStore。
/// 只负责读写，不解释登录态。
/// </summary>
public interface ITokenStore
{
    string? GetAccessToken();
    string? GetRefreshToken();
    string? GetMpAccountId();
    string? GetLastUsername();

    void Save(string accessToken, string? refreshToken = null, string? mpAccountId = null, string? username = null);
    void Clear();
}
