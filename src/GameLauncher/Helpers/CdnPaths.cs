namespace GameLauncher.Helpers;

/// <summary>
/// 与 LocalCDN data 约定一致的路径拼装。生产只换 CdnBaseUrl。
/// </summary>
public static class CdnPaths
{
    public static string Combine(string baseUrl, string relativePath)
    {
        var b = (baseUrl ?? "").TrimEnd('/');
        var r = (relativePath ?? "").TrimStart('/');
        return string.IsNullOrEmpty(r) ? b + "/" : $"{b}/{r}";
    }

    /// <summary>config/{gameId}/version.json</summary>
    public static string VersionManifest(string baseUrl, string gameId)
        => Combine(baseUrl, $"config/{gameId}/version.json");

    /// <summary>manifest 内 path → 完整 URL</summary>
    public static string File(string baseUrl, string relativePath)
        => Combine(baseUrl, relativePath);
}
