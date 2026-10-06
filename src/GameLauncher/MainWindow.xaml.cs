using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using GameLauncher.Services;
using GameLauncher.ViewModels;
using GameLauncher.Views;
using Application = System.Windows.Application;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using MouseButtonState = System.Windows.Input.MouseButtonState;
using Mouse = System.Windows.Input.Mouse;
using MouseEventHandler = System.Windows.Input.MouseEventHandler;
using MouseButtonEventHandler = System.Windows.Input.MouseButtonEventHandler;

namespace GameLauncher;

public partial class MainWindow : Window
{
    private NotifyIcon? _tray;
    private bool _reallyExit;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closing += OnClosing;
        InitTray();
        // 先不 ShowLogin：OnLoaded 里按 ADR-0004 做 token 自动登录
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // 确保无系统标题栏
        WindowStyle = WindowStyle.None;
        _ = InitVersionAndCheckUpdateAsync();
        _ = TryAutoLoginThenNavigateAsync();
    }

    /// <summary>
    /// ADR-0004：TryRestoreToken → ValidateSession → Main / Login。
    /// 与 match3 / act Unity 客户端启动流一致。
    /// </summary>
    private async System.Threading.Tasks.Task TryAutoLoginThenNavigateAsync()
    {
        try
        {
            var auth = App.Services.GetService(typeof(IMpAuthService)) as IMpAuthService;
            if (auth is not null && await auth.TryAutoLoginAsync())
            {
                ShowMain();
                return;
            }
        }
        catch
        {
            // 网络/异常 → 回登录页
        }
        ShowLogin();
    }

    private async System.Threading.Tasks.Task InitVersionAndCheckUpdateAsync()
    {
        var svc = App.Services.GetService(typeof(IUpdateService)) as IUpdateService;
        var ver = svc?.CurrentVersion ?? "0.0.0";
        VersionLabel.Text = $"  ·  v{ver}";

        if (svc is null) return;
        try
        {
            var result = await svc.CheckAsync();
            if (!result.UpdateAvailable || result.Info is null) return;

            var dlg = new Views.UpdateDialog(result.CurrentVersion, result.NewVersion!);
            dlg.Owner = this;
            if (dlg.ShowDialog() == true && dlg.ShouldUpdate)
            {
                await svc.DownloadAndApplyAsync(result.Info);
            }
        }
        catch
        {
            // 网络失败等静默
        }
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        var svc = App.Services.GetService(typeof(IUpdateService)) as IUpdateService;
        var ver = svc?.CurrentVersion ?? "0.0.0";
        // isInstalled: quick check without network
        var installed = false;
        try
        {
            var r = svc?.CheckAsync().GetAwaiter().GetResult();
            installed = r?.IsInstalled ?? false;
            if (r is not null) ver = r.CurrentVersion;
        }
        catch { }
        var about = new Views.AboutDialog(ver, installed) { Owner = this };
        about.ShowDialog();
    }

    private void InitTray()
    {
        _tray = new NotifyIcon
        {
            Text = "GameLauncher",
            Visible = true,
            Icon = LoadAppIcon() ?? SystemIcons.Application
        };
        _tray.DoubleClick += (_, _) => RestoreFromTray();
        var menu = new ContextMenuStrip();
        menu.Items.Add("显示 GameLauncher", null, (_, _) => RestoreFromTray());
        menu.Items.Add("退出", null, (_, _) =>
        {
            _reallyExit = true;
            Close();
        });
        _tray.ContextMenuStrip = menu;

        // 窗口 / 任务栏图标（WPF Window.Icon；ApplicationIcon 管 exe 文件图标）
        try
        {
            var uri = new Uri("pack://application:,,,/Assets/app.ico", UriKind.Absolute);
            var sri = System.Windows.Application.GetResourceStream(uri);
            if (sri is null)
            {
                // Content 复制到输出目录时的文件路径回退
                var path = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
                if (File.Exists(path))
                    Icon = BitmapFrameFromFile(path);
            }
            else
            {
                Icon = System.Windows.Media.Imaging.BitmapFrame.Create(sri.Stream);
            }
        }
        catch
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
            if (File.Exists(path))
            {
                try { Icon = BitmapFrameFromFile(path); } catch { /* ignore */ }
            }
        }

        ApplyTitleBarIcon();
    }

    private void ApplyTitleBarIcon()
    {
        try
        {
            System.Windows.Media.Imaging.BitmapSource? src = null;

            // pack 嵌入
            try
            {
                var uri = new Uri("pack://application:,,,/Assets/app.ico", UriKind.Absolute);
                src = new System.Windows.Media.Imaging.BitmapImage(uri);
            }
            catch { /* try file */ }

            if (src is null)
            {
                foreach (var path in CandidateIconPaths())
                {
                    if (!File.Exists(path)) continue;
                    var bi = new System.Windows.Media.Imaging.BitmapImage();
                    bi.BeginInit();
                    bi.UriSource = new Uri(path, UriKind.Absolute);
                    bi.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bi.DecodePixelWidth = 32;
                    bi.EndInit();
                    bi.Freeze();
                    src = bi;
                    break;
                }
            }

            if (src is null)
            {
                if (TitleBarIcon is not null) TitleBarIcon.Visibility = System.Windows.Visibility.Collapsed;
                if (TitleBarIconFallback is not null) TitleBarIconFallback.Visibility = System.Windows.Visibility.Visible;
                return;
            }

            if (TitleBarIcon is not null)
            {
                TitleBarIcon.Source = src;
                TitleBarIcon.Visibility = System.Windows.Visibility.Visible;
            }
            if (TitleBarIconFallback is not null)
                TitleBarIconFallback.Visibility = System.Windows.Visibility.Collapsed;

            Icon ??= src;
        }
        catch
        {
            if (TitleBarIcon is not null) TitleBarIcon.Visibility = System.Windows.Visibility.Collapsed;
            if (TitleBarIconFallback is not null) TitleBarIconFallback.Visibility = System.Windows.Visibility.Visible;
        }
    }

    /// <summary>
    /// 托盘用 System.Drawing.Icon。
    /// 优先 pack 嵌入资源 Assets/app.ico，其次输出目录 / 工程目录松散文件。
    /// 旧代码写死 SystemIcons.Application → 系统默认图标，看起来像「没有托盘 icon」。
    /// </summary>
    private static Icon? LoadAppIcon()
    {
        try
        {
            // 1) 嵌入 Resource（csproj <Resource Include="Assets/app.ico"/>）
            var uri = new Uri("pack://application:,,,/Assets/app.ico", UriKind.Absolute);
            var sri = System.Windows.Application.GetResourceStream(uri);
            if (sri?.Stream is not null)
            {
                using (sri.Stream)
                {
                    // Icon 需要可 Seek 的流
                    var ms = new MemoryStream();
                    sri.Stream.CopyTo(ms);
                    ms.Position = 0;
                    return new Icon(ms);
                }
            }
        }
        catch
        {
            // fall through to file paths
        }

        try
        {
            foreach (var path in CandidateIconPaths())
            {
                if (File.Exists(path))
                    return new Icon(path);
            }
        }
        catch
        {
            // ignore
        }
        return null;
    }

    private static IEnumerable<string> CandidateIconPaths()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
        yield return Path.Combine(AppContext.BaseDirectory, "app.ico");
        var dev = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Assets", "app.ico"));
        yield return dev;
    }

    private static System.Windows.Media.Imaging.BitmapFrame BitmapFrameFromFile(string path)
    {
        var bi = new System.Windows.Media.Imaging.BitmapImage();
        bi.BeginInit();
        bi.UriSource = new Uri(path, UriKind.Absolute);
        bi.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
        bi.EndInit();
        bi.Freeze();
        return System.Windows.Media.Imaging.BitmapFrame.Create(bi);
    }

    private void ShowLogin()
    {
        var vm = App.Services.GetService(typeof(LoginViewModel)) as LoginViewModel
                 ?? throw new InvalidOperationException("LoginViewModel");
        vm.LoginSucceeded += () => Dispatcher.Invoke(ShowMain);
        var view = new LoginView { DataContext = vm };
        RootContent.Content = view;
    }

    private async void ShowMain()
    {
        var vm = App.Services.GetService(typeof(MainViewModel)) as MainViewModel
                 ?? throw new InvalidOperationException("MainViewModel");
        vm.LogoutRequested += () => Dispatcher.Invoke(ShowLogin);
        var view = new MainView { DataContext = vm };
        RootContent.Content = view;
        await vm.LoadCommand.ExecuteAsync(null);
    }

    private void Content_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    // ─── Steam / Battle.net 风格：标题栏按钮 ───────────────────────────

    private void TitleBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximize();
            return;
        }
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        // 普通最小化到任务栏（与 Steam 默认 − 一致）
        WindowState = WindowState.Minimized;
    }

    private void Maximize_Click(object sender, RoutedEventArgs e) => ToggleMaximize();

    private void ToggleMaximize()
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
            if (MaxRestoreButton is not null)
                MaxRestoreButton.Content = "□";
            if (MaxRestoreButton is not null)
                MaxRestoreButton.ToolTip = "最大化";
        }
        else
        {
            WindowState = WindowState.Maximized;
            if (MaxRestoreButton is not null)
                MaxRestoreButton.Content = "❐";
            if (MaxRestoreButton is not null)
                MaxRestoreButton.ToolTip = "还原";
        }
    }

    /// <summary>标题栏 【×】：收进系统托盘，进程继续跑（Steam / BN 同款）。</summary>
    private void CloseToTray_Click(object sender, RoutedEventArgs e) => HideToTray(showTip: true);

    private void Window_StateChanged(object? sender, EventArgs e)
    {
        // 同步最大化按钮图标
        if (MaxRestoreButton is null) return;
        if (WindowState == WindowState.Maximized)
        {
            MaxRestoreButton.Content = "❐";
            MaxRestoreButton.ToolTip = "还原";
        }
        else if (WindowState == WindowState.Normal)
        {
            MaxRestoreButton.Content = "□";
            MaxRestoreButton.ToolTip = "最大化";
        }
    }

    private void ToggleVisibility_Click(object sender, RoutedEventArgs e)
    {
        if (IsVisible && WindowState != WindowState.Minimized)
            HideToTray(showTip: true);
        else
            RestoreFromTray();
    }

    private void HideToTray(bool showTip)
    {
        Hide();
        if (showTip && _tray is not null)
        {
            _tray.ShowBalloonTip(
                1500,
                "GameLauncher",
                "已收起到系统托盘。双击图标可恢复，右键可退出。",
                ToolTipIcon.Info);
        }
    }

    private void RestoreFromTray()
    {
        Show();
        if (WindowState == WindowState.Minimized)
            WindowState = WindowState.Normal;
        Activate();
        Topmost = true;
        Topmost = false;
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        _reallyExit = true;
        Close();
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Alt+F4 / 系统关闭：同样收托盘，不真正退出（Steam / BN）
        if (!_reallyExit)
        {
            e.Cancel = true;
            HideToTray(showTip: true);
            return;
        }

        _tray?.Dispose();
        Application.Current.Shutdown();
    }

    // 简易四边缩放
    private void Resize_Top(object sender, MouseButtonEventArgs e) => BeginResize(ResizeDirection.Top);
    private void Resize_Bottom(object sender, MouseButtonEventArgs e) => BeginResize(ResizeDirection.Bottom);
    private void Resize_Left(object sender, MouseButtonEventArgs e) => BeginResize(ResizeDirection.Left);
    private void Resize_Right(object sender, MouseButtonEventArgs e) => BeginResize(ResizeDirection.Right);

    private enum ResizeDirection { Top, Bottom, Left, Right }

    private void BeginResize(ResizeDirection dir)
    {
        // 使用原生 WM_SYSCOMMAND 更稳妥；P0 用简单高度/宽度调整
        if (Mouse.LeftButton != MouseButtonState.Pressed) return;
        var start = PointToScreen(Mouse.GetPosition(this));
        var startW = Width;
        var startH = Height;
        var startL = Left;
        var startT = Top;

        MouseEventHandler? move = null;
        MouseButtonEventHandler? up = null;

        move = (_, _) =>
        {
            var cur = PointToScreen(Mouse.GetPosition(this));
            var dx = cur.X - start.X;
            var dy = cur.Y - start.Y;
            switch (dir)
            {
                case ResizeDirection.Bottom:
                    Height = Math.Max(MinHeight, startH + dy);
                    break;
                case ResizeDirection.Top:
                    var nh = Math.Max(MinHeight, startH - dy);
                    Top = startT + (startH - nh);
                    Height = nh;
                    break;
                case ResizeDirection.Right:
                    Width = Math.Max(MinWidth, startW + dx);
                    break;
                case ResizeDirection.Left:
                    var nw = Math.Max(MinWidth, startW - dx);
                    Left = startL + (startW - nw);
                    Width = nw;
                    break;
            }
        };
        up = (_, _) =>
        {
            MouseMove -= move;
            MouseUp -= up;
            Mouse.Capture(null);
        };
        Mouse.Capture(this);
        MouseMove += move;
        MouseUp += up;
    }
}
