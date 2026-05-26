using Godot;

namespace GodotGame;

public sealed class DebugLauncher
{
    public bool IsDebugEnabled()
    {
        return OS.HasFeature("editor") || HasArgument("--debug-scene");
    }

    public string GetStartupScene()
    {
        string scene = GetArgumentValue("--debug-scene");
        return scene switch
        {
            SceneId.OrbitStation => SceneId.OrbitStation,
            SceneId.SurfaceExpedition => SceneId.SurfaceExpedition,
            SceneId.ReturnSummary => SceneId.ReturnSummary,
            SceneId.Prologue => SceneId.Prologue,
            _ => string.Empty
        };
    }

    public int GetSeed()
    {
        string seedText = GetArgumentValue("--seed");
        return int.TryParse(seedText, out int seed) ? seed : 460001;
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
