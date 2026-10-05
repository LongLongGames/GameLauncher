using System;
using Velopack;

namespace GameLauncher;

/// <summary>
/// Explicit entry point so Velopack hooks run before any WPF/UI code.
/// Required by Velopack: VelopackApp.Build().Run() must be first.
/// </summary>
public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Must be the first thing that runs (install / update / uninstall hooks).
        VelopackApp.Build().Run();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
