using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
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
        ShowLogin();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // 确保无系统标题栏
        WindowStyle = WindowStyle.None;
    }

    private void InitTray()
    {
        _tray = new NotifyIcon
        {
            Text = "GameLauncher",
            Visible = true,
            Icon = SystemIcons.Application
        };
        _tray.DoubleClick += (_, _) => RestoreFromTray();
        var menu = new ContextMenuStrip();
        menu.Items.Add("显示", null, (_, _) => RestoreFromTray());
        menu.Items.Add("退出", null, (_, _) =>
        {
            _reallyExit = true;
            Close();
        });
        _tray.ContextMenuStrip = menu;
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

    private void ToggleVisibility_Click(object sender, RoutedEventArgs e)
    {
        if (IsVisible)
        {
            Hide();
            _tray!.ShowBalloonTip(1500, "GameLauncher", "已隐藏到托盘，双击图标可恢复。", ToolTipIcon.Info);
        }
        else
        {
            RestoreFromTray();
        }
    }

    private void RestoreFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        _reallyExit = true;
        Close();
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!_reallyExit)
        {
            e.Cancel = true;
            Hide();
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
