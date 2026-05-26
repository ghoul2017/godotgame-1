using Godot;

namespace GodotGame;

public static class InputActions
{
    public static void EnsureConfigured()
    {
        AddMouseAction("select_primary", MouseButton.Left);
        AddMouseAction("command_context", MouseButton.Right);
        AddMouseAction("drag_select", MouseButton.Left);
        AddMouseAction("camera_pan_mouse", MouseButton.Middle);

        for (int index = 1; index <= 9; index++)
        {
            AddKeyAction($"group_{index}", Key.Key0 + index);
        }

        for (int index = 1; index <= 9; index++)
        {
            AddModifiedKeyAction("assign_group", Key.Key0 + index, ctrlPressed: true);
        }

        AddKeyAction("open_build_menu", Key.B);
        AddKeyAction("open_inventory", Key.I);
        AddKeyAction("open_map", Key.M);
        AddKeyAction("open_character_panel", Key.C);
        AddKeyAction("toggle_behavior_panel", Key.V);
        AddKeyAction("cancel_action", Key.Escape);
        AddKeyAction("confirm_action", Key.Enter);

        AddKeyAction("camera_move_up", Key.W);
        AddKeyAction("camera_move_up", Key.Up);
        AddKeyAction("camera_move_down", Key.S);
        AddKeyAction("camera_move_down", Key.Down);
        AddKeyAction("camera_move_left", Key.A);
        AddKeyAction("camera_move_left", Key.Left);
        AddKeyAction("camera_move_right", Key.D);
        AddKeyAction("camera_move_right", Key.Right);
        AddMouseAction("camera_zoom_in", MouseButton.WheelUp);
        AddMouseAction("camera_zoom_out", MouseButton.WheelDown);

        GD.Print("[输入] Input Map 基础行为已确认");
    }

    private static void AddKeyAction(string actionName, Key key)
    {
        EnsureAction(actionName);
        InputEventKey inputEvent = new()
        {
            Keycode = key
        };

        AddEventIfMissing(actionName, inputEvent);
    }

    private static void AddMouseAction(string actionName, MouseButton button)
    {
        EnsureAction(actionName);
        InputEventMouseButton inputEvent = new()
        {
            ButtonIndex = button
        };

        AddEventIfMissing(actionName, inputEvent);
    }

    private static void AddModifiedKeyAction(string actionName, Key key, bool ctrlPressed)
    {
        EnsureAction(actionName);
        InputEventKey inputEvent = new()
        {
            Keycode = key,
            CtrlPressed = ctrlPressed
        };

        AddEventIfMissing(actionName, inputEvent);
    }

    private static void EnsureAction(string actionName)
    {
        if (!InputMap.HasAction(actionName))
        {
            InputMap.AddAction(actionName);
        }
    }

    private static void AddEventIfMissing(string actionName, InputEvent inputEvent)
    {
        foreach (InputEvent existingEvent in InputMap.ActionGetEvents(actionName))
        {
            if (existingEvent.IsMatch(inputEvent, true))
            {
                return;
            }
        }

        InputMap.ActionAddEvent(actionName, inputEvent);
    }
}
