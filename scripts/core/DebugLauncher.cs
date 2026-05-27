using Godot;

namespace GodotGame;

public sealed class DebugLauncher
{
    public bool IsDebugEnabled()
    {
        return HasArgument("--debug") || HasArgument("--debug-scene") || OS.GetEnvironment("GODOTGAME_DEBUG") == "1";
    }

    public string GetStartupScene()
    {
        string scene = GetArgumentValue("--debug-scene");
        return scene switch
        {
            SceneId.OrbitStation => SceneId.OrbitStation,
            SceneId.DropConfig => SceneId.DropConfig,
            SceneId.SurfaceExpedition => SceneId.SurfaceExpedition,
            SceneId.ReturnSummary => SceneId.ReturnSummary,
            _ => string.Empty
        };
    }

    public int GetSeed()
    {
        string seedText = GetArgumentValue("--seed");
        return int.TryParse(seedText, out int seed) ? seed : 460001;
    }

    public string GetOrbitPage()
    {
        string page = GetArgumentValue("--orbit-page");
        return string.IsNullOrWhiteSpace(page) ? "inventory" : page;
    }

    private static bool HasArgument(string key)
    {
        foreach (string argument in OS.GetCmdlineArgs())
        {
            if (argument == key || argument.StartsWith($"{key}=", System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetArgumentValue(string key)
    {
        foreach (string argument in OS.GetCmdlineArgs())
        {
            string prefix = $"{key}=";
            if (argument.StartsWith(prefix, System.StringComparison.Ordinal))
            {
                return argument[prefix.Length..];
            }
        }

        return string.Empty;
    }
}
