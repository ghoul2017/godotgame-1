using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GodotGame;

public sealed class OrbitTransactionService
{
    private const string TradeOfferResourceRoot = "res://resources/data/trade_offers";
    private const string ResearchProjectResourceRoot = "res://resources/data/research_projects";
    private readonly GameSession _session;
    private readonly DataRegistry _registry;
    private readonly List<TradeOfferData> _tradeOffers;
    private readonly List<ResearchProjectData> _researchProjects;

    public OrbitTransactionService(GameSession session, DataRegistry registry)
    {
        _session = session;
        _registry = registry;
        _tradeOffers = LoadTradeOffersFromResources();
        if (_tradeOffers.Count == 0)
        {
            _tradeOffers = CreateTradeOffers();
        }

        _researchProjects = LoadResearchProjectsFromResources();
        if (_researchProjects.Count == 0)
        {
            _researchProjects = CreateResearchProjects();
        }
    }

    public IReadOnlyList<TradeOfferData> TradeOffers => _tradeOffers;
    public IReadOnlyList<ResearchProjectData> ResearchProjects => _researchProjects;

    public OrbitActionEvaluation EvaluateTrade(TradeOfferData offer)
    {
        if (offer.StockLimit > 0 && CountSuccessfulTransactions("trade", offer.Id) >= offer.StockLimit)
        {
            return OrbitActionEvaluation.Blocked("已售罄", "交易库存已售罄");
        }

        string prerequisiteIssue = CheckBlueprintProtocolStoryPrerequisites(
            offer.RequiredBlueprintIds,
            offer.RequiredProtocolIds,
            offer.RequiredStoryFlags);
        if (!string.IsNullOrEmpty(prerequisiteIssue))
        {
            return OrbitActionEvaluation.Blocked("前置不足", prerequisiteIssue);
        }

        string affordabilityIssue = CheckCost(offer.GetCostItems(), offer.CostCredits);
        if (!string.IsNullOrEmpty(affordabilityIssue))
        {
            return OrbitActionEvaluation.Blocked("资源不足", affordabilityIssue);
        }

        string rewardIssue = CheckRewardCapacity(offer.GetRewardItems(), offer.GetCostItems());
        if (!string.IsNullOrEmpty(rewardIssue))
        {
            return OrbitActionEvaluation.Blocked("库存不足", rewardIssue);
        }

        return OrbitActionEvaluation.Ready();
    }

    public OrbitActionEvaluation EvaluateResearch(ResearchProjectData project)
    {
        if (IsResearchCompleted(project.Id) || AreAllUnlocksPresent(project))
        {
            return OrbitActionEvaluation.Completed();
        }

        foreach (string requiredResearchId in project.RequiredResearchIds)
        {
            if (!IsResearchCompleted(requiredResearchId))
            {
                return OrbitActionEvaluation.Blocked("前置不足", $"前置研发未完成：{requiredResearchId}");
            }
        }

        foreach (string storyFlag in project.RequiredStoryFlags)
        {
            if (!_session.OrbitState.StoryFlags.TryGetValue(storyFlag, out bool enabled) || !enabled)
            {
                return OrbitActionEvaluation.Blocked("剧情锁定", $"剧情条件未满足：{storyFlag}");
            }
        }

        string affordabilityIssue = CheckCost(project.GetCostItems(), project.CostCredits);
        if (!string.IsNullOrEmpty(affordabilityIssue))
        {
            return OrbitActionEvaluation.Blocked("资源不足", affordabilityIssue);
        }

        return OrbitActionEvaluation.Ready("可研发");
    }

