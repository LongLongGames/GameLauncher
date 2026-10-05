using System.Windows;

namespace GameLauncher.Views;

public partial class UpdateDialog : Window
{
    public bool ShouldUpdate { get; private set; }

    public UpdateDialog(string currentVersion, string newVersion)
    {
        InitializeComponent();
        MessageBlock.Text =
            "检测到新版本启动器。更新会关闭当前窗口并自动重启，正在进行的操作将被中断。";
        VersionBlock.Text = $"当前版本  v{currentVersion}    →    新版本  v{newVersion}";
    }

    private void Update_Click(object sender, RoutedEventArgs e)
    {
        ShouldUpdate = true;
        DialogResult = true;
        Close();
    }

    private void Later_Click(object sender, RoutedEventArgs e)
    {
        ShouldUpdate = false;
        DialogResult = false;
        Close();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Later_Click(sender, e);
}
