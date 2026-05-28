using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GodotGame;

public static class SurfaceExpeditionValidator
{
    public static bool TryValidate(
        ScenePayload payload,
        GameSession session,
        out ExpeditionState? expeditionState,
        out string message)
    {
        expeditionState = session.ActiveExpedition;
        ExpeditionStartPayloadData? expeditionData = payload.ExpeditionStartData;
        if (expeditionData is null)
        {
            message = "缺少地表远征载荷。";
            return false;
        }

        if (expeditionState is null)
        {
            message = "缺少当前远征状态。";
            return false;
        }

        if (!session.DropPlans.TryGetValue(expeditionData.DropPlanId, out DropPlan? dropPlan))
        {
            message = $"找不到空投计划：{expeditionData.DropPlanId}";
            return false;
        }

        if (expeditionData.ExpeditionId != expeditionState.ExpeditionId ||
            expeditionData.DropPlanId != expeditionState.DropPlanId ||
            expeditionData.Seed != expeditionState.Seed ||
            expeditionData.Seed != dropPlan.Seed ||
            expeditionData.TargetCoordinateId != expeditionState.TargetCoordinateId ||
            expeditionData.TargetCoordinateId != dropPlan.TargetCoordinateId ||
            expeditionData.DropPosition != expeditionState.DropPosition ||
            expeditionData.DropPosition != dropPlan.TargetCoordinate ||
            expeditionData.DropPodCargoInventoryId != expeditionState.DropPodCargoInventoryId)
        {
            message = "地表载荷、当前远征和空投计划核心字段不一致。";
            return false;
        }

        if (!expeditionState.LocationInventoryIds.Contains(expeditionData.DropPodCargoInventoryId) ||
            !session.Inventories.ContainsKey(expeditionData.DropPodCargoInventoryId))
        {
            message = $"空投货物库存缺失：{expeditionData.DropPodCargoInventoryId}";
            return false;
        }

        List<string> plannedUnitIds = dropPlan.SelectedAwakenedUnitInstanceIds
            .Concat(dropPlan.SelectedMassUnitInstanceIds)
            .Concat(dropPlan.CreatedUnitInstanceIds)
            .ToList();
        if (HasDuplicate(expeditionData.ActiveUnitInstanceIds) ||
            HasDuplicate(expeditionState.ActiveUnitInstanceIds) ||
            HasDuplicate(plannedUnitIds))
        {
            message = "地表载荷、当前远征或空投计划中存在重复单位。";
            return false;
        }

        if (!SameSet(expeditionData.ActiveUnitInstanceIds, expeditionState.ActiveUnitInstanceIds))
        {
            message = "地表载荷单位集合与当前远征单位集合不一致。";
            return false;
        }

        if (!SameSet(expeditionState.ActiveUnitInstanceIds, plannedUnitIds))
        {
            message = "当前远征单位集合无法由空投计划推导。";
            return false;
        }

        foreach (string unitInstanceId in expeditionState.ActiveUnitInstanceIds)
        {
            if (!session.UnitInstances.TryGetValue(unitInstanceId, out UnitInstance? unitInstance) ||
                unitInstance.LockedByExpeditionId != expeditionState.ExpeditionId)
            {
                message = $"远征单位实例不属于当前远征：{unitInstanceId}";
                return false;
            }
        }

        message = "地表远征载荷校验通过。";
        return true;
    }

    private static bool SameSet(IReadOnlyList<string> left, IReadOnlyList<string> right)
    {
        return left.Count == right.Count && left.OrderBy(value => value).SequenceEqual(right.OrderBy(value => value));
    }

    private static bool HasDuplicate(IReadOnlyList<string> values)
    {
        return values.Count != values.Distinct().Count();
    }
}
