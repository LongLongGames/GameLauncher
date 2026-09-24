using GameLauncher.Models;

namespace GameLauncher.Services;

public sealed class SessionState
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public UserInfo? User { get; set; }

    public bool IsLoggedIn => !string.IsNullOrEmpty(AccessToken);

    public void Clear()
    {
        AccessToken = null;
        RefreshToken = null;
        User = null;
    }

    public void Apply(LoginResponse resp)
    {
        var r = resp.Data ?? resp;
        AccessToken = r.AccessToken;
        RefreshToken = r.RefreshToken;
        User = r.User;
    }
}
