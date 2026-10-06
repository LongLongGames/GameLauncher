using System.IO;

namespace GameLauncher.Helpers;

/// <summary>
/// 安装路径对齐 Steam：{InstallRoot}/common/{gameId}/
/// InstallRoot 为空时：启动器 exe 父目录作为平台根。
/// </summary>
public static class InstallPathResolver
{
    /// <param name="installRoot">配置项 InstallRoot；空则自动</param>
    /// <param name="libraryFolder">配置项 InstallLibraryFolder，默认 common</param>
    /// <param name="launcherExeDirectory">启动器 exe 所在目录；null 则用 AppContext.BaseDirectory</param>
    public static string ResolveLibraryRoot(
        string? installRoot,
        string? libraryFolder = "common",
        string? launcherExeDirectory = null)
    {
        var lib = string.IsNullOrWhiteSpace(libraryFolder) ? "common" : libraryFolder.Trim();

        string platformRoot;
        if (!string.IsNullOrWhiteSpace(installRoot))
        {
            platformRoot = Environment.ExpandEnvironmentVariables(installRoot.Trim());
        }
        else
        {
            var exeDir = launcherExeDirectory
                ?? AppContext.BaseDirectory.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar);
            // E:\LongLongGames\GameLauncher → E:\LongLongGames
            platformRoot = Directory.GetParent(exeDir)?.FullName
                ?? exeDir;
        }

        return Path.GetFullPath(Path.Combine(platformRoot, lib));
    }

    public static string GameDirectory(string libraryRoot, string gameId)
        => Path.Combine(libraryRoot, gameId);

    public static string VersionFile(string libraryRoot, string gameId)
        => Path.Combine(GameDirectory(libraryRoot, gameId), "version.txt");

    public static string? ReadLocalVersion(string libraryRoot, string gameId)
    {
        var path = VersionFile(libraryRoot, gameId);
        if (!File.Exists(path)) return null;
        var v = File.ReadAllText(path).Trim();
        return string.IsNullOrEmpty(v) ? null : v;
    }

    public static void WriteLocalVersion(string libraryRoot, string gameId, string version)
    {
        var dir = GameDirectory(libraryRoot, gameId);
        Directory.CreateDirectory(dir);
        File.WriteAllText(VersionFile(libraryRoot, gameId), version.Trim());
    }
}