    public OrbitTransactionResult ExecuteTrade(TradeOfferData offer)
    {
        OrbitActionEvaluation evaluation = EvaluateTrade(offer);
        if (!evaluation.CanExecute)
        {
            GD.PushWarning($"[轨道] 交易失败：{offer.Id}，{evaluation.FailureReason}");
            return OrbitTransactionResult.Fail(evaluation.FailureReason);
        }

        InventoryContainer orbitInventory = EnsureOrbitInventory();
        int creditsBefore = _session.OrbitState.Credits;
        InventoryContainer simulatedInventory = CloneInventory(orbitInventory, "orbit_trade_commit_validation");
        Dictionary<string, ItemInstance> generatedInstances = new(_session.ItemInstances);
        List<string> relatedTransferIds = new();
        List<string> rewardInstanceIds = new();
        Dictionary<string, List<string>> rewardInstancesByItem = new();
        List<ItemStack> costStacks = AggregateStacks(offer.GetCostItems());
        IReadOnlyList<ItemStack> rewardStacks = offer.GetRewardItems();

        foreach (ItemStack cost in costStacks)
        {
            if (!simulatedInventory.RemoveStack(cost.ItemId, cost.Count))
            {
                string message = $"交易提交预检失败，库存扣除失败：{cost.ItemId}";
                GD.PushWarning($"[轨道] {message}");
                return OrbitTransactionResult.Fail(message);
            }
        }

        foreach (ItemStack reward in rewardStacks)
        {
            InventoryTransferResult rewardResult = AddRewardToSimulation(simulatedInventory, reward, generatedInstances, rewardInstanceIds, rewardInstancesByItem);
            if (!rewardResult.IsSuccess)
            {
                GD.PushWarning($"[轨道] 交易提交预检失败：{rewardResult.Message}");
                return OrbitTransactionResult.Fail(rewardResult.Message);
            }
        }

        _session.OrbitState.Credits -= offer.CostCredits;
        ReplaceInventoryContents(orbitInventory, simulatedInventory);
        foreach (KeyValuePair<string, ItemInstance> generatedInstance in generatedInstances)
        {
            _session.ItemInstances[generatedInstance.Key] = generatedInstance.Value;
        }

        AddCostTransfers(orbitInventory.InventoryId, "orbit_trade_sink", costStacks, $"orbit_trade_cost:{offer.Id}", relatedTransferIds);
        AddRewardTransfers(orbitInventory.InventoryId, rewardStacks, rewardInstancesByItem, $"orbit_trade_reward:{offer.Id}", relatedTransferIds);

        OrbitTransactionRecord record = CreateRecord("trade", offer.Id, offer.GetCostItems(), offer.CostCredits, creditsBefore, _session.OrbitState.Credits);
        record.RewardItems.AddRange(CopyStacks(rewardStacks));
        record.RewardItemInstanceIds.AddRange(rewardInstanceIds);
        record.RelatedTransferIds.AddRange(relatedTransferIds);
        _session.OrbitTransactionRecords.Add(record);
        GD.Print($"[轨道] 交易完成：{offer.Id}");
        return OrbitTransactionResult.Success(record, "交易完成");
    }

    public OrbitTransactionResult ExecuteResearch(ResearchProjectData project)
    {
        OrbitActionEvaluation evaluation = EvaluateResearch(project);
        if (!evaluation.CanExecute)
        {
            GD.PushWarning($"[轨道] 研发失败：{project.Id}，{evaluation.FailureReason}");
            return OrbitTransactionResult.Fail(evaluation.FailureReason);
        }

        InventoryContainer orbitInventory = EnsureOrbitInventory();
        int creditsBefore = _session.OrbitState.Credits;
        InventoryContainer simulatedInventory = CloneInventory(orbitInventory, "orbit_research_commit_validation");
        List<string> relatedTransferIds = new();
        List<ItemStack> costStacks = AggregateStacks(project.GetCostItems());
        List<string> blueprintIdsBefore = _session.OrbitState.UnlockedBlueprints.ToList();
        List<string> protocolIdsBefore = _session.OrbitState.UnlockedProtocols.ToList();
        List<string> newBlueprintIds = project.UnlockBlueprintIds.Where(id => !_session.OrbitState.UnlockedBlueprints.Contains(id)).ToList();
        List<string> newProtocolIds = project.UnlockProtocolIds.Where(id => !_session.OrbitState.UnlockedProtocols.Contains(id)).ToList();
        List<string> blueprintIdsAfter = blueprintIdsBefore.Union(newBlueprintIds).ToList();
        List<string> protocolIdsAfter = protocolIdsBefore.Union(newProtocolIds).ToList();

        foreach (ItemStack cost in costStacks)
        {
            if (!simulatedInventory.RemoveStack(cost.ItemId, cost.Count))
            {
                string message = $"研发提交预检失败，库存扣除失败：{cost.ItemId}";
                GD.PushWarning($"[轨道] {message}");
                return OrbitTransactionResult.Fail(message);
            }
        }

        _session.OrbitState.Credits -= project.CostCredits;
        ReplaceInventoryContents(orbitInventory, simulatedInventory);
        _session.OrbitState.UnlockedBlueprints.Clear();
        _session.OrbitState.UnlockedBlueprints.AddRange(blueprintIdsAfter);
        _session.OrbitState.UnlockedProtocols.Clear();
        _session.OrbitState.UnlockedProtocols.AddRange(protocolIdsAfter);
        AddCostTransfers(orbitInventory.InventoryId, "orbit_research_sink", costStacks, $"orbit_research_cost:{project.Id}", relatedTransferIds);

        OrbitTransactionRecord record = CreateRecord("research", project.Id, project.GetCostItems(), project.CostCredits, creditsBefore, _session.OrbitState.Credits);
        record.UnlockBlueprintIds.AddRange(newBlueprintIds);
        record.UnlockProtocolIds.AddRange(newProtocolIds);
        record.BlueprintIdsBefore.AddRange(blueprintIdsBefore);
        record.BlueprintIdsAfter.AddRange(blueprintIdsAfter);
        record.ProtocolIdsBefore.AddRange(protocolIdsBefore);
        record.ProtocolIdsAfter.AddRange(protocolIdsAfter);
        record.RelatedTransferIds.AddRange(relatedTransferIds);
        _session.OrbitTransactionRecords.Add(record);
        GD.Print($"[轨道] 研发完成：{project.Id}");
        return OrbitTransactionResult.Success(record, "研发完成");
    }

