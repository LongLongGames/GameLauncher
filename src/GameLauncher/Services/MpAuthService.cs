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

    public MpAuthService(HttpClient http, AppConfig config, SessionState session)
    {
        _http = http;
        _config = config;
        _session = session;
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

    private static string Truncate(string s, int max = 200)
        => s.Length <= max ? s : s[..max] + "…";
}
