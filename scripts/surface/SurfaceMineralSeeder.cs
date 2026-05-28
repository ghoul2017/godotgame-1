using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GodotGame;

public static class SurfaceMineralSeeder
{
    private static readonly Vector2I[] VisibleOffsets =
    {
        new(-220, -120),
        new(180, -116),
        new(-150, 142),
        new(230, 128)
    };

    public static int EnsureInitialMinerals(ExpeditionState expeditionState, DataRegistry registry)
    {
        List<MineralDepositData> visibleMinerals = registry.MineralDeposits.Values
            .Where(mineral => mineral.RequiresScanLevel <= 0)
            .OrderBy(mineral => mineral.Id)
            .ToList();
        int createdCount = 0;
        for (int index = 0; index < visibleMinerals.Count; index++)
        {
            MineralDepositData mineralData = visibleMinerals[index];
            if (expeditionState.MineralDepositStates.Values.Any(instance => instance.MineralDepositId == mineralData.Id))
            {
                continue;
            }

            Vector2I offset = VisibleOffsets[index % VisibleOffsets.Length];
            Vector2I position = expeditionState.DropPosition + offset + SeedJitter(expeditionState.Seed, index);
            string instanceId = UniqueMineralId(expeditionState, mineralData.Id);
            MineralDepositInstance instance = new()
            {
                MineralDepositInstanceId = instanceId,
                MineralDepositId = mineralData.Id,
                ExpeditionId = expeditionState.ExpeditionId,
                Position = position,
                RemainingYield = mineralData.MaxYield,
                IsDiscovered = true,
                SourceCoordinateId = expeditionState.TargetCoordinateId,
                CreatedBySeed = expeditionState.Seed
            };
            expeditionState.MineralDepositStates[instance.MineralDepositInstanceId] = instance;
            if (!expeditionState.MapState.DiscoveredMineralDepositIds.Contains(instance.MineralDepositInstanceId))
            {
                expeditionState.MapState.DiscoveredMineralDepositIds.Add(instance.MineralDepositInstanceId);
            }

            createdCount++;
        }

        if (createdCount > 0)
        {
            GD.Print($"[矿产] 首批正式矿产点实例化：{createdCount}");
        }

        return createdCount;
    }

    private static Vector2I SeedJitter(int seed, int index)
    {
        int x = ((seed + index * 37) % 41) - 20;
        int y = ((seed / 7 + index * 29) % 41) - 20;
        return new Vector2I(x, y);
    }

    private static string UniqueMineralId(ExpeditionState expeditionState, string mineralId)
    {
        string baseId = $"mineral_{expeditionState.ExpeditionId}_{mineralId}";
        if (!expeditionState.MineralDepositStates.ContainsKey(baseId))
        {
            return baseId;
        }

        int index = 1;
        string candidate;
        do
        {
            candidate = $"{baseId}_{index}";
            index++;
        }
        while (expeditionState.MineralDepositStates.ContainsKey(candidate));

        return candidate;
    }
}
