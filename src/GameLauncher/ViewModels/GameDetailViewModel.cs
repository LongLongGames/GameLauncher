using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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

    /// <summary>详情区背景：优先 CoverUrl，否则 Assets/covers/{game_id}.*</summary>
    [ObservableProperty]
    private ImageSource? _coverImage;

    public GameDetailViewModel(IGameInstallService install)
    {
        _install = install;
    }

    public void Bind(GameCatalogItem? game)
    {
        _cts?.Cancel();
        Game = game;
        CoverImage = ResolveCover(game);
        RefreshFromLocal();
    }

    /// <summary>
    /// 解析封面：
    /// 1) 远程 CoverUrl（http/https）
    /// 2) 本地 Assets/covers/{game_id}.jpg|png|jpeg|webp
    /// 3) Assets/covers/_default.jpg|png
    /// </summary>
    public static ImageSource? ResolveCover(GameCatalogItem? game)
    {
        if (game is null) return GameAssetLoader.LoadCover("_default");

        if (!string.IsNullOrWhiteSpace(game.CoverUrl)
            && (game.CoverUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || game.CoverUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.UriSource = new Uri(game.CoverUrl, UriKind.Absolute);
                bi.EndInit();
                bi.Freeze();
                return bi;
            }
            catch { }
        }

        var img = GameAssetLoader.LoadCover(game.GameId);
        System.Diagnostics.Debug.WriteLine(
            "[Cover] game_id=" + game.GameId + " -> " + GameAssetLoader.DebugProbe("covers", game.GameId));
        return img;
    }

    public static ImageSource? ResolveIcon(GameCatalogItem? game)
    {
        if (game is null || string.IsNullOrWhiteSpace(game.GameId)) return null;
        return GameAssetLoader.LoadIcon(game.GameId);
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
