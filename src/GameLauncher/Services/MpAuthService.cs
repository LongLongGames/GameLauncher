using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GameLauncher.Models;

namespace GameLauncher.Services;

public sealed class MpAuthService : IMpAuthService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly AppConfig _config;
    private readonly SessionState _session;
    private readonly ITokenStore _tokenStore;

    public MpAuthService(HttpClient http, AppConfig config, SessionState session, ITokenStore tokenStore)
    {
        _http = http;
        _config = config;
        _session = session;
        _tokenStore = tokenStore;
    }

    public async Task<LoginResponse> LoginOfficialAsync(string username, string password, CancellationToken ct = default)
    {
        var body = new LoginRequest
        {
            Provider = "official",
            AppId = _config.AppId,
            DeviceId = _config.DeviceId,
            AuthPayload = new AuthPayload
            {
                Username = username,
                Password = password
            }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "api/v1/auth/login");
        req.Content = JsonContent.Create(body);

        using var resp = await _http.SendAsync(req, ct);
        var raw = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"登录失败 ({(int)resp.StatusCode}): {Truncate(raw)}");

        var parsed = JsonSerializer.Deserialize<LoginResponse>(raw, JsonOpts)
                     ?? throw new InvalidOperationException("登录响应解析失败");

        var effective = parsed.Data ?? parsed;
        if (string.IsNullOrEmpty(effective.AccessToken))
            throw new InvalidOperationException("登录响应缺少 access_token");

        _session.Apply(parsed);

        // 持久化（对齐 Unity AuthService：内存 + TokenStore）
        var mpAccountId = effective.User?.Id;
        _tokenStore.Save(
            effective.AccessToken!,
            effective.RefreshToken,
            mpAccountId,
            username);

        return parsed;
    }

    public async Task<MeResponse?> GetMeAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_session.AccessToken))
            return null;

        using var req = new HttpRequestMessage(HttpMethod.Get, "api/v1/auth/me");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.AccessToken);

        using var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
            return null;

        var raw = await resp.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<MeResponse>(raw, JsonOpts);
    }

    public void TryRestoreToken()
    {
        var access = _tokenStore.GetAccessToken();
        if (string.IsNullOrEmpty(access))
            return;

        _session.AccessToken = access;
        _session.RefreshToken = _tokenStore.GetRefreshToken();

        // 用上次用户名占位，真正 User 信息等 ValidateSession /me 再填
        var lastUser = _tokenStore.GetLastUsername();
        if (!string.IsNullOrEmpty(lastUser) && _session.User is null)
        {
            _session.User = new UserInfo
            {
                Username = lastUser,
                DisplayName = lastUser
            };
        }
    }

    public async Task<bool> ValidateSessionAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_session.AccessToken))
            return false;

        using var req = new HttpRequestMessage(HttpMethod.Get, "api/v1/auth/me");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.AccessToken);

        try
        {
            using var resp = await _http.SendAsync(req, ct);

            if (resp.StatusCode == HttpStatusCode.Unauthorized)
            {
                // 明确未授权 → 清 token
                Logout();
                return false;
            }

            if (!resp.IsSuccessStatusCode)
            {
                // 5xx / 网络层外的其它错误：不保留假登录
                return false;
            }

            var raw = await resp.Content.ReadAsStringAsync(ct);
            var me = JsonSerializer.Deserialize<MeResponse>(raw, JsonOpts);
            if (me is not null)
            {
                // 用 /me 补全用户信息（MP 返回 mp_account_id 等）
                _session.User ??= new UserInfo();
                if (!string.IsNullOrEmpty(me.Id))
                    _session.User.Id = me.Id;
                if (!string.IsNullOrEmpty(me.Username))
                    _session.User.Username = me.Username;
                if (!string.IsNullOrEmpty(me.DisplayName))
                    _session.User.DisplayName = me.DisplayName;
                // 兼容只返回 mp_account_id 的 MeResponse
                if (string.IsNullOrEmpty(_session.User.Id) && !string.IsNullOrEmpty(me.MpAccountId))
                    _session.User.Id = me.MpAccountId;
            }

            return true;
        }
        catch (HttpRequestException)
        {
            // 网络不可达：不得当作已登录
            return false;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }

    public void Logout()
    {
        _session.Clear();
        _tokenStore.Clear();
    }

    public async Task<bool> TryAutoLoginAsync(CancellationToken ct = default)
    {
        TryRestoreToken();
        if (string.IsNullOrEmpty(_session.AccessToken))
            return false;

        return await ValidateSessionAsync(ct);
    }

    private static string Truncate(string s, int max = 200)
        => s.Length <= max ? s : s[..max] + "…";
}
