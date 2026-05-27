using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GodotGame;

public partial class OrbitStation
{
    private void SetPageTitle(string title)
    {
        if (_pageTitle is not null)
        {
            _pageTitle.Text = title;
        }
    }

    private void SetFeedback(string text)
    {
        if (_feedbackLabel is not null)
        {
            _feedbackLabel.Text = text;
        }
    }

    private void AddEmptyListMessage(string text)
    {
        _listContainer?.AddChild(new Label
        {
            Text = text,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });
    }

    private void AddHintFilter(string text)
    {
        Label label = new()
        {
            Text = text,
            VerticalAlignment = VerticalAlignment.Center
        };
        _filterContainer?.AddChild(label);
    }

    private void ShowInfoDetail(OrbitInfoRow row)
    {
        SetDetail(row.Title, row.IconPath, row.Detail);
        ResetAction();
    }

    private void ShowEmptyDetail(string title, string body)
    {
        SetDetail(title, UiAssets.OrbitIconLocked, body);
        ResetAction();
    }

    private void SetDetail(string title, string iconPath, string body)
    {
        if (_detailTitle is not null)
        {
            _detailTitle.Text = title;
        }

        if (_detailIcon is not null)
        {
            _detailIcon.Texture = UiAssets.LoadTexture(iconPath);
        }

        if (_detailBody is not null)
        {
            _detailBody.Text = body;
        }
    }

    private void ConfigureAction(string text, bool enabled, string disabledReason, Action callback)
    {
        if (_actionButton is null || _cancelButton is null)
        {
            return;
        }

        ClearPressedHandlers(_actionButton);
        _actionButton.Text = enabled ? text : $"{text}（{disabledReason}）";
        _actionButton.Disabled = !enabled;
        _actionButton.Pressed += callback;
        _cancelButton.Disabled = string.IsNullOrEmpty(_pendingActionId);
    }

    private void ResetAction()
    {
        if (_actionButton is null || _cancelButton is null)
        {
            return;
        }

        ClearPressedHandlers(_actionButton);
        _actionButton.Text = "无可执行操作";
        _actionButton.Disabled = true;
        _cancelButton.Disabled = string.IsNullOrEmpty(_pendingActionId);
    }

    private void ClearPressedHandlers(Button button)
    {
        foreach (Godot.Collections.Dictionary connection in button.GetSignalConnectionList(BaseButton.SignalName.Pressed))
        {
            Callable callable = connection["callable"].AsCallable();
            button.Disconnect(BaseButton.SignalName.Pressed, callable);
        }
    }