    public bool IsResearchCompleted(string projectId)
    {
        return _session.OrbitTransactionRecords.Any(record =>
            record.TransactionType == "research" &&
            record.SourceId == projectId &&
            record.Result == "success");
    }

    private OrbitTransactionRecord CreateRecord(string transactionType, string sourceId, IReadOnlyList<ItemStack> costItems, int costCredits, int creditsBefore, int creditsAfter)
    {
        OrbitTransactionRecord record = new()
        {
            TransactionId = Guid.NewGuid().ToString("N"),
            TransactionType = transactionType,
            SourceId = sourceId,
            CostCredits = costCredits,
            CreditsBefore = creditsBefore,
            CreditsAfter = creditsAfter,
            CreatedAt = DateTime.UtcNow,
            Result = "success"
        };
        record.CostItems.AddRange(CopyStacks(costItems));
        return record;
    }

    private InventoryTransferResult AddRewardToSimulation(
        InventoryContainer simulatedInventory,
        ItemStack reward,
        Dictionary<string, ItemInstance> generatedInstances,
        List<string> rewardInstanceIds,
        Dictionary<string, List<string>> rewardInstancesByItem)
    {
        if (!_registry.TryGetItem(reward.ItemId, out ItemData? itemData) || itemData is null)
        {
            return InventoryTransferResult.Fail(InventoryTransferStatus.MissingDefinition, $"交易奖励缺失道具定义：{reward.ItemId}");
        }

        if (!itemData.RequiresInstance)
        {
            return simulatedInventory.AddStack(new ItemStack { ItemId = reward.ItemId, Count = reward.Count }, _registry);
        }

        for (int index = 0; index < reward.Count; index++)
        {
            string instanceId = $"{reward.ItemId}_{Guid.NewGuid():N}";
            generatedInstances[instanceId] = new ItemInstance
            {
                InstanceId = instanceId,
                ItemId = reward.ItemId,
                Durability = 100,
                Quality = "standard"
            };
            InventoryTransferResult addResult = simulatedInventory.AddItemInstance(instanceId, generatedInstances, _registry);
            if (!addResult.IsSuccess)
            {
                return addResult;
            }

            rewardInstanceIds.Add(instanceId);
            if (!rewardInstancesByItem.TryGetValue(reward.ItemId, out List<string>? itemInstanceIds))
            {
                itemInstanceIds = new List<string>();
                rewardInstancesByItem[reward.ItemId] = itemInstanceIds;
            }

            itemInstanceIds.Add(instanceId);
        }

        return InventoryTransferResult.Success(new InventoryTransfer
        {
            TransferId = Guid.NewGuid().ToString("N"),
            ToInventoryId = simulatedInventory.InventoryId,
            ItemId = reward.ItemId,
            Count = reward.Count
        });
    }

