using System;
using ImGuiNET;

namespace LibExec.MonoGame;

public static class Gui
{
    public static void Button(string text, Action? callback = null, bool? disabled = null, bool? enabled = null)
    {
        ImGui.BeginDisabled(disabled ?? enabled == false);
        if (ImGui.Button(text))
        {
            callback?.Invoke();
        }

        ImGui.EndDisabled();
    }
}