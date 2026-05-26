using System.Collections.Generic;
using Godot;

namespace GodotGame;

public sealed class SceneRouter
{
    private readonly Node _sceneContainer;
    private readonly Dictionary<string, string> _scenePaths = new()
    {
        [SceneId.OrbitStation] = "res://scenes/orbit/orbit_station.tscn",
        [SceneId.SurfaceExpedition] = "res://scenes/surface/surface_expedition.tscn",
        [SceneId.ReturnSummary] = "res://scenes/summary/return_summary.tscn"
    };

    public SceneRouter(Node sceneContainer)
    {
        _sceneContainer = sceneContainer;
    }

    public bool ChangeScene(string targetScene, ScenePayload payload)
    {
        if (!_scenePaths.TryGetValue(targetScene, out string? scenePath))
        {
            GD.PushError($"[场景] 未注册的目标场景：{targetScene}");
            return false;
        }

        PackedScene? packedScene = ResourceLoader.Load<PackedScene>(scenePath);
        if (packedScene is null)
        {
            GD.PushError($"[场景] 场景加载失败：{scenePath}");
            return false;
        }

        foreach (Node child in _sceneContainer.GetChildren())
        {
            child.QueueFree();
        }

        Node sceneRoot = packedScene.Instantiate();
        _sceneContainer.AddChild(sceneRoot);

        payload.TargetScene = targetScene;
        if (sceneRoot is ScenePayloadReceiver receiver)
        {
            receiver.ReceivePayload(payload);
        }

        GD.Print($"[场景] 切换完成：{targetScene}");
        return true;
    }
}