    private void AddCostTransfers(string fromInventoryId, string toInventoryId, IReadOnlyList<ItemStack> costStacks, string reason, List<string> relatedTransferIds)
    {
        foreach (ItemStack cost in costStacks)
        {
            InventoryTransfer transfer = new()
            {
                TransferId = Guid.NewGuid().ToString("N"),
                FromInventoryId = fromInventoryId,
                ToInventoryId = toInventoryId,
                ItemId = cost.ItemId,
                Count = cost.Count,
                Reason = reason
            };
            _session.InventoryTransfers.Add(transfer);
            relatedTransferIds.Add(transfer.TransferId);
        }
    }

    private void AddRewardTransfers(
        string orbitInventoryId,
        IReadOnlyList<ItemStack> rewardStacks,
        IReadOnlyDictionary<string, List<string>> rewardInstancesByItem,
        string reason,
        List<string> relatedTransferIds)
    {
        foreach (ItemStack reward in rewardStacks)
        {
            InventoryTransfer transfer = new()
            {
                TransferId = Guid.NewGuid().ToString("N"),
                ToInventoryId = orbitInventoryId,
                ItemId = reward.ItemId,
                Count = reward.Count,
                Reason = reason
            };
            if (rewardInstancesByItem.TryGetValue(reward.ItemId, out List<string>? itemInstanceIds))
            {
                transfer.ItemInstanceIds.AddRange(itemInstanceIds);
            }

            _session.InventoryTransfers.Add(transfer);
            relatedTransferIds.Add(transfer.TransferId);
        }
    }

    private static void ReplaceInventoryContents(InventoryContainer target, InventoryContainer source)
    {
        target.ItemStacks.Clear();
        foreach (ItemStack stack in source.ItemStacks)
        {
            target.ItemStacks.Add(new ItemStack
            {
                ItemId = stack.ItemId,
                Count = stack.Count
            });
        }

        target.ItemInstanceIds.Clear();
        target.ItemInstanceIds.AddRange(source.ItemInstanceIds);
    }

    private string CheckBlueprintProtocolStoryPrerequisites(IReadOnlyList<string> blueprintIds, IReadOnlyList<string> protocolIds, IReadOnlyList<string> storyFlags)
    {
        foreach (string blueprintId in blueprintIds)
        {
            if (!_session.OrbitState.UnlockedBlueprints.Contains(blueprintId))
            {
                return $"缺少蓝图：{blueprintId}";
            }
        }

        foreach (string protocolId in protocolIds)
        {
            if (!_session.OrbitState.UnlockedProtocols.Contains(protocolId))
            {
                return $"缺少协议：{protocolId}";
            }
        }

        foreach (string storyFlag in storyFlags)
        {
            if (!_session.OrbitState.StoryFlags.TryGetValue(storyFlag, out bool enabled) || !enabled)
            {
                return $"剧情条件未满足：{storyFlag}";
            }
        }

        return string.Empty;
    }

    private string CheckCost(IReadOnlyList<ItemStack> costItems, int costCredits)
    {
        if (_session.OrbitState.Credits < costCredits)
        {
            return $"信用点不足：需要 {costCredits}，当前 {_session.OrbitState.Credits}";
        }

        InventoryContainer orbitInventory = EnsureOrbitInventory();
        foreach (ItemStack cost in AggregateStacks(costItems))
        {
            if (!_registry.TryGetItem(cost.ItemId, out ItemData? itemData) || itemData is null)
            {
                return $"找不到道具定义：{cost.ItemId}";
            }

            if (itemData.RequiresInstance)
            {
                return $"当前消耗不支持按堆叠扣除实例道具：{cost.ItemId}";
            }

            int currentCount = orbitInventory.GetItemCount(cost.ItemId);
            if (currentCount < cost.Count)
            {
                return $"{_registry.GetItemName(cost.ItemId)} 不足：需要 {cost.Count}，当前 {currentCount}";
            }
        }

        return string.Empty;
    }

