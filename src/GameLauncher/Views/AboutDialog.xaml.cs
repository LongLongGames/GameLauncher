using System.Windows;
using GameLauncher.Services;

namespace GameLauncher.Views;

public partial class AboutDialog : Window
{
    public AboutDialog(string version, bool isInstalled)
    {
        InitializeComponent();
        VersionBlock.Text = $"版本  v{version}";
        ChannelBlock.Text = isInstalled
            ? "已通过安装器安装（可自动更新）"
            : "开发/便携运行（不会自动更新）";
    }

    private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
    {
        var btn = sender as System.Windows.Controls.Button;
        if (btn is not null) btn.IsEnabled = false;
        try
        {
            var svc = App.Services.GetService(typeof(IUpdateService)) as IUpdateService;
            if (svc is null) return;

            var result = await svc.CheckAsync();
            if (!result.UpdateAvailable || result.Info is null)
            {
                System.Windows.MessageBox.Show(
                    result.IsInstalled ? "当前已是最新版本。" : "当前不是安装版，无法从 Release 更新。",
                    "检查更新",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var dlg = new UpdateDialog(result.CurrentVersion, result.NewVersion!);
            dlg.Owner = this;
            if (dlg.ShowDialog() == true && dlg.ShouldUpdate)
            {
                await svc.DownloadAndApplyAsync(result.Info);
            }
        }
        finally
        {
            if (btn is not null) btn.IsEnabled = true;
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
