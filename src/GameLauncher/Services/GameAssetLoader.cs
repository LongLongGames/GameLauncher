using System.IO;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GameLauncher.Services;

/// <summary>从输出目录 / 程序集旁 / pack Resource 加载封面与图标。</summary>
public static class GameAssetLoader
{
    private static readonly string[] Exts = [".jpg", ".jpeg", ".png", ".webp", ".bmp"];

    public static ImageSource? LoadCover(string? gameId)
        => Load("covers", gameId) ?? Load("covers", "_default");

    public static ImageSource? LoadIcon(string? gameId)
        => Load("icons", gameId);

    public static ImageSource? Load(string folder, string? fileBase)
    {
        if (string.IsNullOrWhiteSpace(fileBase)) return null;
        fileBase = fileBase.Trim();

        foreach (var root in CandidateRoots())
        {
            var dir = Path.Combine(root, "Assets", folder);
            foreach (var ext in Exts)
            {
                var path = Path.Combine(dir, fileBase + ext);
                if (!File.Exists(path)) continue;
                var img = FromFile(path);
                if (img != null) return img;
            }
        }

        foreach (var ext in Exts)
        {
            var pack = $"pack://application:,,,/Assets/{folder}/{fileBase}{ext}";
            try
            {
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.UriSource = new Uri(pack, UriKind.Absolute);
                bi.EndInit();
                bi.Freeze();
                return bi;
            }
            catch { }
        }

        return null;
    }

    public static string DebugProbe(string folder, string fileBase)
    {
        var tried = new List<string>();
        foreach (var root in CandidateRoots())
        {
            var dir = Path.Combine(root, "Assets", folder);
            tried.Add(dir);
            foreach (var ext in Exts)
            {
                var path = Path.Combine(dir, fileBase + ext);
                if (File.Exists(path)) return "FOUND: " + path;
            }
        }
        return "NOT FOUND. BaseDirectory=" + AppContext.BaseDirectory
               + " | tried: " + string.Join(" ; ", tried);
    }

    private static IEnumerable<string> CandidateRoots()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void add(string? p)
        {
            if (string.IsNullOrWhiteSpace(p)) return;
            try { set.Add(Path.GetFullPath(p)); } catch { set.Add(p!); }
        }

        add(AppContext.BaseDirectory);
        add(AppDomain.CurrentDomain.BaseDirectory);
        try
        {
            var asm = Assembly.GetExecutingAssembly().Location;
            if (!string.IsNullOrEmpty(asm))
                add(Path.GetDirectoryName(asm));
        }
        catch { }

        add(Directory.GetCurrentDirectory());
        add(Path.Combine(Directory.GetCurrentDirectory(), "src", "GameLauncher"));
        return set;
    }

    private static ImageSource? FromFile(string path)
    {
        try
        {
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            bi.UriSource = new Uri(path, UriKind.Absolute);
            bi.EndInit();
            bi.Freeze();
            return bi;
        }
        catch { return null; }
    }
}
