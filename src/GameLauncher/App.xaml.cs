using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using GameLauncher.Services;
using GameLauncher.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Application = System.Windows.Application;

namespace GameLauncher;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var config = LoadConfig();
        var services = new ServiceCollection();

        services.AddSingleton(config);
        services.AddSingleton(new HttpClient
        {
            BaseAddress = new Uri(config.MpBaseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(30)
        });
        services.AddSingleton<IMpAuthService, MpAuthService>();
        services.AddSingleton<ICatalogService, CatalogService>();
        services.AddSingleton<IGameInstallService, GameInstallService>();
        services.AddSingleton<SessionState>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<GameDetailViewModel>();

        Services = services.BuildServiceProvider();
    }

    private static AppConfig LoadConfig()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path))
            return new AppConfig();

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
        }
        catch
        {
            return new AppConfig();
        }
    }
}

public sealed class AppConfig
{
    public string MpBaseUrl { get; set; } = "http://localhost:11080";
    public string AppId { get; set; } = "game_launcher";
    public string DeviceId { get; set; } = "pc-launcher-p0";
}
