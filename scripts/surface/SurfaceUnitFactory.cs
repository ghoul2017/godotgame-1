using System.Collections.Generic;
using Godot;

namespace GodotGame;

public sealed class SurfaceUnitFactory
{
    private readonly GameSession _session;
    private readonly DataRegistry _registry;

    public SurfaceUnitFactory(GameSession session, DataRegistry registry)
    {
        _session = session;
        _registry = registry;
    }

    public List<SurfaceUnit> CreateUnits(ExpeditionState expeditionState, Node2D unitLayer)
    {
        List<SurfaceUnit> units = new();
        int count = expeditionState.ActiveUnitInstanceIds.Count;
        for (int index = 0; index < count; index++)
        {
            string unitInstanceId = expeditionState.ActiveUnitInstanceIds[index];
            if (!_session.UnitInstances.TryGetValue(unitInstanceId, out UnitInstance? unitInstance) ||
                !_registry.TryGetUnit(unitInstance.UnitId, out UnitData? unitData) ||
                unitData is null)
            {
                GD.PushWarning($"[地表] 单位实例缺少定义，无法实例化：{unitInstanceId}");
                continue;
            }

            SurfaceUnitRuntimeState runtimeState = EnsureRuntimeState(expeditionState, unitInstanceId, index, count);
            SurfaceUnit surfaceUnit = new();
            surfaceUnit.Configure(unitInstance, unitData, runtimeState);
            unitLayer.AddChild(surfaceUnit);
            units.Add(surfaceUnit);
        }

        return units;
    }

    private static SurfaceUnitRuntimeState EnsureRuntimeState(ExpeditionState expeditionState, string unitInstanceId, int index, int count)
    {
        if (expeditionState.UnitRuntimeStates.TryGetValue(unitInstanceId, out SurfaceUnitRuntimeState? runtimeState))
        {
            return runtimeState;
        }

        float angle = count <= 1 ? 0f : Mathf.Tau * index / count;
        float radius = count <= 1 ? 0f : 72f;
        Vector2 dropPosition = new(expeditionState.DropPosition.X, expeditionState.DropPosition.Y);
        Vector2 position = dropPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        runtimeState = new SurfaceUnitRuntimeState
        {
            UnitInstanceId = unitInstanceId,
            ExpeditionId = expeditionState.ExpeditionId,
            Position = position,
            LastReachablePosition = position,
            MovementState = "idle",
            IsControllable = true
        };
        expeditionState.UnitRuntimeStates[unitInstanceId] = runtimeState;
        return runtimeState;
    }
}
