using System;
using System.Reflection;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace GameLauncher.Services;

public sealed class UpdateCheckResult
{
    public bool IsInstalled { get; init; }
    public string CurrentVersion { get; init; } = "";
    public string? NewVersion { get; init; }
    public bool UpdateAvailable => NewVersion is not null;
    public UpdateInfo? Info { get; init; }
}

public interface IUpdateService
{
    string CurrentVersion { get; }
    Task<UpdateCheckResult> CheckAsync();
    Task DownloadAndApplyAsync(UpdateInfo update);
}

public sealed class UpdateService : IUpdateService
{
    public const string PackId = "LongLongGames.GameLauncher";
    private static readonly string RepoUrl = "https://github.com/LongLongGames/GameLauncher";

    public string CurrentVersion { get; } =
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)
        ?? "0.0.0";

    public async Task<UpdateCheckResult> CheckAsync()
    {
        var current = CurrentVersion;
        try
        {
            var source = new GithubSource(RepoUrl, accessToken: null, prerelease: false);
            var mgr = new UpdateManager(source);

            if (!mgr.IsInstalled)
            {
                return new UpdateCheckResult { IsInstalled = false, CurrentVersion = current };
            }

            try { current = mgr.CurrentVersion?.ToString() ?? current; } catch { }

            var info = await mgr.CheckForUpdatesAsync();
            return new UpdateCheckResult
            {
                IsInstalled = true,
                CurrentVersion = current,
                NewVersion = info?.TargetFullRelease?.Version?.ToString(),
                Info = info
            };
        }
        catch (NotInstalledException)
        {
            return new UpdateCheckResult { IsInstalled = false, CurrentVersion = current };
        }
        catch
        {
            return new UpdateCheckResult { IsInstalled = false, CurrentVersion = current };
        }
    }

    public async Task DownloadAndApplyAsync(UpdateInfo update)
    {
        var source = new GithubSource(RepoUrl, accessToken: null, prerelease: false);
        var mgr = new UpdateManager(source);
        await mgr.DownloadUpdatesAsync(update);
        mgr.ApplyUpdatesAndRestart(update);
    }
}