    private Button CreateRowButton(string rowId, string text, string iconPath, Action pressed)
    {
        Button button = new()
        {
            Text = text,
            Icon = UiAssets.LoadTexture(iconPath),
            ExpandIcon = true,
            ToggleMode = true,
            ButtonPressed = rowId == _selectedId,
            CustomMinimumSize = new Vector2(0, 78),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        button.AddThemeConstantOverride("icon_max_width", 44);
        StyleBoxTexture? rowStyle = UiAssets.CreateTextureStyleBox(UiAssets.OrbitListRow, new Rect2(8, 8, 624, 70), 10);
        if (rowStyle is not null)
        {
            button.AddThemeStyleboxOverride("normal", rowStyle);
        }

        button.Pressed += pressed;
        return button;
    }

    private static Control WrapWithStatusIcon(Button button, string statusIconPath, string statusName)
    {
        HBoxContainer row = new()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        TextureRect statusIcon = UiAssets.CreateTextureRect(statusName, statusIconPath);
        statusIcon.CustomMinimumSize = new Vector2(42, 42);
        row.AddChild(statusIcon);
        button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(button);
        return row;
    }

    private static Label CreateLabel(string text, float width)
    {
        return new Label
        {
            Text = text,
            CustomMinimumSize = new Vector2(width, 0),
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
    }

    private static void ClearChildren(Node? node)
    {
        if (node is null)
        {
            return;
        }

        foreach (Node child in node.GetChildren())
        {
            node.RemoveChild(child);
            child.QueueFree();
        }
    }

    private static string StatusIconFor(OrbitActionEvaluation evaluation)
    {
        if (evaluation.IsCompleted)
        {
            return UiAssets.OrbitIconCompleted;
        }

        return evaluation.CanExecute ? UiAssets.OrbitIconAvailable : UiAssets.OrbitIconInsufficient;
    }

    private string BuildCreditPreview(int costCredits)
    {
        if (_gameRoot is null)
        {
            return "信用点变化：未知";
        }

        int before = _gameRoot.Session.OrbitState.Credits;
        return $"信用点变化：{before} -> {before - costCredits}";
    }

    private string FormatCost(IReadOnlyList<ItemStack> items, int credits)
    {
        List<string> parts = new();
        if (credits > 0)
        {
            parts.Add($"{credits} 信用点");
        }

        parts.AddRange(items.Select(stack => $"{GetItemName(stack.ItemId)} x{stack.Count}"));
        return parts.Count == 0 ? "无" : string.Join(", ", parts);
    }

    private string FormatStacks(IReadOnlyList<ItemStack> stacks)
    {
        return stacks.Count == 0 ? "无" : string.Join(", ", stacks.Select(stack => $"{GetItemName(stack.ItemId)} x{stack.Count}"));
    }

    private static string FormatUnlocks(IReadOnlyList<string> blueprints, IReadOnlyList<string> protocols)
    {
        List<string> parts = new();
        parts.AddRange(blueprints.Select(id => $"蓝图 {id}"));
        parts.AddRange(protocols.Select(id => $"协议 {id}"));
        return parts.Count == 0 ? "无" : string.Join(", ", parts);
    }

    private static string FormatIds(IReadOnlyList<string> ids)
    {
        return ids.Count == 0 ? "无" : string.Join(", ", ids);
    }

    private string GetItemName(string itemId)
    {
        return _gameRoot?.DataRegistry.GetItemName(itemId) ?? itemId;
    }

    private static int SkillLevel(UnitInstance instance, string skillId)
    {
        return instance.SkillLevels.TryGetValue(skillId, out int level) ? level : 0;
    }

    private static bool ResourceExists(string path)
    {
        return !string.IsNullOrWhiteSpace(path) && FileAccess.FileExists(path);
    }

    private void PlayAudio(string path)
    {
        if (_audioPlayer is null)
        {
            return;
        }

        if (DisplayServer.GetName() == "headless")
        {
            if (!FileAccess.FileExists(path))
            {
                GD.PushWarning($"[轨道] 音频资源缺失：{path}");
            }

            return;
        }

        AudioStream? stream = LoadAudioStream(path);
        if (stream is null)
        {
            GD.PushWarning($"[轨道] 音频资源加载失败：{path}");
            return;
        }

        _audioPlayer.Stream = stream;
        _audioPlayer.Play();
    }

    private static AudioStream? LoadAudioStream(string path)
    {
        if (path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) && FileAccess.FileExists(path))
        {
            return LoadWavDirect(path);
        }

        AudioStream? importedStream = ResourceLoader.Load<AudioStream>(path);
        if (importedStream is not null)
        {
            return importedStream;
        }

        return null;
    }

    private static AudioStream? LoadWavDirect(string path)
    {
        byte[] bytes = FileAccess.GetFileAsBytes(path);
        if (bytes.Length < 44 ||
            bytes[0] != 'R' ||
            bytes[1] != 'I' ||
            bytes[2] != 'F' ||
            bytes[3] != 'F' ||
            bytes[8] != 'W' ||
            bytes[9] != 'A' ||
            bytes[10] != 'V' ||
            bytes[11] != 'E')
        {
            return null;
        }

        short channels = BitConverter.ToInt16(bytes, 22);
        int sampleRate = BitConverter.ToInt32(bytes, 24);
        short bitsPerSample = BitConverter.ToInt16(bytes, 34);
        int dataOffset = -1;
        int dataSize = 0;
        for (int index = 12; index + 8 <= bytes.Length; index++)
        {
            if (bytes[index] == 'd' && bytes[index + 1] == 'a' && bytes[index + 2] == 't' && bytes[index + 3] == 'a')
            {
                dataOffset = index + 8;
                dataSize = BitConverter.ToInt32(bytes, index + 4);
                break;
            }
        }

        if (dataOffset < 0 || dataSize <= 0 || dataOffset + dataSize > bytes.Length || bitsPerSample != 16)
        {
            return null;
        }

        byte[] data = new byte[dataSize];
        Array.Copy(bytes, dataOffset, data, 0, dataSize);
        return new AudioStreamWav
        {
            Format = AudioStreamWav.FormatEnum.Format16Bits,
            MixRate = sampleRate,
            Stereo = channels == 2,
            Data = data
        };
    }
}
