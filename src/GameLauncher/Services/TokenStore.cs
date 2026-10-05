using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameLauncher.Services;

/// <summary>
/// 将 session 写入 %LocalAppData%\LongLongGames\GameLauncher\session.json。
/// 对应 Unity 侧 PlayerPrefs + Save()。
/// </summary>
public sealed class TokenStore : ITokenStore
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly string _path;

    public TokenStore()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LongLongGames",
            "GameLauncher");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "session.json");
    }

    // 便于单测注入路径
    internal TokenStore(string path)
    {
        _path = path;
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
    }

    public string? GetAccessToken() => Read()?.AccessToken;
    public string? GetRefreshToken() => Read()?.RefreshToken;
    public string? GetMpAccountId() => Read()?.MpAccountId;
    public string? GetLastUsername() => Read()?.LastUsername;

    public void Save(string accessToken, string? refreshToken = null, string? mpAccountId = null, string? username = null)
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            Clear();
            return;
        }

        var data = new SessionFile
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            MpAccountId = mpAccountId,
            LastUsername = username,
            SavedAtUtc = DateTime.UtcNow
        };
        var json = JsonSerializer.Serialize(data, JsonOpts);
        // 原子写：先写临时再替换，避免半写
        var tmp = _path + ".tmp";
        File.WriteAllText(tmp, json);
        File.Move(tmp, _path, overwrite: true);
    }

    public void Clear()
    {
        try
        {
            if (File.Exists(_path))
                File.Delete(_path);
        }
        catch
        {
            // 忽略清盘失败
        }
    }

    private SessionFile? Read()
    {
        try
        {
            if (!File.Exists(_path))
                return null;
            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<SessionFile>(json, JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    private sealed class SessionFile
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("mp_account_id")]
        public string? MpAccountId { get; set; }

        [JsonPropertyName("last_username")]
        public string? LastUsername { get; set; }

        [JsonPropertyName("saved_at_utc")]
        public DateTime? SavedAtUtc { get; set; }
    }
}
