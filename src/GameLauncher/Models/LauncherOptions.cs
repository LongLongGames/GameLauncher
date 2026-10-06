namespace GameLauncher.Models;

/// <summary>
/// 对应 appsettings.json。在 DI 里 Bind 一次后注入，避免服务直接依赖 IConfiguration。
/// </summary>
public sealed class LauncherOptions
{
    public string MpBaseUrl { get; set; } = "http://localhost:11080";
    public string CdnBaseUrl { get; set; } = "http://localhost:12280/";
    public string AppId { get; set; } = "game_launcher";
    public string DeviceId { get; set; } = "pc-launcher-p0";
    public string Platform { get; set; } = "windows";
    public string Channel { get; set; } = "official";

    /// <summary>空 = 启动器父目录作为平台根</summary>
    public string InstallRoot { get; set; } = "";

    /// <summary>库子目录，默认 common（对齐 Steam steamapps/common）</summary>
    public string InstallLibraryFolder { get; set; } = "common";
}
