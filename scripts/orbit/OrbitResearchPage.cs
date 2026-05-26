using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GodotGame;

public partial class OrbitStation
{
    private void BuildResearchPage()
    {
        SetPageTitle("研发 | 蓝图与协议解锁");
        AddHintFilter("首批正式研发条目");

        if (_transactionService is null)
        {
            AddEmptyListMessage("研发服务未接入。");
            ShowEmptyDetail("研发不可用", "主入口尚未提供 OrbitTransactionService。");
            return;
        }

        IReadOnlyList<ResearchProjectData> projects = _transactionService.ResearchProjects;
        if (string.IsNullOrEmpty(_selectedId) || projects.All(project => project.Id != _selectedId))
        {
            _selectedId = projects[0].Id;
        }

        foreach (ResearchProjectData project in projects)
        {
            OrbitActionEvaluation evaluation = _transactionService.EvaluateResearch(project);
            Button button = CreateRowButton(
                project.Id,
                $"{project.DisplayName}\n{project.Category}  消耗 {FormatCost(project.GetCostItems(), project.CostCredits)}  解锁 {FormatUnlocks(project.UnlockBlueprintIds, project.UnlockProtocolIds)}  状态 {evaluation.StatusText}",
                project.IconPath,
                () =>
                {
                    _selectedId = project.Id;
                    _pendingActionId = string.Empty;
                    PlayAudio(UiAssets.OrbitAudioSelect);
                    RefreshCurrentPage();
                });
            button.ButtonPressed = project.Id == _selectedId;
            _listContainer?.AddChild(WrapWithStatusIcon(button, StatusIconFor(evaluation), $"{project.Id}Status"));
        }

        ResearchProjectData selected = projects.First(project => project.Id == _selectedId);
        ShowResearchDetail(selected);
    }

    private void ShowResearchDetail(ResearchProjectData project)
    {
        if (_transactionService is null)
        {
            return;
        }

        OrbitActionEvaluation evaluation = _transactionService.EvaluateResearch(project);
        bool pending = _pendingActionId == project.Id;
        string preview = BuildCreditPreview(project.CostCredits);
        SetDetail(project.DisplayName, project.IconPath, string.Join("\n", new[]
        {
            project.Description,
            $"分类：{project.Category}",
            $"消耗：{FormatCost(project.GetCostItems(), project.CostCredits)}",
            $"解锁：{FormatUnlocks(project.UnlockBlueprintIds, project.UnlockProtocolIds)}",
            $"前置研发：{FormatIds(project.RequiredResearchIds)}",
            $"剧情条件：{FormatIds(project.RequiredStoryFlags)}",
            $"当前状态：{evaluation.StatusText}",
            string.IsNullOrEmpty(evaluation.FailureReason) ? preview : $"失败原因：{evaluation.FailureReason}",
            pending ? "确认预览：再次确认后将写入蓝图 / 协议解锁，并生成轨道研发审计记录。" : "点击执行研发会先进入确认状态。"
        }));

        ConfigureAction(pending ? "确认研发" : "执行研发", evaluation.CanExecute, evaluation.FailureReason, () =>
        {
            if (_pendingActionId != project.Id)
            {
                _pendingActionId = project.Id;
                SetFeedback("请确认研发消耗和解锁内容。");
                PlayAudio(UiAssets.OrbitAudioDialogOpen);
                RefreshCurrentPage();
                return;
            }

            OrbitTransactionResult result = _transactionService.ExecuteResearch(project);
            _pendingActionId = string.Empty;
            SetFeedback(result.Message);
            PlayAudio(result.IsSuccess ? UiAssets.OrbitAudioSuccess : UiAssets.OrbitAudioFailure);
            RefreshStatus();
            RefreshCurrentPage();
        });
    }
}