    private string CheckRewardCapacity(IReadOnlyList<ItemStack> rewardItems, IReadOnlyList<ItemStack> costItems)
    {
        InventoryContainer simulatedInventory = CloneInventory(EnsureOrbitInventory(), "orbit_reward_validation");
        Dictionary<string, ItemInstance> simulatedInstances = new(_session.ItemInstances);
        foreach (ItemStack cost in AggregateStacks(costItems))
        {
            simulatedInventory.RemoveStack(cost.ItemId, cost.Count);
        }

        foreach (ItemStack reward in rewardItems)
        {
            if (!_registry.TryGetItem(reward.ItemId, out ItemData? itemData) || itemData is null)
            {
                return $"找不到奖励道具定义：{reward.ItemId}";
            }

            if (!itemData.RequiresInstance)
            {
                InventoryTransferResult stackResult = simulatedInventory.AddStack(new ItemStack { ItemId = reward.ItemId, Count = reward.Count }, _registry);
                if (!stackResult.IsSuccess)
                {
                    return stackResult.Message;
                }

                continue;
            }

            for (int index = 0; index < reward.Count; index++)
            {
                string instanceId = $"validate_{reward.ItemId}_{index}";
                simulatedInstances[instanceId] = new ItemInstance
                {
                    InstanceId = instanceId,
                    ItemId = reward.ItemId,
                    Durability = 100,
                    Quality = "standard"
                };
                InventoryTransferResult instanceResult = simulatedInventory.AddItemInstance(instanceId, simulatedInstances, _registry);
                if (!instanceResult.IsSuccess)
                {
                    return instanceResult.Message;
                }
            }
        }

        return string.Empty;
    }

    private bool AreAllUnlocksPresent(ResearchProjectData project)
    {
        bool hasUnlocks = project.UnlockBlueprintIds.Count > 0 || project.UnlockProtocolIds.Count > 0;
        return hasUnlocks &&
            project.UnlockBlueprintIds.All(_session.OrbitState.UnlockedBlueprints.Contains) &&
            project.UnlockProtocolIds.All(_session.OrbitState.UnlockedProtocols.Contains);
    }

    private int CountSuccessfulTransactions(string transactionType, string sourceId)
    {
        return _session.OrbitTransactionRecords.Count(record =>
            record.TransactionType == transactionType &&
            record.SourceId == sourceId &&
            record.Result == "success");
    }

    private InventoryContainer EnsureOrbitInventory()
    {
        if (_session.Inventories.TryGetValue(_session.OrbitState.InventoryId, out InventoryContainer? inventory))
        {
            return inventory;
        }

        InventoryContainer created = new()
        {
            InventoryId = _session.OrbitState.InventoryId,
            OwnerType = "orbit_inventory",
            OwnerId = _session.OrbitState.OrbitStateId,
            SlotLimit = 64,
            WeightLimit = 2000f
        };
        _session.Inventories[created.InventoryId] = created;
        return created;
    }

    private static InventoryContainer CloneInventory(InventoryContainer source, string inventoryId)
    {
        InventoryContainer clone = new()
        {
            InventoryId = inventoryId,
            OwnerType = source.OwnerType,
            OwnerId = source.OwnerId,
            SlotLimit = source.SlotLimit,
            WeightLimit = source.WeightLimit
        };
        clone.AcceptedTags.AddRange(source.AcceptedTags);
        clone.BlockedTags.AddRange(source.BlockedTags);
        foreach (ItemStack stack in source.ItemStacks)
        {
            clone.ItemStacks.Add(new ItemStack
            {
                ItemId = stack.ItemId,
                Count = stack.Count
            });
        }

        clone.ItemInstanceIds.AddRange(source.ItemInstanceIds);
        return clone;
    }

    private static List<ItemStack> AggregateStacks(IReadOnlyList<ItemStack> stacks)
    {
        Dictionary<string, int> totals = new();
        foreach (ItemStack stack in stacks)
        {
            totals.TryGetValue(stack.ItemId, out int currentCount);
            totals[stack.ItemId] = currentCount + stack.Count;
        }

        return totals.Select(pair => new ItemStack { ItemId = pair.Key, Count = pair.Value }).ToList();
    }

    private static List<ItemStack> CopyStacks(IReadOnlyList<ItemStack> stacks)
    {
        return stacks.Select(stack => new ItemStack
        {
            ItemId = stack.ItemId,
            Count = stack.Count
        }).ToList();
    }

