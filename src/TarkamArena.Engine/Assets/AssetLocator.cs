namespace TarkamArena.Engine.Assets;

/// <summary>
/// Finds the <c>assets</c> folder: next to the executable in a published build, or in the repo
/// root during development. Override with the <c>TARKAM_ASSETS</c> environment variable.
/// </summary>
public static class AssetLocator
{
    private static readonly Lazy<string> s_root = new(FindRoot);

    public static string Root => s_root.Value;

    public static string PathOf(string relative) => System.IO.Path.Combine(Root, relative.Replace('/', System.IO.Path.DirectorySeparatorChar));

    public static bool Exists(string relative) => File.Exists(PathOf(relative));

    private static string FindRoot()
    {
        var env = Environment.GetEnvironmentVariable("TARKAM_ASSETS");
        if (!string.IsNullOrEmpty(env) && Directory.Exists(env)) return env;

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = System.IO.Path.Combine(dir.FullName, "assets");
            if (Directory.Exists(System.IO.Path.Combine(candidate, "models")) || Directory.Exists(System.IO.Path.Combine(candidate, "fonts")))
                return candidate;
            dir = dir.Parent;
        }
        return System.IO.Path.Combine(AppContext.BaseDirectory, "assets");
    }
}
