using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameLauncher.Models;
using GameLauncher.Services;

namespace GameLauncher.ViewModels;

public partial class GameDetailViewModel : ObservableObject
{
    private readonly IGameInstallService _install;
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private GameCatalogItem? _game;

    [ObservableProperty]
    private string _localVersionText = "未安装";

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private string _actionText = "下载";

    [ObservableProperty]
    private bool _actionEnabled = true;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private bool _showProgress;

    public GameDetailViewModel(IGameInstallService install)
    {
        _install = install;
    }

    public void Bind(GameCatalogItem? game)
    {
        _cts?.Cancel();
        Game = game;
        RefreshFromLocal();
    }

    private void RefreshFromLocal()
    {
        if (Game == null)
        {
            LocalVersionText = "—";
            StatusText = "";
            ActionText = "";
            ActionEnabled = false;
            ShowProgress = false;
            return;
        }

        var state = _install.GetState(Game.GameId);
        var latest = Game.LatestVersion ?? "1.0.0";

        // 若已安装且版本落后，标记可更新
        if (state.Status == GameLocalStatus.UpToDate
            && !string.IsNullOrEmpty(state.InstalledVersion)
            && !string.Equals(state.InstalledVersion, latest, StringComparison.OrdinalIgnoreCase))
        {
            state.Status = GameLocalStatus.UpdateAvailable;
        }

        LocalVersionText = state.InstalledVersion ?? "未安装";
        Progress = state.Progress;
        ShowProgress = state.Status is GameLocalStatus.Downloading or GameLocalStatus.Installing;

        switch (state.Status)
        {
            case GameLocalStatus.NotOwned:
                StatusText = "未购买";
                ActionText = "购买";
                ActionEnabled = true;
                break;
            case GameLocalStatus.NotInstalled:
                StatusText = "已购买 · 未安装";
                ActionText = "下载";
                ActionEnabled = true;
                break;
            case GameLocalStatus.UpdateAvailable:
                StatusText = $"可更新 → {latest}";
                ActionText = "更新";
                ActionEnabled = true;
                break;
            case GameLocalStatus.UpToDate:
                StatusText = "已是最新";
                ActionText = "启动";
                ActionEnabled = true;
                break;
            case GameLocalStatus.Downloading:
                StatusText = "下载中…";
                ActionText = "下载中";
                ActionEnabled = false;
                break;
            case GameLocalStatus.Installing:
                StatusText = "安装中…";
                ActionText = "安装中";
                ActionEnabled = false;
                break;
        }
    }

    [RelayCommand]
    private async Task ActionAsync()
    {
        if (Game == null || !ActionEnabled) return;

        var state = _install.GetState(Game.GameId);
        var latest = Game.LatestVersion ?? "1.0.0";

        try
        {
            if (state.Status == GameLocalStatus.NotOwned)
            {
                await _install.PurchaseAsync(Game.GameId);
                RefreshFromLocal();
                return;
            }

            if (state.Status is GameLocalStatus.NotInstalled or GameLocalStatus.UpdateAvailable)
            {
                _cts = new CancellationTokenSource();
                var progress = new Progress<double>(p =>
                {
                    Progress = p;
                    ShowProgress = true;
                    StatusText = state.Status == GameLocalStatus.Installing ? "安装中…" : $"下载中… {(int)(p * 100)}%";
                });
                ActionEnabled = false;
                await _install.DownloadOrUpdateAsync(Game.GameId, latest, progress, _cts.Token);
                RefreshFromLocal();
                return;
            }

            if (state.Status == GameLocalStatus.UpToDate)
            {
                // P0：仅提示，不真正拉起游戏进程
                StatusText = "启动请求已发送（P0 占位）";
            }
        }
        catch (OperationCanceledException)
        {
            RefreshFromLocal();
        }
        catch (Exception ex)
        {
            StatusText = "操作失败: " + ex.Message;
            ActionEnabled = true;
        }
    }
}