    private static ItemStack Stack(string itemId, int count)
    {
        return new ItemStack
        {
            ItemId = itemId,
            Count = count
        };
    }

    private static List<TradeOfferData> LoadTradeOffersFromResources()
    {
        List<TradeOfferData> offers = new();
        DirAccess? directory = DirAccess.Open(TradeOfferResourceRoot);
        if (directory is null)
        {
            return offers;
        }

        foreach (string fileName in directory.GetFiles().OrderBy(fileName => fileName, StringComparer.Ordinal))
        {
            if (!fileName.EndsWith(".tres", StringComparison.OrdinalIgnoreCase) &&
                !fileName.EndsWith(".res", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            TradeOfferData? offer = ResourceLoader.Load<TradeOfferData>($"{TradeOfferResourceRoot}/{fileName}");
            if (offer is not null && !string.IsNullOrWhiteSpace(offer.Id))
            {
                offers.Add(offer);
            }
        }

        return offers.OrderBy(offer => offer.Id, StringComparer.Ordinal).ToList();
    }

    private static List<ResearchProjectData> LoadResearchProjectsFromResources()
    {
        List<ResearchProjectData> projects = new();
        DirAccess? directory = DirAccess.Open(ResearchProjectResourceRoot);
        if (directory is null)
        {
            return projects;
        }

        foreach (string fileName in directory.GetFiles().OrderBy(fileName => fileName, StringComparer.Ordinal))
        {
            if (!fileName.EndsWith(".tres", StringComparison.OrdinalIgnoreCase) &&
                !fileName.EndsWith(".res", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            ResearchProjectData? project = ResourceLoader.Load<ResearchProjectData>($"{ResearchProjectResourceRoot}/{fileName}");
            if (project is not null && !string.IsNullOrWhiteSpace(project.Id))
            {
                projects.Add(project);
            }
        }

        return projects.OrderBy(project => project.Id, StringComparer.Ordinal).ToList();
    }

    private static List<TradeOfferData> CreateTradeOffers()
    {
        List<TradeOfferData> offers = new();

        TradeOfferData energy = new()
        {
            Id = "trade_basic_energy_cells",
            DisplayName = "能源块补给",
            Category = "资源补给",
            Description = "从轨道补给商购买稳定能源块，用于空投准备、地表生产和火箭制造。",
            CostCredits = 40,
            IconPath = "res://assets/ui/orbit/trade/trade_basic_energy_cells.svg"
        };
        energy.AddRewardItem("energy_cell", 8);
        offers.Add(energy);

        TradeOfferData repairTools = new()
        {
            Id = "trade_basic_repair_tools",
            DisplayName = "简易维修工具包",
            Category = "工具装备",
            Description = "用基础材料换取可空投的维修工具，提升地表早期维修保障。",
            CostCredits = 25,
            IconPath = "res://assets/ui/orbit/trade/trade_basic_repair_tools.svg"
        };
        repairTools.AddCostItem("metal", 6);
        repairTools.AddRewardItem("repair_tool_basic", 1);
        offers.Add(repairTools);

        TradeOfferData scanner = new()
        {
            Id = "trade_basic_scanner",
            DisplayName = "简易扫描器",
            Category = "探索工具",
            Description = "购买基础扫描器，为地表资源点、废墟和特殊信号调查提供工具入口。",
            CostCredits = 35,
            IconPath = "res://assets/ui/orbit/trade/trade_basic_scanner.svg"
        };
        scanner.AddCostItem("electronic_parts", 4);
        scanner.AddRewardItem("scanner_basic", 1);
        scanner.RequiredProtocolIds.Add("protocol_basic_scanning");
        offers.Add(scanner);

        TradeOfferData serviceBot = new()
        {
            Id = "trade_service_bot_platform",
            DisplayName = "服务型平台机体",
            Category = "单位平台",
            Description = "购买服务型量产机器人平台，后续可在空投配置中转化为远征单位。",
            CostCredits = 60,
            IconPath = "res://assets/ui/orbit/trade/trade_service_bot_platform.svg"
        };
        serviceBot.AddCostItem("metal", 18);
        serviceBot.AddCostItem("electronic_parts", 6);
        serviceBot.AddRewardItem("service_bot_platform", 1);
        offers.Add(serviceBot);

        TradeOfferData aiChip = new()
        {
            Id = "trade_ai_chip_basic",
            DisplayName = "通用 AI 芯片",
            Category = "芯片",
            Description = "购买通用 AI 芯片，用于后续觉醒者、改装和高级单位相关系统。",
            CostCredits = 90,
            IconPath = "res://assets/ui/orbit/trade/trade_ai_chip_basic.svg"
        };
        aiChip.AddCostItem("clean_data", 3);
        aiChip.AddRewardItem("ai_chip_basic", 1);
        offers.Add(aiChip);

        return offers;
    }

    private static List<ResearchProjectData> CreateResearchProjects()
    {
        List<ResearchProjectData> projects = new();

        ResearchProjectData assembly = new()
        {
            Id = "research_basic_assembly",
            DisplayName = "基础组装工艺",
            Category = "建造生产",
            Description = "整理旧世界组装机资料，解锁地表基础组装机的正式建造蓝图。",
            CostCredits = 30,
            IconPath = "res://assets/ui/orbit/research/research_basic_assembly.svg"
        };
        assembly.AddCostItem("metal", 20);
        assembly.AddCostItem("electronic_parts", 4);
        assembly.UnlockBlueprintIds.Add("blueprint_assembler_basic");
        projects.Add(assembly);

        ResearchProjectData repair = new()
        {
            Id = "research_field_repair_protocol",
            DisplayName = "野外维修协议",
            Category = "远征协议",
            Description = "整合维修日志和能源调度策略，解锁后续野外维修效率协议。",
            CostCredits = 45,
            IconPath = "res://assets/ui/orbit/research/research_field_repair_protocol.svg"
        };
        repair.AddCostItem("energy_cell", 4);
        repair.AddCostItem("clean_data", 2);
        repair.UnlockProtocolIds.Add("protocol_field_repair");
        projects.Add(repair);

        ResearchProjectData scanning = new()
        {
            Id = "research_basic_scanning_protocol",
            DisplayName = "基础扫描协议",
            Category = "探索协议",
            Description = "解析废墟信号样本，解锁基础扫描协议和扫描器交易前置。",
            CostCredits = 50,
            IconPath = "res://assets/ui/orbit/research/research_basic_scanning_protocol.svg"
        };
        scanning.AddCostItem("electronic_parts", 6);
        scanning.AddCostItem("clean_data", 2);
        scanning.UnlockProtocolIds.Add("protocol_basic_scanning");
        projects.Add(scanning);

        ResearchProjectData rocket = new()
        {
            Id = "research_rocket_part_fabrication",
            DisplayName = "火箭部件制造",
            Category = "火箭工程",
            Description = "建立地表火箭部件的制造规范，解锁后续火箭制造配方蓝图。",
            CostCredits = 80,
            IconPath = "res://assets/ui/orbit/research/research_rocket_part_fabrication.svg"
        };
        rocket.AddCostItem("alloy", 8);
        rocket.AddCostItem("electronic_parts", 8);
        rocket.AddCostItem("clean_data", 3);
        rocket.RequiredResearchIds.Add("research_basic_assembly");
        rocket.UnlockBlueprintIds.Add("blueprint_rocket_part_fabrication");
        projects.Add(rocket);

        ResearchProjectData dropCapacity = new()
        {
            Id = "research_drop_pod_capacity_1",
            DisplayName = "空投舱载荷审计 I",
            Category = "空投能力",
            Description = "优化空投舱载荷审计和结构加固规则，作为后续容量改进的正式入口。",
            CostCredits = 100,
            IconPath = "res://assets/ui/orbit/research/research_drop_pod_capacity_1.svg"
        };
        dropCapacity.AddCostItem("alloy", 6);
        dropCapacity.AddCostItem("energy_cell", 8);
        dropCapacity.AddCostItem("clean_data", 4);
        dropCapacity.RequiredResearchIds.Add("research_basic_scanning_protocol");
        dropCapacity.UnlockBlueprintIds.Add("blueprint_drop_pod_capacity_1");
        dropCapacity.UnlockProtocolIds.Add("protocol_drop_mass_audit_1");
        projects.Add(dropCapacity);

        return projects;
    }
}
