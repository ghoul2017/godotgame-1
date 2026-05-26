using Godot;

namespace GodotGame;

public sealed class InputIntentController
{
    public enum SurfaceIntent
    {
        None,
        SelectPrimary,
        CommandContext,
        Cancel
    }

    public bool IsUiBlocked { get; private set; }

    public void SetUiBlocked(bool blocked)
    {
        IsUiBlocked = blocked;
    }

    public bool CanHandleSurfaceCommand()
    {
        return !IsUiBlocked;
    }

    public bool IsActionPressed(string actionName)
    {
        return Input.IsActionPressed(actionName);
    }

    public bool IsActionJustPressed(string actionName)
    {
        return Input.IsActionJustPressed(actionName);
    }

    public SurfaceIntent GetSurfaceIntent(InputEvent inputEvent)
    {
        if (IsUiBlocked)
        {
            return SurfaceIntent.None;
        }

        if (inputEvent.IsActionPressed("select_primary"))
        {
            return SurfaceIntent.SelectPrimary;
        }

        if (inputEvent.IsActionPressed("command_context"))
        {
            return SurfaceIntent.CommandContext;
        }

        if (inputEvent.IsActionPressed("cancel_action"))
        {
            return SurfaceIntent.Cancel;
        }

        return SurfaceIntent.None;
    }
}
