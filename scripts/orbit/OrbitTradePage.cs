using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GodotGame;

public partial class OrbitStation
{
    private void BuildTradePage()
    {
        SetPageTitle("交易 | 轨道补给与装备来源");
        AddHintFilter("首批正式交易条目");

        if (_transactionService is null)
        {
            AddEmptyListMessage("交易服务未接入。");
            ShowEmptyDetail("交易不可用", "主入口尚未提供 OrbitTransactionService。");
            return;
        }

        IReadOnlyList<TradeOfferData> offers = _transactionService.TradeOffers;
        if (string.IsNullOrEmpty(_selectedId) || offers.All(offer => offer.Id != _selectedId))
        {
            _selectedId = offers[0].Id;
        }

        foreach (TradeOfferData offer in offers)
        {
            OrbitActionEvaluation evaluation = _transactionService.EvaluateTrade(offer);
            Button button = CreateRowButton(
                offer.Id,
                $"{offer.DisplayName}\n{offer.Category}  消耗 {FormatCost(offer.GetCostItems(), offer.CostCredits)}  获得 {FormatStacks(offer.GetRewardItems())}  状态 {evaluation.StatusText}",
                offer.IconPath,
                () =>
                {
                    _selectedId = offer.Id;
                    _pendingActionId = string.Empty;
                    PlayAudio(UiAssets.OrbitAudioSelect);
                    RefreshCurrentPage();
                });
            button.ButtonPressed = offer.Id == _selectedId;
            _listContainer?.AddChild(WrapWithStatusIcon(button, StatusIconFor(evaluation), $"{offer.Id}Status"));
        }

        TradeOfferData selected = offers.First(offer => offer.Id == _selectedId);
        ShowTradeDetail(selected);
    }

    private void ShowTradeDetail(TradeOfferData offer)
    {
        if (_transactionService is null)
        {
            return;
        }

        OrbitActionEvaluation evaluation = _transactionService.EvaluateTrade(offer);
        bool pending = _pendingActionId == offer.Id;
        string preview = BuildCreditPreview(offer.CostCredits);
        SetDetail(offer.DisplayName, offer.IconPath, string.Join("\n", new[]
        {
            offer.Description,
            $"分类：{offer.Category}",
            $"消耗：{FormatCost(offer.GetCostItems(), offer.CostCredits)}",
            $"获得：{FormatStacks(offer.GetRewardItems())}",
            $"前置蓝图：{FormatIds(offer.RequiredBlueprintIds)}",
            $"前置协议：{FormatIds(offer.RequiredProtocolIds)}",
            $"剧情条件：{FormatIds(offer.RequiredStoryFlags)}",
            $"库存限制：{(offer.StockLimit <= 0 ? "不限" : offer.StockLimit.ToString())}",
            $"当前状态：{evaluation.StatusText}",
            string.IsNullOrEmpty(evaluation.FailureReason) ? preview : $"失败原因：{evaluation.FailureReason}",
            pending ? "确认预览：再次确认后将扣除消耗并写入轨道永久库存，审计记录会关联库存转移。" : "点击执行交易会先进入确认状态。"
        }));

        ConfigureAction(pending ? "确认交易" : "执行交易", evaluation.CanExecute, evaluation.FailureReason, () =>
        {
            if (_pendingActionId != offer.Id)
            {
                _pendingActionId = offer.Id;
                SetFeedback("请确认交易消耗和获得内容。");
                PlayAudio(UiAssets.OrbitAudioDialogOpen);
                RefreshCurrentPage();
                return;
            }

            OrbitTransactionResult result = _transactionService.ExecuteTrade(offer);
            _pendingActionId = string.Empty;
            SetFeedback(result.Message);
            PlayAudio(result.IsSuccess ? UiAssets.OrbitAudioSuccess : UiAssets.OrbitAudioFailure);
            RefreshStatus();
            RefreshCurrentPage();
        });
    }
}
